using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Implements <see cref="IEventMessagePushWithTopics"/>.
    /// </summary>
    public class EventMessagePushWithTopics : IEventMessagePushWithTopics, IFeatureHealthCheck, IDisposable {

        /// <summary>
        /// The <see cref="EventMessage"/> property name that contains the event's topic name.
        /// </summary>
        public const string EventTopicPropertyName = "Topic";

        /// <summary>
        /// Indicates if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The scheduler to use when running background tasks.
        /// </summary>
        protected IBackgroundTaskService Scheduler { get; }

        /// <summary>
        /// The logger.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Fires when the <see cref="EventMessagePushWithTopics"/> is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// A cancellation token that will fire when the object is disposed.
        /// </summary>
        protected CancellationToken DisposedToken => _disposedTokenSource.Token;

        /// <summary>
        /// Feature options.
        /// </summary>
        private readonly EventMessagePushWithTopicsOptions _options;

        /// <summary>
        /// Channel that is used to publish new event messages. This is a single-consumer channel; the 
        /// consumer thread will then re-publish to subscribers as required.
        /// </summary>
        private readonly Channel<(EventMessage Value, EventMessageSubscriptionWithTopicsBase[] Subscribers)> _masterChannel = Channel.CreateUnbounded<(EventMessage, EventMessageSubscriptionWithTopicsBase[])>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false
        });

        /// <summary>
        /// Channel that is used to publish changes to subscribed topics.
        /// </summary>
        private readonly Channel<(string Topic, bool Added, TaskCompletionSource<bool> Processed)> _topicSubscriptionChangesChannel = Channel.CreateUnbounded<(string, bool, TaskCompletionSource<bool>)>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });

        /// <summary>
        /// All subscriptions.
        /// </summary>
        private readonly HashSet<Subscription> _subscriptions = new HashSet<Subscription>();

        /// <summary>
        /// Maps from topic name to the subscriber count for that topic.
        /// </summary>
        private readonly Dictionary<string, int> _subscriberCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// For protecting access to <see cref="_subscriptions"/> and <see cref="_subscriberCount"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _subscriptionsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Indicates if the subscription manager currently holds any subscriptions.
        /// </summary>
        protected bool HasSubscriptions { get; private set; }

        /// <summary>
        /// Indicates if the subscription manager holds any active subscriptions. If your adapter uses 
        /// a forward-only cursor that you do not want to advance when only passive listeners are 
        /// attached to the adapter, you can use this property to identify if any active listeners are 
        /// attached.
        /// </summary>
        protected bool HasActiveSubscriptions { get; private set; }

        /// <summary>
        /// Emits all values that are published to the internal master channel.
        /// </summary>
        public event Action<EventMessage> Publish;


        /// <summary>
        /// Creates a new <see cref="EventMessagePushWithTopics"/> object.
        /// </summary>
        /// <param name="options">
        ///   The feature options.
        /// </param>
        /// <param name="scheduler">
        ///   The task scheduler to use when running background operations.
        /// </param>
        /// <param name="logger">
        ///   The logger for the subscription manager.
        /// </param>
        public EventMessagePushWithTopics(EventMessagePushWithTopicsOptions options, IBackgroundTaskService scheduler, ILogger logger) {
            _options = options ?? new EventMessagePushWithTopicsOptions();
            Scheduler = scheduler ?? BackgroundTaskService.Default;
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

            Scheduler.QueueBackgroundWorkItem(ProcessTopicSubscriptionChangesChannel, _disposedTokenSource.Token);
            Scheduler.QueueBackgroundWorkItem(ProcessPublishToSubscribersChannel, _disposedTokenSource.Token);
        }


        /// <summary>
        /// Invoked by a subscription object when a topic is added to the subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="topic">
        ///   The topic.
        /// </param>
        private async Task OnTopicAddedToSubscription(Subscription subscription, string topic) {
            if (subscription == null || string.IsNullOrWhiteSpace(topic)) {
                return;
            }

            var isNewSubscription = false;
            TaskCompletionSource<bool> processed = null;

            _subscriptionsLock.EnterWriteLock();
            try {
                if (!_subscriberCount.TryGetValue(topic, out var subscriberCount)) {
                    subscriberCount = 0;
                    isNewSubscription = true;
                }

                _subscriberCount[topic] = ++subscriberCount;
                if (isNewSubscription) {
                    processed = new TaskCompletionSource<bool>();
                    _topicSubscriptionChangesChannel.Writer.TryWrite((topic, true, processed));
                }
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }

            if (processed == null) {
                return;
            }

            // Wait for change to be processed.
            await processed.Task.WithCancellation(DisposedToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Invoked by a subscription object when a topic is removed from a subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="topic">
        ///   The topic.
        /// </param>
        private async Task OnTopicRemovedFromSubscription(Subscription subscription, string topic) {
            if (subscription == null || string.IsNullOrWhiteSpace(topic)) {
                return;
            }

            TaskCompletionSource<bool> processed = null;

            _subscriptionsLock.EnterWriteLock();
            try {
                if (!_subscriberCount.TryGetValue(topic, out var subscriberCount)) {
                    // No subscribers
                    return;
                }

                --subscriberCount;

                if (subscriberCount == 0) {
                    // No subscribers remaining.
                    _subscriberCount.Remove(topic);
                    processed = new TaskCompletionSource<bool>();
                    _topicSubscriptionChangesChannel.Writer.TryWrite((topic, false, processed));
                }
                else {
                    _subscriberCount[topic] = subscriberCount;
                }
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }

            if (processed == null) {
                return;
            }

            // Wait for change to be processed.
            await processed.Task.WithCancellation(DisposedToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Invoked when a subscription has been cancelled.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        private void OnSubscriptionCancelled(Subscription subscription) {
            _subscriptionsLock.EnterWriteLock();
            try {
                _subscriptions.Remove(subscription);
                foreach (var topic in subscription.GetSubscribedTopics()) {
                    if (!_subscriberCount.TryGetValue(topic, out var subscriberCount)) {
                        continue;
                    }

                    --subscriberCount;

                    if (subscriberCount == 0) {
                        _subscriberCount.Remove(topic);
                        _topicSubscriptionChangesChannel.Writer.TryWrite((topic, false, null));
                    }
                    else {
                        _subscriberCount[topic] = subscriberCount;
                    }
                }
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }
        }


        /// <summary>
        /// Called when the number of subscribers for a topic changes from zero to one.
        /// </summary>
        /// <param name="topic">
        ///   The topic.
        /// </param>
        protected virtual void OnTopicAddedToSubscription(string topic) {
            _options.OnTopicSubscriptionAdded?.Invoke(topic);
        }


        /// <summary>
        /// Called when the number of subscribers for a topic changes from one to zero.
        /// </summary>
        /// <param name="topic">
        ///   The topic.
        /// </param>
        protected virtual void OnTopicRemovedFromSubscription(string topic) {
            _options.OnTopicSubscriptionRemoved?.Invoke(topic);
        }



        /// <summary>
        /// Starts a long-running that that will read and process subscription changes published 
        /// to <see cref="_topicSubscriptionChangesChannel"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the task should exit.
        /// </param>
        /// <returns>
        ///   A long-running task.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exceptions are written to associated TaskCompletionSource instances")]
        private async Task ProcessTopicSubscriptionChangesChannel(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                if (!await _topicSubscriptionChangesChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    break;
                }

                if (!_topicSubscriptionChangesChannel.Reader.TryRead(out var change) || string.IsNullOrWhiteSpace(change.Topic)) {
                    continue;
                }

                try {
                    if (change.Added) {
                        OnTopicAddedToSubscription(change.Topic);
                    }
                    else {
                        OnTopicRemovedFromSubscription(change.Topic);
                    }

                    if (change.Processed != null) {
                        change.Processed.TrySetResult(true);
                    }
                }
                catch (Exception e) {
                    if (change.Processed != null) {
                        change.Processed.TrySetException(e);
                    }

                    Logger.LogError(
                        e,
                        Resources.Log_ErrorWhileProcessingSubscriptionTopicChange,
                        change.Topic,
                        change.Added
                            ? SubscriptionUpdateAction.Subscribe
                            : SubscriptionUpdateAction.Unsubscribe
                    );
                }
            }
        }



        /// <summary>
        /// Starts a long-running task that will read values published to <see cref="_masterChannel"/> 
        /// and re-publish them to subscribers for the topic.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the task should exit.
        /// </param>
        /// <returns>
        ///   A long-running task.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Ensures recovery from errors occurring when publishing messages to subscribers")]
        private async Task ProcessPublishToSubscribersChannel(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                if (!await _masterChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    break;
                }

                if (!_masterChannel.Reader.TryRead(out var item)) {
                    continue;
                }

                Publish?.Invoke(item.Value);

                foreach (var subscriber in item.Subscribers) {
                    try {
                        var success = await subscriber.ValueReceived(item.Value, cancellationToken).ConfigureAwait(false);
                        if (!success) {
                            Logger.LogTrace(Resources.Log_PublishToSubscriberWasUnsuccessful, subscriber.Context?.ConnectionId);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception e) {
                        Logger.LogError(e, Resources.Log_PublishToSubscriberThrewException, subscriber.Context?.ConnectionId);
                    }
                }
            }
        }


        /// <summary>
        /// Creates a new subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscriber.
        /// </param>
        /// <param name="request">
        ///   The subscription request settings.
        /// </param>
        /// <returns>
        ///   The new subscription.
        /// </returns>
        protected virtual Subscription CreateSubscription(IAdapterCallContext context, CreateEventMessageSubscriptionRequest request) {
            return new Subscription(context, request, this);
        }


        /// <summary>
        /// Invoked when a subscription is created.
        /// </summary>
        protected virtual void OnSubscriptionAdded() { }


        /// <summary>
        /// Invoked when a subscription is removed.
        /// </summary>
        /// <returns>
        ///   A task that will perform subscription-related activities.
        /// </returns>
        protected virtual void OnSubscriptionRemoved() { }


        /// <summary>
        /// Publishes a value to subscribers.
        /// </summary>
        /// <param name="value">
        ///   The value to publish.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the value was published to subscribers.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask<bool> ValueReceived(EventMessage value, CancellationToken cancellationToken = default) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            EventMessageSubscriptionWithTopicsBase[] subscribers;

            _subscriptionsLock.EnterReadLock();
            try {
                subscribers = _subscriptions.Where(x => x.IsSubscribed(value)).ToArray();
            }
            finally {
                _subscriptionsLock.ExitReadLock();
            }

            if (!subscribers.Any()) {
                return false;
            }

            try {
                using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposedTokenSource.Token)) {
                    await _masterChannel.Writer.WaitToWriteAsync(ctSource.Token).ConfigureAwait(false);
                    return _masterChannel.Writer.TryWrite((value, subscribers));
                }
            }
            catch (OperationCanceledException) {
                if (cancellationToken.IsCancellationRequested) {
                    // Cancellation token provided by the caller has fired; rethrow the exception.
                    throw;
                }

                // The stream manager is being disposed.
                return false;
            }
        }


        /// <inheritdoc/>
        public async Task<IEventMessageSubscriptionWithTopics> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageSubscriptionRequest request
        ) {
            var subscription = CreateSubscription(context, request ?? new CreateEventMessageSubscriptionRequest());

            bool added;
            _subscriptionsLock.EnterWriteLock();
            try {
                added = _subscriptions.Add(subscription);
                if (added) {
                    HasSubscriptions = _subscriptions.Count > 0;
                    HasActiveSubscriptions = _subscriptions.Any(x => x.SubscriptionType == EventMessageSubscriptionType.Active);
                }
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }

            if (added) {
                await subscription.Start().ConfigureAwait(false);
                OnSubscriptionAdded();
            }

            return subscription;
        }


        /// <inheritdoc/>
        public Task<HealthCheckResult> CheckFeatureHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            Subscription[] subscriptions;
            int subscribedTagCount;

            _subscriptionsLock.EnterReadLock();
            try {
                subscriptions = _subscriptions.ToArray();
                subscribedTagCount = _subscriberCount.Count;
            }
            finally {
                _subscriptionsLock.ExitReadLock();
            }


            var result = HealthCheckResult.Healthy(nameof(EventMessagePushWithTopics), data: new Dictionary<string, string>() {
                { Resources.HealthChecks_Data_SubscriberCount, subscriptions.Length.ToString(context?.CultureInfo) },
                { Resources.HealthChecks_Data_TopicCount, subscribedTagCount.ToString(context?.CultureInfo) }
            });

            return Task.FromResult(result);
        }


        /// <inheritdoc/>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~EventMessagePushWithTopics() {
            Dispose(false);
        }


        /// <summary>
        /// Releases resources held by the <see cref="EventMessagePushWithTopics"/>.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the object is being disposed, or <see langword="false"/> 
        ///   if it is being finalized.
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (_isDisposed) {
                return;
            }

            if (disposing) {
                _isDisposed = true;
                _masterChannel.Writer.TryComplete();
                _topicSubscriptionChangesChannel.Writer.TryComplete();
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
                _subscriptionsLock.EnterWriteLock();
                try {
                    _subscriberCount.Clear();
                    foreach (var subscription in _subscriptions.ToArray()) {
                        subscription.Dispose();
                    }
                    _subscriptions.Clear();
                }
                finally {
                    _subscriptionsLock.ExitWriteLock();
                    _subscriptionsLock.Dispose();
                }
            }
        }


        #region [ Inner Types ]

        /// <summary>
        /// Implements <see cref="EventMessageSubscriptionBase"/>.
        /// </summary>

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Access to private members required")]
        public class Subscription : EventMessageSubscriptionWithTopicsBase {

            /// <summary>
            /// The owning feature.
            /// </summary>
            private readonly EventMessagePushWithTopics _feature;


            /// <summary>
            /// Creates a new <see cref="Subscription"/> object.
            /// </summary>
            /// <param name="context">
            ///   The adapter call context for the subscriber.
            /// </param>
            /// <param name="request">
            ///   The subscription request.
            /// </param>
            /// <param name="feature">
            ///   The subscription manager that the subscription is attached to.
            /// </param>
            public Subscription(
                IAdapterCallContext context,
                CreateEventMessageSubscriptionRequest request,
                EventMessagePushWithTopics feature
            ) : base(
                context,
                feature?._options?.AdapterId,
                request?.SubscriptionType ?? throw new ArgumentNullException(nameof(request))
            ) {
                _feature = feature ?? throw new ArgumentNullException(nameof(feature));
            }


            /// <inheritdoc/>
            protected override Task OnTopicAdded(string topic) {
                return _feature.OnTopicAddedToSubscription(this, topic);
            }


            /// <inheritdoc/>
            protected override Task OnTopicRemoved(string topic) {
                return _feature.OnTopicRemovedFromSubscription(this, topic);
            }


            /// <inheritdoc/>
            protected override void OnCancelled() {
                base.OnCancelled();
                _feature.OnSubscriptionCancelled(this);
            }

        }

        #endregion

    }


    /// <summary>
    /// Options for <see cref="EventMessagePush"/>.
    /// </summary>
    public class EventMessagePushWithTopicsOptions {

        /// <summary>
        /// The adapter name to use when creating subscription IDs.
        /// </summary>
        public string AdapterId { get; set; }

        /// <summary>
        /// A delegate that is invoked when the number of subscribers for a topic changes from zero 
        /// to one.
        /// </summary>
        public Action<string> OnTopicSubscriptionAdded { get; set; }

        /// <summary>
        /// A delegate that is invoked when the number of subscribers for a topic changes from one 
        /// to zero.
        /// </summary>
        public Action<string> OnTopicSubscriptionRemoved { get; set; }

    }

}
