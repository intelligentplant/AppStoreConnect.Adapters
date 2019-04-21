﻿using System;
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
        /// Cancellation token source that fires when the object is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();


        /// <summary>
        /// Creates a mew <see cref="PollingSnapshotTagValueSubscriptionManager"/> object.
        /// </summary>
        /// <param name="pollingInterval">
        ///   The interval between polling queries. If less than or equal to <see cref="TimeSpan.Zero"/>, 
        ///   <see cref="DefaultPollingInterval"/> will be used.
        /// </param>
        protected PollingSnapshotTagValueSubscriptionManager(TimeSpan pollingInterval) : base() {
            _pollingInterval = pollingInterval <= TimeSpan.Zero
                ? DefaultPollingInterval
                : pollingInterval;

            // Run the dedicated polling task.
            _ = Task.Factory.StartNew(() => RunSnapshotPollingLoop(_disposedTokenSource.Token), TaskCreationOptions.LongRunning);
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
            try {
                while (!cancellationToken.IsCancellationRequested) {
                    await Task.Delay(_pollingInterval, cancellationToken).ConfigureAwait(false);

                    try {
                        var tags = await GetSubscribedTags(cancellationToken).ConfigureAwait(false);
                        if (tags == null || !tags.Any()) {
                            return;
                        }
                        var values = await GetSnapshotTagValues(tags, cancellationToken).ConfigureAwait(false);
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
            catch (OperationCanceledException) {
                // Cancellation token fired.
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
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
            }
        }
    }
}
