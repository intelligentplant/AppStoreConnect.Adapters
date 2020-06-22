using System;
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
    internal class HealthCheckManager : IHealthCheck, IDisposable {

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
        /// The active subscriptions.
        /// </summary>
        private readonly HashSet<HealthCheckSubscription> _subscriptions = new HashSet<HealthCheckSubscription>();

        /// <summary>
        /// Lock for accessing <see cref="_latestHealthCheck"/> and <see cref="_subscriptions"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _subscriptionsLock = new ReaderWriterLockSlim();

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
            _adapter.TaskScheduler.QueueBackgroundWorkItem(PublishToHealthCheckSubscribers, _adapter.StopToken);
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

                HealthCheckSubscription[] subscribers;

                _subscriptionsLock.EnterReadLock();
                try {
                    subscribers = _subscriptions.ToArray();
                }
                finally {
                    _subscriptionsLock.ExitReadLock();
                }

                var update = await CheckHealthAsync(
                    new DefaultAdapterCallContext(),
                    cancellationToken
                ).ConfigureAwait(false);

                if (subscribers.Length == 0) {
                    // No subscribers; no point recomputing health status.
                    continue;
                }

                foreach (var subscriber in subscribers) {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }

                    try {
                        var success = await subscriber.ValueReceived(update, cancellationToken).ConfigureAwait(false);
                        if (!success) {
                            _adapter.Logger.LogTrace(Resources.Log_PublishToSubscriberWasUnsuccessful, subscriber.Context?.ConnectionId);
                        }
                    }
                    catch (OperationCanceledException) { }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
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
                _subscriptionsLock.EnterWriteLock();
                try {
                    _latestHealthCheck = overallResult;
                }
                finally {
                    _subscriptionsLock.ExitWriteLock();
                }

                return overallResult;
            }
            catch (OperationCanceledException) {
                throw;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                return HealthCheckResult.Unhealthy(Resources.HealthChecks_DisplayName_OverallAdapterHealth, Resources.HealthChecks_CompositeResultDescription_Error, e.Message);
            }
            finally {
                _updateLock.Release();
            }
        }


        /// <inheritdoc/>
        public async Task<IHealthCheckSubscription> Subscribe(IAdapterCallContext context) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            var subscription = new HealthCheckSubscription(context, this);

            bool added;
            HealthCheckResult? latestResult;

            _subscriptionsLock.EnterWriteLock();
            try {
                added = _subscriptions.Add(subscription);
                latestResult = _latestHealthCheck;
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }

            if (added) {
                await subscription.Start().ConfigureAwait(false);
                if (latestResult != null) {
                    await subscription.ValueReceived(latestResult.Value).ConfigureAwait(false);
                }
            }

            return subscription;
        }


        /// <summary>
        /// Called when a subscription is cancelled.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        private void OnHealthCheckSubscriptionCancelled(HealthCheckSubscription subscription) {
            if (_isDisposed) {
                return;
            }

            _subscriptionsLock.EnterWriteLock();
            try {
                _subscriptions.Remove(subscription);
            }
            finally {
                _subscriptionsLock.ExitWriteLock();
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _subscriptionsLock.Dispose();
            _updateLock.Dispose();
            _subscriptions.Clear();
            _isDisposed = true;
        }


        /// <summary>
        /// <see cref="IHealthCheckSubscription"/> implementation.
        /// </summary>
        private class HealthCheckSubscription : AdapterSubscription<HealthCheckResult>, IHealthCheckSubscription {

            /// <summary>
            /// The subscribed adapter.
            /// </summary>
            private readonly HealthCheckManager _manager;


            /// <summary>
            /// Creates a new <see cref="HealthCheckSubscription"/> object.
            /// </summary>
            /// <param name="context">
            ///   The <see cref="IAdapterCallContext"/> for the subscriber.
            /// </param>
            /// <param name="manager">
            ///   The <see cref="HealthCheckManager"/> that the caller is subscribing to.
            /// </param>
            internal HealthCheckSubscription(
                IAdapterCallContext context,
                HealthCheckManager manager
            ) : base(context, manager?._adapter.Descriptor.Id) {
                _manager = manager;
            }


            /// <inheritdoc/>
            protected override void OnCancelled() {
                _manager.OnHealthCheckSubscriptionCancelled(this);
            }

        }

    }
}
