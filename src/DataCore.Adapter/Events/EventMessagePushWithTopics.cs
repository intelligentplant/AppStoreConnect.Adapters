﻿using System;
using System.Collections.Concurrent;
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
        /// The <see cref="IBackgroundTaskService"/> to use when running background tasks.
        /// </summary>
        protected IBackgroundTaskService BackgroundTaskService { get; }

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
        private readonly Channel<(EventMessage Value, EventSubscriptionChannel<int>[] Subscribers)> _masterChannel = Channel.CreateUnbounded<(EventMessage, EventSubscriptionChannel<int>[])>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false
        });

        /// <summary>
        /// Channel that is used to publish changes to subscribed topics.
        /// </summary>
        private readonly Channel<(List<string> Topics, bool Added, TaskCompletionSource<bool> Processed)> _topicSubscriptionChangesChannel = Channel.CreateUnbounded<(List<string>, bool, TaskCompletionSource<bool>)>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });

        /// <summary>
        /// Maximum number of concurrent subscriptions.
        /// </summary>
        private readonly int _maxSubscriptionCount;

        /// <summary>
        /// The last subscription ID that was issued.
        /// </summary>
        private int _lastSubscriptionId;

        /// <summary>
        /// The current subscriptions.
        /// </summary>
        private readonly ConcurrentDictionary<int, EventSubscriptionChannel<int>> _subscriptions = new ConcurrentDictionary<int, EventSubscriptionChannel<int>>();

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
        protected bool HasSubscriptions { get { return !_subscriptions.IsEmpty; } }

        /// <summary>
        /// Indicates if the subscription manager holds any active subscriptions. If your adapter uses 
        /// a forward-only cursor that you do not want to advance when only passive listeners are 
        /// attached to the adapter, you can use this property to identify if any active listeners are 
        /// attached.
        /// </summary>
        protected bool HasActiveSubscriptions { get; private set; }

        /// <summary>
        /// Publishes all event messages passed to the <see cref="EventMessagePushWithTopics"/> via the 
        /// <see cref="ValueReceived"/> method.
        /// </summary>
        public event Action<EventMessage>? Publish;


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
        public EventMessagePushWithTopics(EventMessagePushWithTopicsOptions? options, IBackgroundTaskService? scheduler, ILogger? logger) {
            _options = options ?? new EventMessagePushWithTopicsOptions();
            _maxSubscriptionCount = _options.MaxSubscriptionCount;
            BackgroundTaskService = scheduler ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

            BackgroundTaskService.QueueBackgroundWorkItem(ProcessTopicSubscriptionChangesChannel, _disposedTokenSource.Token);
            BackgroundTaskService.QueueBackgroundWorkItem(ProcessPublishToSubscribersChannel, _disposedTokenSource.Token);
        }


        /// <summary>
        /// Gets the composite set of topics that are currently being subscribed to by all 
        /// subscribers.
        /// </summary>
        /// <returns>
        ///   The subscribed topics.
        /// </returns>
        protected IEnumerable<string> GetSubscribedTopics() {
            _subscriptionsLock.EnterReadLock();
            try {
                return _subscriberCount.Keys.ToArray();
            }
            finally {
                _subscriptionsLock.ExitReadLock();
            }
        }


        /// <summary>
        /// Called when topics are added to a subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="topics">
        ///   The subscription topics
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will process the operation.
        /// </returns>
        private async Task OnTopicsAddedToSubscriptionInternal(EventSubscriptionChannel<int> subscription, IEnumerable<string> topics, CancellationToken cancellationToken) {
            TaskCompletionSource<bool> processed = null!;

            subscription.AddTopics(topics);

            _subscriptionsLock.EnterWriteLock();
            try {
                var newSubscriptions = new List<string>();

                foreach (var topic in topics) {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }
                    if (topic == null) {
                        continue;
                    }

                    if (!_subscriberCount.TryGetValue(topic, out var subscriberCount)) {
                        subscriberCount = 0;
                        newSubscriptions.Add(topic);
                    }

                    _subscriberCount[topic] = ++subscriberCount;
                }

                if (newSubscriptions.Count > 0) {
                    processed = new TaskCompletionSource<bool>();
                    _topicSubscriptionChangesChannel.Writer.TryWrite((newSubscriptions, true, processed));
                }
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }

            if (processed == null) {
                return;
            }

            // Wait for last change to be processed.
            await processed.Task.WithCancellation(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Called when topics are removed from a subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="topics">.
        ///   The subscription topics
        /// </param>
        /// <returns>
        ///   A task that will process the operation.
        /// </returns>
        private void OnTopicsRemovedFromSubscriptionInternal(EventSubscriptionChannel<int> subscription, IEnumerable<string> topics) {
            _subscriptionsLock.EnterWriteLock();
            try {
                var removedSubscriptions = new List<string>();

                foreach (var topic in topics) {
                    if (topic == null || !subscription.RemoveTopic(topic)) {
                        continue;
                    }

                    if (!_subscriberCount.TryGetValue(topic, out var subscriberCount)) {
                        continue;
                    }

                    --subscriberCount;

                    if (subscriberCount == 0) {
                        _subscriberCount.Remove(topic);
                        removedSubscriptions.Add(topic);
                    }
                    else {
                        _subscriberCount[topic] = subscriberCount;
                    }
                }

                if (removedSubscriptions.Count > 0) {
                    _topicSubscriptionChangesChannel.Writer.TryWrite((removedSubscriptions, false, null!));
                }
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }
        }


        /// <summary>
        /// Invoked by a subscription object when a topic is added to the subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="initialTopics">
        ///   The initial topics to subscribe to,
        /// </param>
        private async Task OnSubscriptionAddedInternal(EventSubscriptionChannel<int> subscription, IEnumerable<string> initialTopics) {
            await OnTopicsAddedToSubscriptionInternal(subscription, initialTopics, subscription.CancellationToken).ConfigureAwait(false);
            OnSubscriptionAdded(subscription);
        }


        /// <summary>
        /// Invoked when a subscription has been cancelled.
        /// </summary>
        /// <param name="subscriptionId">
        ///   The subscription ID.
        /// </param>
        private void OnSubscriptionCancelledInternal(int subscriptionId) {
            if (_isDisposed) {
                return;
            }

            if (!_subscriptions.TryRemove(subscriptionId, out var subscription)) {
                return;
            }

            try {
                OnTopicsRemovedFromSubscriptionInternal(subscription, subscription.Topics);
            }
            finally {
                subscription.Dispose();
                HasActiveSubscriptions = HasSubscriptions && _subscriptions.Values.Any(x => x.SubscriptionType == EventMessageSubscriptionType.Active);
                OnSubscriptionCancelled(subscription);
            }
        }


        /// <summary>
        /// Called when the number of subscribers for a topic changes from zero to one.
        /// </summary>
        /// <param name="topics">
        ///   The topics that were added.
        /// </param>
        protected virtual void OnTopicsAdded(IEnumerable<string> topics) {
            _options.OnTopicSubscriptionsAdded?.Invoke(topics);
        }


        /// <summary>
        /// Called when the number of subscribers for a topic changes from one to zero.
        /// </summary>
        /// <param name="topics">
        ///   The topics that were removed.
        /// </param>
        protected virtual void OnTopicsRemoved(IEnumerable<string> topics) {
            _options.OnTopicSubscriptionsRemoved?.Invoke(topics);
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
                try {
                    if (!await _topicSubscriptionChangesChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        break;
                    }
                }
                catch (OperationCanceledException) {
                    break;
                }
                catch (ChannelClosedException) {
                    break;
                }

                while (_topicSubscriptionChangesChannel.Reader.TryRead(out var change)) {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }

                    try {
                        if (change.Added) {
                            OnTopicsAdded(change.Topics);
                        }
                        else {
                            OnTopicsRemoved(change.Topics);
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
                            change.Topics.Count,
                            change.Added
                                ? SubscriptionUpdateAction.Subscribe
                                : SubscriptionUpdateAction.Unsubscribe
                        );
                    }
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
                try {
                    if (!await _masterChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        break;
                    }
                }
                catch (OperationCanceledException) {
                    break;
                }
                catch (ChannelClosedException) {
                    break;
                }

                while (_masterChannel.Reader.TryRead(out var item)) {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }

                    Publish?.Invoke(item.Value);

                    foreach (var subscriber in item.Subscribers) {
                        if (cancellationToken.IsCancellationRequested) {
                            break;
                        }

                        try {
                            var success = subscriber.Publish(item.Value);
                            if (!success) {
                                Logger.LogTrace(Resources.Log_PublishToSubscriberWasUnsuccessful, subscriber.Context?.ConnectionId);
                            }
                        }
                        catch (Exception e) {
                            Logger.LogError(e, Resources.Log_PublishToSubscriberThrewException, subscriber.Context?.ConnectionId);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Invoked when a subscription is created.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        protected virtual void OnSubscriptionAdded(EventSubscriptionChannel<int> subscription) { }


        /// <summary>
        /// Invoked when a subscription is cancelled.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        protected virtual void OnSubscriptionCancelled(EventSubscriptionChannel<int> subscription) { }


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

            EventSubscriptionChannel<int>[] subscribers;

            _subscriptionsLock.EnterReadLock();
            try {
                subscribers = _subscriptions.Values.Where(x => x.Topics.Any(t => IsTopicMatch(t, value.Topic))).ToArray();
            }
            finally {
                _subscriptionsLock.ExitReadLock();
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


        /// <summary>
        /// Checks to see if the specified subscription topic and event message topic match.
        /// </summary>
        /// <param name="subscriptionTopic">
        ///   The topic that was specified by the subscriber.
        /// </param>
        /// <param name="eventMessageTopic">
        ///   The topic for the received event message.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the event message should be published to the subscriber, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   This method is used to determine if an event message will be pushed to a subscriber. 
        ///   The default behaviour is to return <see langword="true"/> if <paramref name="subscriptionTopic"/> 
        ///   and <paramref name="eventMessageTopic"/> topic are equal using a case-insensitive 
        ///   match.
        /// </para>
        /// 
        /// <para>
        ///   Override this method if your adapter allows e.g. the use of wildcards in 
        ///   subscription topics.
        /// </para>
        /// 
        /// </remarks>
        protected virtual bool IsTopicMatch(string subscriptionTopic, string? eventMessageTopic) {
            return string.Equals(subscriptionTopic, eventMessageTopic, StringComparison.Ordinal);
        }


        /// <summary>
        /// Runs a long-running task that will process changes on a subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <param name="channel">
        ///   The subscription changes channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The long-running task.
        /// </returns>
        private async Task RunSubscriptionChanngesListener(EventSubscriptionChannel<int> subscription, ChannelReader<EventMessageSubscriptionUpdate> channel, CancellationToken cancellationToken) {
            while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (channel.TryRead(out var item)) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (item?.Topics == null) {
                        continue;
                    }

                    var topics = item.Topics.Where(x => x != null).ToArray();
                    if (topics.Length == 0) {
                        continue;
                    }

                    if (item.Action == SubscriptionUpdateAction.Subscribe) {
                        await OnTopicsAddedToSubscriptionInternal(subscription, topics, cancellationToken).ConfigureAwait(false);
                    }
                    else {
                        OnTopicsRemovedFromSubscriptionInternal(subscription, topics);
                    }
                }
            }
        }


        /// <inheritdoc/>
        public async Task<ChannelReader<EventMessage>> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageTopicSubscriptionRequest request,
            ChannelReader<EventMessageSubscriptionUpdate> channel,
            CancellationToken cancellationToken
        ) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            ValidationExtensions.ValidateObject(request);
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            if (_maxSubscriptionCount > 0 && _subscriptions.Count >= _maxSubscriptionCount) {
                throw new InvalidOperationException(Resources.Error_TooManySubscriptions);
            }

            var subscriptionId = Interlocked.Increment(ref _lastSubscriptionId);
            var subscription = new EventSubscriptionChannel<int>(
                subscriptionId,
                context,
                BackgroundTaskService,
                request.SubscriptionType,
                TimeSpan.Zero,
                new[] { DisposedToken, cancellationToken },
                () => OnSubscriptionCancelledInternal(subscriptionId),
                10
            );
            _subscriptions[subscriptionId] = subscription;

            try {
                await OnSubscriptionAddedInternal(subscription, request.Topics?.Where(x => x != null) ?? Array.Empty<string>()).ConfigureAwait(false);
            }
            catch {
                OnSubscriptionCancelledInternal(subscriptionId);
                throw;
            }

            BackgroundTaskService.QueueBackgroundWorkItem(ct => RunSubscriptionChanngesListener(subscription, channel, ct), subscription.CancellationToken);

            return subscription.Reader;
        }


        /// <inheritdoc/>
        public Task<HealthCheckResult> CheckFeatureHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            int subscribedTagCount;

            _subscriptionsLock.EnterReadLock();
            try {
                subscribedTagCount = _subscriberCount.Count;
            }
            finally {
                _subscriptionsLock.ExitReadLock();
            }


            var result = HealthCheckResult.Healthy(nameof(EventMessagePushWithTopics), data: new Dictionary<string, string>() {
                { Resources.HealthChecks_Data_SubscriberCount, _subscriptions.Count.ToString(context?.CultureInfo) },
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
                    foreach (var subscription in _subscriptions.Values.ToArray()) {
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

    }


    /// <summary>
    /// Options for <see cref="EventMessagePush"/>.
    /// </summary>
    public class EventMessagePushWithTopicsOptions {

        /// <summary>
        /// The adapter name to use when creating subscription IDs.
        /// </summary>
        public string AdapterId { get; set; } = default!;

        /// <summary>
        /// The maximum number of concurrent subscriptions allowed. When this limit is hit, 
        /// attempts to create additional subscriptions will throw exceptions. A value less than 
        /// one indicates no limit.
        /// </summary>
        public int MaxSubscriptionCount { get; set; }

        /// <summary>
        /// A delegate that is invoked when the number of subscribers for a topic changes from zero 
        /// to one.
        /// </summary>
        public Action<IEnumerable<string>>? OnTopicSubscriptionsAdded { get; set; }

        /// <summary>
        /// A delegate that is invoked when the number of subscribers for a topic changes from one 
        /// to zero.
        /// </summary>
        public Action<IEnumerable<string>>? OnTopicSubscriptionsRemoved { get; set; }

    }

}
