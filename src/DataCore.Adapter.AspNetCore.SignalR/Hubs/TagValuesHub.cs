using System;
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
        /// Holds subscriptions for all connections.
        /// </summary>
        private static readonly ConnectionSubscriptionManager<TagValueQueryResult, TopicSubscriptionWrapper<TagValueQueryResult>> s_snapshotSubscriptions = new ConnectionSubscriptionManager<TagValueQueryResult, TopicSubscriptionWrapper<TagValueQueryResult>>();


        /// <summary>
        /// Invoked when a client disconnects.
        /// </summary>
        partial void OnTagValuesHubDisconnection() {
            s_snapshotSubscriptions.RemoveAllSubscriptions(Context.ConnectionId);
        }


        /// <summary>
        /// Creates a snapshot tag value subscription. Note that this does not add any tags to the 
        /// subscription; this must be done separately via calls to <see cref="CreateSnapshotTagValueChannel"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to subscribe to.
        /// </param>
        /// <param name="request">
        ///   The subscription request parameters.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return the ID for the subscription.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Subscription lifecycle is managed externally to this method")]
        public async Task<string> CreateSnapshotTagValueSubscription(
            string adapterId,
            CreateSnapshotTagValueSubscriptionRequest request
        ) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ISnapshotTagValuePush>(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);

            var wrappedSubscription = new TopicSubscriptionWrapper<TagValueQueryResult>(
                await adapter.Feature.Subscribe(adapterCallContext, request).ConfigureAwait(false),
                TaskScheduler
            );

            return s_snapshotSubscriptions.AddSubscription(Context.ConnectionId, wrappedSubscription);
        }


        /// <summary>
        /// Deletes a snapshot tag value subscription. This will cancel all active calls to 
        /// <see cref="CreateSnapshotTagValueChannel"/> for the subscription.
        /// </summary>
        /// <param name="subscriptionId">
        ///   The subscription ID. Specify <see langword="null"/> to delete all subscriptions for 
        ///   the connection.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a flag indicating if the operation 
        ///   was successful.
        /// </returns>
        public Task<bool> DeleteSnapshotTagValueSubscription(
            string subscriptionId    
        ) {
            if (string.IsNullOrWhiteSpace(subscriptionId)) {
                s_snapshotSubscriptions.RemoveAllSubscriptions(Context.ConnectionId);
                return Task.FromResult(true);
            }
            var result = s_snapshotSubscriptions.RemoveSubscription(Context.ConnectionId, subscriptionId);
            return Task.FromResult(result);
        }



        /// <summary>
        /// Subscribes to receive snapshot tag values for a tag.
        /// </summary>
        /// <param name="subscriptionId">
        ///   The subscription ID to add the tag to.
        /// </param>
        /// <param name="tagIdOrName">
        ///   The tag ID or name to subscribe to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a channel that emits value changes 
        ///   for the tag.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exceptions are written to the response channel")]
        public Task<ChannelReader<TagValueQueryResult>> CreateSnapshotTagValueChannel(
            string subscriptionId,
            string tagIdOrName,
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(subscriptionId)) {
                throw new ArgumentException(Resources.Error_SubscriptionIdRequired, nameof(subscriptionId));
            }
            if (string.IsNullOrWhiteSpace(tagIdOrName)) {
                throw new ArgumentException(Resources.Error_TagNameOrIdRequired, nameof(tagIdOrName));
            }

            if (!s_snapshotSubscriptions.TryGetSubscription(Context.ConnectionId, subscriptionId, out var subscription)) {
                throw new ArgumentException(Resources.Error_SubscriptionDoesNotExist, nameof(subscriptionId));
            }

            var result = Channel.CreateUnbounded<TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var topicChannel = await subscription.CreateTopicChannel(tagIdOrName).ConfigureAwait(false);
                try {
                    while (!ct.IsCancellationRequested) {
                        try {
                            var val = await topicChannel.Reader.ReadAsync(ct).ConfigureAwait(false);
                            await ch.WriteAsync(val, ct).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { }
                        catch (ChannelClosedException) { }
                    }
                }
                catch (Exception e) {
                    topicChannel.Writer.TryComplete(e);
                }
                finally {
                    await topicChannel.DisposeAsync().ConfigureAwait(false);
                }
            }, true, TaskScheduler, cancellationToken);

            return Task.FromResult(result.Reader);
        }

        #endregion

        #region [ Polling Data Queries ]

        /// <summary>
        /// Gets snapshot tag values via polling. Use <see cref="CreateSnapshotTagValueSubscription"/> 
        /// and <see cref="CreateSnapshotTagValueChannel"/> to receive snapshot tag 
        /// values via push messages.
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
