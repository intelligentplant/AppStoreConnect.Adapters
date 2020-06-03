using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.RealTimeData;
using DataCore.Adapter.RealTimeData;
using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for requesting tag values, including pushing real-time snapshot value 
    // changes to subscribers. Snapshot push is only supported on adapters that implement the 
    // ISnapshotTagValuePush feature.

    public partial class AdapterHub {

        #region [ Snapshot Subscription Management ]

        /// <summary>
        /// Holds all active snapshot subscriptions. First index is by connection ID; second index 
        /// is by adapter ID.
        /// </summary>
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Task<SnapshotSubscriptionWrapper>>> s_snapshotSubscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<string, Task<SnapshotSubscriptionWrapper>>>();


        /// <summary>
        /// Invoked when a client disconnects.
        /// </summary>
        partial void OnTagValuesHubDisconnection() {
            if (s_snapshotSubscriptions.TryRemove(Context.ConnectionId, out var s)) {
                foreach (var item in s.Values) {
                    try {
                        item.Result.Dispose();
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                }
            }
        }


        /// <summary>
        /// Creates a snapshot tag value subscription.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="tagIdOrName">
        ///   The tag ID or name to subscribe to.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the subscription is no longer required.
        /// </param>
        /// <returns>
        ///   A channel reader that the subscriber can observe to receive new tag values.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> CreateSnapshotTagValueChannel(
            string adapterId, 
            string tagIdOrName, 
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(tagIdOrName)) {
                throw new ArgumentException(string.Empty, nameof(tagIdOrName));
            }

            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ISnapshotTagValuePush>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);

            // Create the subscription.
            var subscriptionsForConnection = s_snapshotSubscriptions.GetOrAdd(Context.ConnectionId, k => new ConcurrentDictionary<string, Task<SnapshotSubscriptionWrapper>>());
            var subscription = await subscriptionsForConnection.GetOrAdd(adapterId, k => Task.Run(async () => {
                var sub = await adapter.Feature.Subscribe(adapterCallContext).ConfigureAwait(false);
                return new SnapshotSubscriptionWrapper(sub, TaskScheduler);
            }, cancellationToken)).ConfigureAwait(false);

            var result = Channel.CreateUnbounded<TagValueQueryResult>(new UnboundedChannelOptions() { 
                SingleReader = true,
                SingleWriter = true
            });

            var tagSubscription = await subscription.AddSubscription(tagIdOrName).ConfigureAwait(false);

            TaskScheduler.QueueBackgroundWorkItem(async ct => { 
                try {
                    await tagSubscription.Reader.Forward(result, ct).ConfigureAwait(false);
                }
                finally {
                    tagSubscription.Dispose();
                }
            }, cancellationToken);

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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadSnapshotTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return await adapter.Feature.ReadSnapshotTagValues(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadRawTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return await adapter.Feature.ReadRawTagValues(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadPlotTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return await adapter.Feature.ReadPlotTagValues(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadTagValuesAtTimes>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return await adapter.Feature.ReadTagValuesAtTimes(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadProcessedTagValues>(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            return await adapter.Feature.GetSupportedDataFunctions(adapterCallContext, cancellationToken).ConfigureAwait(false);
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadProcessedTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return await adapter.Feature.ReadProcessedTagValues(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IWriteSnapshotTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            return await adapter.Feature.WriteSnapshotTagValues(adapterCallContext, channel, cancellationToken).ConfigureAwait(false);
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IWriteHistoricalTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            return await adapter.Feature.WriteHistoricalTagValues(adapterCallContext, channel, cancellationToken).ConfigureAwait(false);
        }

        #endregion

    }
}
