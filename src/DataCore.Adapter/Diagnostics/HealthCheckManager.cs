using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Default <see cref="IHealthCheck"/> implementation.
    /// </summary>
    internal partial class HealthCheckManager<TAdapterOptions> : IBackgroundTaskServiceProvider, IHealthCheck, IDisposable where TAdapterOptions : AdapterOptions, new() {

        /// <summary>
        /// Indicates if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The owning adapter.
        /// </summary>
        private readonly AdapterBase<TAdapterOptions> _adapter;

        /// <summary>
        /// The most recent health check that was performed.
        /// </summary>
        private HealthCheckResultWithSequenceId? _latestHealthCheck;

        /// <summary>
        /// The last subscription ID that was issued.
        /// </summary>
        private int _lastSubscriptionId;

        /// <summary>
        /// The active subscriptions.
        /// </summary>
        private readonly ConcurrentDictionary<int, SubscriberRegistration> _subscriptions = new ConcurrentDictionary<int, SubscriberRegistration>();

        /// <summary>
        /// Lock to ensure that only a single call to <see cref="CheckHealthAsync"/> is ongoing.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncLock _updateLock = new Nito.AsyncEx.AsyncLock();

        /// <summary>
        /// A channel that is used to inform the <see cref="HealthCheckManager{TAdapterOptions}"/> that it should 
        /// recompute the adapter health status.
        /// </summary>
        private readonly Channel<bool> _recomputeHealthChannel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite
        });

        /// <inheritdoc/>
        public IBackgroundTaskService BackgroundTaskService => _adapter.BackgroundTaskService;


        /// <summary>
        /// Creates a new <see cref="HealthCheckManager{TAdapterOptions}"/> object.
        /// </summary>
        /// <param name="adapter">
        ///   The owning adapter.
        /// </param>
        internal HealthCheckManager(AdapterBase<TAdapterOptions> adapter) {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _logger = adapter.LoggerFactory.CreateLogger("DataCore.Adapter.Diagnostics.HealthCheckManager");
        }


        /// <summary>
        /// Initialises the <see cref="HealthCheckManager{TAdapterOptions}"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will initialise the <see cref="HealthCheckManager{TAdapterOptions}"/> and 
        ///   get the initial health status.
        /// </returns>
        internal async Task InitAsync(CancellationToken cancellationToken) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            await CheckHealthAsync(new DefaultAdapterCallContext(), cancellationToken).ConfigureAwait(false);
            _adapter.BackgroundTaskService.QueueBackgroundWorkItem(ProcessRecomputeHealthChannelAsync);
        }


        /// <summary>
        /// Tells the <see cref="HealthCheckManager{TAdapterOptions}"/> that the health status of the adapter should 
        /// be recalculated.
        /// </summary>
        internal void RecalculateHealthStatus() {
            _recomputeHealthChannel.Writer.TryWrite(true);
        }


        /// <summary>
        /// Long-running task that monitors the <see cref="_recomputeHealthChannel"/>, 
        /// recalculates the adapter health when messages are published to the channel, and then 
        /// publishes the updated health status to subscribers.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that can be used to stop processing of the queue.
        /// </param>
        /// <returns>
        ///   A task that will complete when the cancellation token fires.
        /// </returns>
        private async Task ProcessRecomputeHealthChannelAsync(CancellationToken cancellationToken) {
            using var loggerScope = BeginLoggerScope();

            while (await _recomputeHealthChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!_recomputeHealthChannel.Reader.TryRead(out var val) || !val) {
                    continue;
                }

                var update = await CheckHealthAsyncCore(
                    new DefaultAdapterCallContext(),
                    cancellationToken
                ).ConfigureAwait(false);

                await PublishToHealthCheckSubscribers(update, cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Publishes the specified health check update to all subscribers.
        /// </summary>
        /// <param name="update">
        ///   The health check update.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will publish the update to the subscribers.
        /// </returns>
        private async ValueTask PublishToHealthCheckSubscribers(HealthCheckResultWithSequenceId update, CancellationToken cancellationToken) {
            var subscribers = _subscriptions.Values;

            if (subscribers.Count == 0) {
                return;
            }

            foreach (var registration in subscribers) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }

                try {
                    if (await registration.PublishAsync(update, false).ConfigureAwait(false)) {
                        LogPublishToSubscriberSucceeded(_logger, registration.Subscriber.Context?.ConnectionId);
                    }
                    else {
                        LogPublishToSubscriberFailed(_logger, registration.Subscriber.Context?.ConnectionId);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception e) {
                    LogPublishToSubscriberFaulted(_logger, e, registration.Subscriber.Context?.ConnectionId);
                }
            }
        }


        /// <summary>
        /// Checks the health of the adapter and updates the <see cref="_latestHealthCheck"/>.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">
        ///   The <see cref="HealthCheckManager{TAdapterOptions}"/> has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        private async Task<HealthCheckResultWithSequenceId> CheckHealthAsyncCore(IAdapterCallContext context, CancellationToken cancellationToken) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            using (await _updateLock.LockAsync(cancellationToken).ConfigureAwait(false)) {
                try {
                    var results = await _adapter.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false);
                    if (results == null || !results.Any()) {
                        _latestHealthCheck = new HealthCheckResultWithSequenceId(HealthCheckResult.Healthy(Resources.HealthChecks_DisplayName_OverallAdapterHealth, Resources.HealthChecks_CompositeResultDescription_Healthy));
                        return _latestHealthCheck.Value;
                    }

                    var resultsArray = results.ToArray();

                    var compositeStatus = HealthCheckResult.GetAggregateHealthStatus(resultsArray.Select(x => x.Status));
                    string description;

                    switch (compositeStatus) {
                        case HealthStatus.Unhealthy:
                            description = Resources.HealthChecks_CompositeResultDescription_Unhealthy;
                            break;
                        case HealthStatus.Degraded:
                            description = Resources.HealthChecks_CompositeResultDescription_Degraded;
                            break;
                        default:
                            description = Resources.HealthChecks_CompositeResultDescription_Healthy;
                            break;
                    }

                    _latestHealthCheck = new HealthCheckResultWithSequenceId(new HealthCheckResult(Resources.HealthChecks_DisplayName_OverallAdapterHealth, compositeStatus, description, null, null, resultsArray));
                    return _latestHealthCheck.Value;
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception e) {
                    return new HealthCheckResultWithSequenceId(HealthCheckResult.Unhealthy(Resources.HealthChecks_DisplayName_OverallAdapterHealth, Resources.HealthChecks_CompositeResultDescription_Error, e.Message));
                }
            }
        }


        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            var result = _latestHealthCheck ?? await CheckHealthAsyncCore(context, cancellationToken).ConfigureAwait(false);
            return result.Result;
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<HealthCheckResult> Subscribe(
            IAdapterCallContext context,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            var subscriptionId = Interlocked.Increment(ref _lastSubscriptionId);
            var subscription = new SubscriptionChannel<string, HealthCheckResult>(
                subscriptionId,
                context,
                _adapter.BackgroundTaskService,
                TimeSpan.Zero,
                new[] { _adapter.StopToken, cancellationToken },
                () => {
                    OnHealthCheckSubscriptionCancelled(subscriptionId);
                    return default;
                },
                10
            );

            var registration = new SubscriberRegistration(subscription);
            _subscriptions[subscriptionId] = registration;

            var latestResult = _latestHealthCheck;
            if (latestResult != null) {
                await registration.PublishAsync(latestResult.Value, true).ConfigureAwait(false);
            }

            await foreach (var item in subscription.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Called when a subscription is cancelled.
        /// </summary>
        /// <param name="subscriptionId">
        ///   The subscription ID.
        /// </param>
        private void OnHealthCheckSubscriptionCancelled(int subscriptionId) {
            if (_isDisposed) {
                return;
            }

            if (!_subscriptions.TryRemove(subscriptionId, out var subscription)) {
                return;
            }

            subscription.Subscriber.Dispose();
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            foreach (var subscription in _subscriptions.Values.ToArray()) {
                subscription.Subscriber.Dispose();
            }
            _subscriptions.Clear();
            _isDisposed = true;
        }


        private IDisposable? BeginLoggerScope() => AdapterCore.BeginLoggerScope(_logger, _adapter);


        [LoggerMessage(1, LogLevel.Trace, "Publish to subscriber '{subscriberId}' succeeded.")]
        static partial void LogPublishToSubscriberSucceeded(ILogger logger, string? subscriberId);


        [LoggerMessage(2, LogLevel.Trace, "Publish to subscriber '{subscriberId}' failed.")]
        static partial void LogPublishToSubscriberFailed(ILogger logger, string? subscriberId);


        [LoggerMessage(3, LogLevel.Error, "Publish to subscriber '{subscriberId}' faulted.")]
        static partial void LogPublishToSubscriberFaulted(ILogger logger, Exception e, string? subscriberId);


        /// <summary>
        /// Wraps a <see cref="HealthCheckResult"/> to include a sequence ID as well, so that a 
        /// subscription can choose to ignore the result if it is older than the most-recent 
        /// result received by the subscriber.
        /// </summary>
        private readonly struct HealthCheckResultWithSequenceId {

            /// <summary>
            /// The sequence ID of the most-recent <see cref="HealthCheckResultWithSequenceId"/>.
            /// </summary>
            private static long s_lastSequenceId;
        
            /// <summary>
            /// The <see cref="HealthCheckResult"/>.
            /// </summary>
            public HealthCheckResult Result { get; }

            /// <summary>
            /// The sequence ID for the result.
            /// </summary>
            public long SequenceId { get; }


            /// <summary>
            /// Creates a new <see cref="HealthCheckResultWithSequenceId"/> instance.
            /// </summary>
            /// <param name="result">
            ///   The <see cref="HealthCheckResult"/>.
            /// </param>
            public HealthCheckResultWithSequenceId(HealthCheckResult result) {
                Result = result;
                SequenceId = Interlocked.Increment(ref s_lastSequenceId);
            }

        }


        /// <summary>
        /// Wrapper for a health check subscription channel that will only accept updates that are 
        /// newer than the last update published to the channel.
        /// </summary>
        private class SubscriberRegistration {

            /// <summary>
            /// Ensures that only one publish can occur at a time.
            /// </summary>
            private readonly Nito.AsyncEx.AsyncLock _publishLock = new Nito.AsyncEx.AsyncLock();

            /// <summary>
            /// The subscription channel.
            /// </summary>
            public SubscriptionChannel<string, HealthCheckResult> Subscriber { get; }

            /// <summary>
            /// The sequence ID of the last update that was published to the <see cref="Subscriber"/>.
            /// </summary>
            public long? LastSequenceId { get; private set; }


            /// <summary>
            /// Creates a new <see cref="SubscriberRegistration"/> instance.
            /// </summary>
            /// <param name="subscriber">
            ///   The subscription channel.
            /// </param>
            public SubscriberRegistration(SubscriptionChannel<string, HealthCheckResult> subscriber) {
                Subscriber = subscriber;
            }


            /// <summary>
            /// Publishes a health check result to the subscriber.
            /// </summary>
            /// <param name="message">
            ///   The health check result.
            /// </param>
            /// <param name="immediate">
            ///   Specifies if the message should be published immediately to the subscriber, even 
            ///   if it normally emits new updates on a periodic basis.
            /// </param>
            /// <returns>
            ///   <see langword="true"/> if the <paramref name="message"/> was successfully 
            ///   published, or <see langword="false"/> otherwise.
            /// </returns>
            public async ValueTask<bool> PublishAsync(HealthCheckResultWithSequenceId message, bool immediate) {
                using (await _publishLock.LockAsync().ConfigureAwait(false)) {
                    // LastSequenceId.Value > 0 && message.SequenceId < 0 clause here handles
                    // scenarios where the sequence ID has wrapped around from long.MaxValue to
                    // long.MinValue.
                    var canPublish = !LastSequenceId.HasValue || message.SequenceId > LastSequenceId.Value || LastSequenceId.Value > 0 && message.SequenceId < 0;
                    if (!canPublish) {
                        return false;
                    }

                    LastSequenceId = message.SequenceId;
                    return Subscriber.Publish(message.Result, immediate);
                }
            }

        }

    }
}
