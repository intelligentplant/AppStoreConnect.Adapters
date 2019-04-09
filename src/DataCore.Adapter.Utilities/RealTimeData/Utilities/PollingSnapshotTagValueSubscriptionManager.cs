using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Extends <see cref="SnapshotTagValueSubscriptionManager"/> to provide a snapshot subscription 
    /// manager that periodically polls for the values of tags that are currently being subscribed to.
    /// </summary>
    public abstract class PollingSnapshotTagValueSubscriptionManager : SnapshotTagValueSubscriptionManager {

        /// <summary>
        /// Default polling interval to use.
        /// </summary>
        public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The polling interval.
        /// </summary>
        private readonly TimeSpan _pollingInterval;

        /// <summary>
        /// A timer that will kick off the snapshot polling queries.
        /// </summary>
        private readonly Timer _timer;

        /// <summary>
        /// Signals to the dedicated snapshot polling task that it is time for another query.
        /// </summary>
        private readonly SemaphoreSlim _readValues = new SemaphoreSlim(0);

        /// <summary>
        /// Cancellation token source that fires when the object is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();


        /// <summary>
        /// Creates a mew <see cref="PollingSnapshotTagValueSubscriptionManager"/> object.
        /// </summary>
        /// <param name="adapterDescriptor">
        ///   The descriptor for the adapter that the subscription manager belongs to.
        /// </param>
        /// <param name="backgroundTaskQueue">
        ///   The background task scheduler to use.
        /// </param>
        /// <param name="pollingInterval">
        ///   The interval between polling queries. If less than or equal to <see cref="TimeSpan.Zero"/>, 
        ///   <see cref="DefaultPollingInterval"/> will be used.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterDescriptor"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="backgroundTaskQueue"/> is <see langword="null"/>.
        /// </exception>
        protected PollingSnapshotTagValueSubscriptionManager(AdapterDescriptor adapterDescriptor, IBackgroundTaskQueue backgroundTaskQueue, TimeSpan pollingInterval) : base(adapterDescriptor, backgroundTaskQueue) {
            _pollingInterval = pollingInterval <= TimeSpan.Zero
                ? DefaultPollingInterval
                : pollingInterval;

            _timer = new Timer(OnTimerFired, null, TimeSpan.Zero, _pollingInterval);
            // Run the dedicated polling task.
            BackgroundTaskQueue.QueueBackgroundWorkItem(ct => RunSnapshotPollingLoop(ct));
        }


        /// <summary>
        /// Callback handler for the polling <see cref="_timer"/>.
        /// </summary>
        /// <param name="state">
        ///   The callback state. Not used.
        /// </param>
        private void OnTimerFired(object state) {
            if (_disposedTokenSource.IsCancellationRequested) {
                return;
            }

            // Let the polling loop know that it is time to read values again.
            _readValues.Release();
        }


        /// <summary>
        /// Runs the dedicated polling task.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The system shutdown token.
        /// </param>
        /// <returns>
        ///   A task that will run the polling loop.
        /// </returns>
        private async Task RunSnapshotPollingLoop(CancellationToken cancellationToken) {
            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposedTokenSource.Token)) {
                while (!ctSource.Token.IsCancellationRequested) {
                    // Wait until the semaphore flags that it is time to poll.
                    await _readValues.WaitAsync(ctSource.Token).ConfigureAwait(false);

                    try {
                        var tags = await GetSubscribedTags(ctSource.Token).ConfigureAwait(false);
                        if (tags == null || !tags.Any()) {
                            return;
                        }
                        var values = await GetSnapshotTagValues(tags, ctSource.Token).ConfigureAwait(false);
                        if (values == null || !values.Any()) {
                            continue;
                        }
                        OnValuesChanged(values);
                    }
                    catch (OperationCanceledException) {
                        // Cancellation token fired.
                    }
                    catch (Exception e) {
                        // TODO: logging of errors!
                    }
                }
            }
        }


        /// <summary>
        /// Gets the snapshot values for the specified tags.
        /// </summary>
        /// <param name="tagIds">
        ///   The tag IDs to query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The snapshot values for the tags.
        /// </returns>
        protected abstract Task<IEnumerable<SnapshotTagValue>> GetSnapshotTagValues(IEnumerable<string> tagIds, CancellationToken cancellationToken);


        /// <inheritdoc/>
        protected override async Task<IEnumerable<SnapshotTagValue>> OnSubscribe(IEnumerable<string> tagIds, CancellationToken cancellationToken) {
            return await GetSnapshotTagValues(tagIds, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override Task OnUnsubscribe(IEnumerable<string> tagIds, CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _timer.Dispose();
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
                _readValues.Dispose();
            }
        }
    }
}
