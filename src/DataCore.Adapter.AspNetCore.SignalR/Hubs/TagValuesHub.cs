using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for requesting tag values, including pushing real-time snapshot value 
    // changes to subscribers. Snapshot push is only supported on adapters that implement the 
    // ISnapshotTagValuePush feature.

    public partial class AdapterHub {

        #region [ Snapshot Subscription Management ]

        /// <summary>
        /// Creates a new snapshot push subscription on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="subscriptionChanges">
        ///   A channel that subscription changes will be published to.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the subscription is no longer required.
        /// </param>
        /// <returns>
        ///   A channel reader that the subscriber can observe to receive new tag values.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> CreateSnapshotTagValueChannel(
            string adapterId, 
            ChannelReader<UpdateSnapshotTagValueSubscriptionRequest> subscriptionChanges, 
            CancellationToken cancellationToken
        ) {
            // Resolve the adapter and feature.
            var adapter = await ResolveAdapterAndFeature<ISnapshotTagValuePush>(adapterId, cancellationToken).ConfigureAwait(false);

            // Create the subscription.
            var subscription = await adapter.Feature.Subscribe(AdapterCallContext).ConfigureAwait(false);

            var result = Channel.CreateUnbounded<TagValueQueryResult>();

            // Send a "subscription ready" event so that the caller knows that the stream is 
            // now up-and-running at this end.
            var onReady = new TagValueQueryResult(
                string.Empty,
                string.Empty,
                TagValueBuilder
                    .Create()
                    .WithValue(subscription.Id)
                    .Build()
            );
            await result.Writer.WriteAsync(onReady);

            // Run background operation to forward values emitted from the subscription.
            subscription.Reader.RunBackgroundOperation(async (ch, ct) => {
                while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    if (!ch.TryRead(out var item) || item == null) {
                        continue;
                    }

                    await result.Writer.WriteAsync(item, ct).ConfigureAwait(false);
                }
            }, TaskScheduler, cancellationToken);

            // Run background operation to push incoming changes to the subscription.
            subscriptionChanges.RunBackgroundOperation(async (ch, ct) => { 
                try {
                    while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!ch.TryRead(out var item) || item == null) {
                            continue;
                        }

                        if (item.Action == Common.SubscriptionUpdateAction.Subscribe) {
                            await subscription.AddTagToSubscription(item.Tag).ConfigureAwait(false);
                        }
                        else {
                            await subscription.RemoveTagFromSubscription(item.Tag).ConfigureAwait(false);
                        }
                    }
                }
                finally {
                    subscription.Dispose();
                }
            }, TaskScheduler, cancellationToken); 
            
            // Return the output channel for the subscription.
            return result;
        }

        #endregion

        #region [ Polling Data Queries ]

        /// <summary>
        /// Gets snapshot tag values via polling. Use <see cref="CreateSnapshotTagValueChannel"/> 
        /// to receive snapshot tag values via push messages.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the query results.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> ReadSnapshotTagValues(string adapterId, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadSnapshotTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadSnapshotTagValues(AdapterCallContext, request, cancellationToken);
        }


        /// <summary>
        /// Gets raw tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the query results.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> ReadRawTagValues(string adapterId, ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadRawTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadRawTagValues(AdapterCallContext, request, cancellationToken);
        }


        /// <summary>
        /// Gets visualization-friendly tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the query results.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> ReadPlotTagValues(string adapterId, ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadPlotTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadPlotTagValues(AdapterCallContext, request, cancellationToken);
        }


        /// <summary>
        /// Gets tag values at the specified times.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the query results.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> ReadTagValuesAtTimes(string adapterId, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadTagValuesAtTimes>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadTagValuesAtTimes(AdapterCallContext, request, cancellationToken);
        }


        /// <summary>
        /// Gets the data functions supported by <see cref="ReadProcessedTagValues(string, ReadProcessedTagValuesRequest, CancellationToken)"/> 
        /// queries.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The supported data functions for processed data queries.
        /// </returns>
        public async Task<ChannelReader<DataFunctionDescriptor>> GetSupportedDataFunctions(string adapterId, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadProcessedTagValues>(adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            return adapter.Feature.GetSupportedDataFunctions(AdapterCallContext, cancellationToken);
        }


        /// <summary>
        /// Gets processed tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the query results.
        /// </returns>
        public async Task<ChannelReader<ProcessedTagValueQueryResult>> ReadProcessedTagValues(string adapterId, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadProcessedTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadProcessedTagValues(AdapterCallContext, request, cancellationToken);
        }

        #endregion

        #region [ Tag Value Write ]

        /// <summary>
        /// Writes values to the specified adapter's snapshot.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="channel">
        ///   A channel that will provide the values to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the write results.
        /// </returns>
        public async Task<ChannelReader<WriteTagValueResult>> WriteSnapshotTagValues(string adapterId, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IWriteSnapshotTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            return adapter.Feature.WriteSnapshotTagValues(AdapterCallContext, channel, cancellationToken);
        }


        /// <summary>
        /// Writes values to the specified adapter's historical archive.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="channel">
        ///   A channel that will provide the values to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the write results.
        /// </returns>
        public async Task<ChannelReader<WriteTagValueResult>> WriteHistoricalTagValues(string adapterId, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IWriteHistoricalTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            return adapter.Feature.WriteHistoricalTagValues(AdapterCallContext, channel, cancellationToken);
        }

        #endregion

    }
}
