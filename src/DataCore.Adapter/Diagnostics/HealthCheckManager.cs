using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Default <see cref="IHealthCheck"/> implementation.
    /// </summary>
    internal class HealthCheckManager : IBackgroundTaskServiceProvider, IHealthCheck, IDisposable {

        /// <summary>
        /// Indicates if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The owning adapter.
        /// </summary>
        private readonly AdapterBase _adapter;

        /// <summary>
        /// The most recent health check that was performed.
        /// </summary>
        private HealthCheckResult? _latestHealthCheck;

        /// <summary>
        /// The last subscription ID that was issued.
        /// </summary>
        private int _lastSubscriptionId;

        /// <summary>
        /// The active subscriptions.
        /// </summary>
        private readonly ConcurrentDictionary<int, SubscriptionChannel<string, HealthCheckResult>> _subscriptions = new ConcurrentDictionary<int, SubscriptionChannel<string, HealthCheckResult>>();

        /// <summary>
        /// Lock to ensure that only a single call to <see cref="CheckHealthAsync"/> is ongoing.
        /// </summary>
        private readonly SemaphoreSlim _updateLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// A channel that is used to inform the <see cref="HealthCheckManager"/> that it should 
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
        /// Creates a new <see cref="HealthCheckManager"/> object.
        /// </summary>
        /// <param name="adapter">
        ///   The owning adapter.
        /// </param>
        internal HealthCheckManager(AdapterBase adapter) {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }


        /// <summary>
        /// Initialises the <see cref="HealthCheckManager"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will initialise the <see cref="HealthCheckManager"/> and 
        ///   get the initial health status.
        /// </returns>
        internal async Task Init(CancellationToken cancellationToken) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            await CheckHealthAsync(new DefaultAdapterCallContext(), cancellationToken).ConfigureAwait(false);
            _adapter.BackgroundTaskService.QueueBackgroundWorkItem(PublishToHealthCheckSubscribers);
        }


        /// <summary>
        /// Tells the <see cref="HealthCheckManager"/> that the health status of the adapter should 
        /// be recalculated.
        /// </summary>
        internal void RecalculateHealthStatus() {
            _recomputeHealthChannel.Writer.TryWrite(true);
        }


        /// <summary>
        /// Long-running task that monitors the <see cref="_recomputeHealthChannel"/>, 
        /// recalculates the adapter health when messages are published to the channel, and then 
        /// publishes he health status to subscribers.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that can be used to stop processing of the queue.
        /// </param>
        /// <returns>
        ///   A task that will complete when the cancellation token fires
        /// </returns>
        private async Task PublishToHealthCheckSubscribers(CancellationToken cancellationToken) {
            while (await _recomputeHealthChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!_recomputeHealthChannel.Reader.TryRead(out var val) || !val) {
                    continue;
                }

                var subscribers = _subscriptions.Values.ToArray();

                var update = await CheckHealthAsync(
                    new DefaultAdapterCallContext(),
                    cancellationToken
                ).ConfigureAwait(false);

                _latestHealthCheck = update;

                if (subscribers.Length == 0) {
                    continue;
                }

                foreach (var subscriber in subscribers) {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }

                    try {
                        var success = subscriber.Publish(update);
                        if (!success) {
                            _adapter.Logger.LogTrace(Resources.Log_PublishToSubscriberWasUnsuccessful, subscriber.Context?.ConnectionId);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception e) {
                        _adapter.Logger.LogError(e, Resources.Log_PublishToSubscriberThrewException, subscriber.Context?.ConnectionId);
                    }
                }
            }
        }


        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            await _updateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                var results = await _adapter.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false);
                if (results == null || !results.Any()) {
                    return HealthCheckResult.Healthy(Resources.HealthChecks_DisplayName_OverallAdapterHealth, Resources.HealthChecks_CompositeResultDescription_Healthy);
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

                var overallResult = new HealthCheckResult(Resources.HealthChecks_DisplayName_OverallAdapterHealth, compositeStatus, description, null, null, resultsArray);
                _latestHealthCheck = overallResult;

                return overallResult;
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                return HealthCheckResult.Unhealthy(Resources.HealthChecks_DisplayName_OverallAdapterHealth, Resources.HealthChecks_CompositeResultDescription_Error, e.Message);
            }
            finally {
                _updateLock.Release();
            }
        }


        /// <inheritdoc/>
        public Task<ChannelReader<HealthCheckResult>> Subscribe(
            IAdapterCallContext context, 
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
                () => OnHealthCheckSubscriptionCancelled(subscriptionId),
                10
            );

            var latestResult = _latestHealthCheck;
            if (latestResult != null) {
                subscription.Publish(latestResult.Value, true);
            }

            _subscriptions[subscriptionId] = subscription;

            return Task.FromResult(subscription.Reader);
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

            subscription.Dispose();
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _updateLock.Dispose();
            foreach (var subscription in _subscriptions.Values.ToArray()) {
                subscription.Dispose();
            }
            _subscriptions.Clear();
            _isDisposed = true;
        }

    }
}
