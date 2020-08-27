using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Default implementation of the <see cref="IEventMessagePush"/> feature.
    /// </summary>
    /// <remarks>
    ///   This implementation pushes ephemeral event messages to subscribers. To maintain an 
    ///   in-memory buffer of historical events, use <see cref="InMemoryEventMessageStore"/>.
    /// </remarks>
    public class EventMessagePush : IEventMessagePush, IFeatureHealthCheck, IDisposable {

        /// <summary>
        /// The scheduler to use when running background tasks.
        /// </summary>
        protected IBackgroundTaskService Scheduler { get; }

        /// <summary>
        /// Logging.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Fires when then object is being disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// A cancellation token that will fire when the object is disposed.
        /// </summary>
        protected CancellationToken DisposedToken => _disposedTokenSource.Token;

        /// <summary>
        /// Feature options.
        /// </summary>
        private readonly EventMessagePushOptions _options;

        /// <summary>
        /// The last subscription ID that was issued.
        /// </summary>
        private int _lastSubscriptionId;

        /// <summary>
        /// The current subscriptions.
        /// </summary>
        private readonly ConcurrentDictionary<int, EventSubscriptionChannel<int, string, EventMessage>> _subscriptions = new ConcurrentDictionary<int, EventSubscriptionChannel<int, string, EventMessage>>();

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
        /// Channel that is used to publish new event messages. This is a single-consumer channel; the 
        /// consumer thread will then re-publish to subscribers as required.
        /// </summary>
        private readonly Channel<EventMessage> _masterChannel = Channel.CreateUnbounded<EventMessage>(new UnboundedChannelOptions() {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });

        /// <summary>
        /// Emits all messages that are published to the internal master channel.
        /// </summary>
        public event Action<EventMessage> Publish;


        /// <summary>
        /// Creates a new <see cref="EventMessagePush"/> object.
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
        public EventMessagePush(EventMessagePushOptions options, IBackgroundTaskService scheduler, ILogger logger) {
            _options = options ?? new EventMessagePushOptions();
            Scheduler = scheduler ?? BackgroundTaskService.Default;
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            Scheduler.QueueBackgroundWorkItem(PublishToSubscribers, _disposedTokenSource.Token);
        }


        /// <inheritdoc/>
        public Task<ChannelReader<EventMessage>> Subscribe(
            IAdapterCallContext context,
            CreateEventMessageSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            var subscriptionId = Interlocked.Increment(ref _lastSubscriptionId);
            var subscription = new EventSubscriptionChannel<int, string, EventMessage>(
                subscriptionId,
                context,
                Scheduler,
                null,
                request.SubscriptionType, 
                TimeSpan.Zero,
                new [] { DisposedToken, cancellationToken },
                () => OnSubscriptionCancelledInternal(subscriptionId),
                10
            );
            _subscriptions[subscriptionId] = subscription;

            HasSubscriptions = _subscriptions.Count > 0;
            HasActiveSubscriptions = _subscriptions.Values.Any(x => x.SubscriptionType == EventMessageSubscriptionType.Active);
            OnSubscriptionAdded();

            return Task.FromResult(subscription.Reader);
        }


        /// <summary>
        /// Invoked when a subscription is created.
        /// </summary>
        protected virtual void OnSubscriptionAdded() { }


        /// <summary>
        /// Invoked when a subscription is removed.
        /// </summary>
        protected virtual void OnSubscriptionCancelled() { }


        /// <summary>
        /// Sends an event message to subscribers.
        /// </summary>
        /// <param name="message">
        ///   The message to publish.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the value was published to subscribers.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="message"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask<bool> ValueReceived(EventMessage message, CancellationToken cancellationToken = default) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            try {
                using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposedTokenSource.Token)) {
                    await _masterChannel.Writer.WaitToWriteAsync(ctSource.Token).ConfigureAwait(false);
                    return _masterChannel.Writer.TryWrite(message);
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
        /// Notifies the <see cref="EventMessagePush"/> that a subscription was cancelled.
        /// </summary>
        /// <param name="id">
        ///   The cancelled subscription ID.
        /// </param>
        private void OnSubscriptionCancelledInternal(int id) {
            if (_isDisposed) {
                return;
            }

            if (_subscriptions.TryRemove(id, out var subscription)) {
                subscription.Dispose();
                HasSubscriptions = _subscriptions.Count > 0;
                HasActiveSubscriptions = _subscriptions.Values.Any(x => x.SubscriptionType == EventMessageSubscriptionType.Active);
                OnSubscriptionCancelled();
            }
        }


        /// <inheritdoc/>
        public Task<HealthCheckResult> CheckFeatureHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            var subscriptions = _subscriptions.Values.ToArray();

            var result = HealthCheckResult.Healthy(nameof(EventMessagePush), data: new Dictionary<string, string>() {
                { Resources.HealthChecks_Data_ActiveSubscriberCount, subscriptions.Count(x => x.SubscriptionType == EventMessageSubscriptionType.Active).ToString(context?.CultureInfo) },
                { Resources.HealthChecks_Data_PassiveSubscriberCount, subscriptions.Count(x => x.SubscriptionType == EventMessageSubscriptionType.Passive).ToString(context?.CultureInfo) }
            });

            return Task.FromResult(result);
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~EventMessagePush() {
            Dispose(false);
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the <see cref="EventMessagePush"/> is being 
        ///   disposed, or <see langword="false"/> if it is being finalized.
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (_isDisposed) {
                return;
            }

            if (disposing) {
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
                _masterChannel.Writer.TryComplete();

                foreach (var item in _subscriptions.Values.ToArray()) {
                    item.Dispose();
                }

                _subscriptions.Clear();
            }

            _isDisposed = true;
        }


        /// <summary>
        /// Long-running task that sends event messages to subscribers whenever they are added to 
        /// the <see cref="_masterChannel"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that can be used to stop processing of the queue.
        /// </param>
        /// <returns>
        ///   A task that will complete when the cancellation token fires.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Ensures recovery from errors occurring when publishing messages to subscribers")]
        private async Task PublishToSubscribers(CancellationToken cancellationToken) {
            try {
                while (await _masterChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (_masterChannel.Reader.TryRead(out var message)) {
                        if (message == null) {
                            continue;
                        }

                        var subscribers = _subscriptions.Values.ToArray();
                        foreach (var subscriber in subscribers) {
                            if (cancellationToken.IsCancellationRequested) {
                                break;
                            }

                            try {
                                var success = subscriber.Publish(message);
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
            }
            catch (OperationCanceledException) {
                // Cancellation token fired
            }
            catch (ChannelClosedException) {
                // Channel was closed
            }
        }

    }


    /// <summary>
    /// Options for <see cref="EventMessagePushOptions"/>
    /// </summary>
    public class EventMessagePushOptions {

        /// <summary>
        /// The adapter name to use when creating subscription IDs.
        /// </summary>
        public string AdapterId { get; set; }

    }
}
