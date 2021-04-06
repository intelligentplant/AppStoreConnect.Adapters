using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.RealTimeData;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for requesting tag values, including pushing real-time snapshot value 
    // changes to subscribers. Snapshot push is only supported on adapters that implement the 
    // ISnapshotTagValuePush feature.

    public partial class AdapterHub {

        #region [ Snapshot Subscription Management ]

#if NETSTANDARD2_0 == false

        /// <summary>
        /// Creates a snapshot tag value subscription.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to subscribe to.
        /// </param>
        /// <param name="request">
        ///   The subscription request parameters.
        /// </param>
        /// <param name="channel">
        ///   A channel that can be used to publish subscription changes.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the subscription.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return the channel reader for the subscription.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> CreateSnapshotTagValueChannel(
            string adapterId,
            CreateSnapshotTagValueSubscriptionRequest request,
            ChannelReader<TagValueSubscriptionUpdate> channel,
            CancellationToken cancellationToken
        ) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ISnapshotTagValuePush>(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            ValidateObject(request);

            var result = ChannelExtensions.CreateTagValueChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartSnapshotTagValuePushSubscribeActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.Subscribe(adapterCallContext, request, channel, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

#else

        /// <summary>
        /// Creates a snapshot tag value subscription.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to subscribe to.
        /// </param>
        /// <param name="request">
        ///   The subscription request parameters.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the subscription.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return the channel reader for the subscription.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> CreateSnapshotTagValueChannel(
            string adapterId,
            CreateSnapshotTagValueSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ISnapshotTagValuePush>(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            ValidateObject(request);

            var result = ChannelExtensions.CreateTagValueChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartSnapshotTagValuePushSubscribeActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.Subscribe(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

#endif

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

            var result = ChannelExtensions.CreateTagValueChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartReadSnapshotTagValuesActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.ReadSnapshotTagValues(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
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

            var result = ChannelExtensions.CreateTagValueChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartReadRawTagValuesActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.ReadRawTagValues(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
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

            var result = ChannelExtensions.CreateTagValueChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartReadPlotTagValuesActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.ReadPlotTagValues(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
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

            var result = ChannelExtensions.CreateTagValueChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartReadTagValuesAtTimesActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.ReadTagValuesAtTimes(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
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

            var result = ChannelExtensions.CreateChannel<DataFunctionDescriptor>(DefaultChannelCapacity);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartGetSupportedDataFunctionsActivity(adapter.Adapter.Descriptor.Id)) {
                    var resultChannel = await adapter.Feature.GetSupportedDataFunctions(adapterCallContext, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
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

            var result = ChannelExtensions.CreateTagValueChannel<ProcessedTagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartReadProcessedTagValuesActivity(adapter.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await adapter.Feature.ReadProcessedTagValues(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

        #endregion

        #region [ Tag Value Write ]

#if NETSTANDARD2_0 == false

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

            var result = ChannelExtensions.CreateTagValueWriteResultChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartWriteSnapshotTagValuesActivity(adapter.Adapter.Descriptor.Id)) {
                    var resultChannel = await adapter.Feature.WriteSnapshotTagValues(adapterCallContext, channel, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
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

            var result = ChannelExtensions.CreateTagValueWriteResultChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartWriteHistoricalTagValuesActivity(adapter.Adapter.Descriptor.Id)) {
                    var resultChannel = await adapter.Feature.WriteHistoricalTagValues(adapterCallContext, channel, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

#else

        /// <summary>
        /// Writes a tag value to the specified adapter's snapshot.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="item">
        ///   The value to write.
        /// </param>
        /// <returns>
        ///   The write result.
        /// </returns>
        public async Task<WriteTagValueResult> WriteSnapshotTagValue(string adapterId, WriteTagValueItem item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted)) {
                var cancellationToken = ctSource.Token;
                try {
                    var adapter = await ResolveAdapterAndFeature<IWriteSnapshotTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
                    var inChannel = Channel.CreateUnbounded<WriteTagValueItem>();
                    inChannel.Writer.TryWrite(item);
                    inChannel.Writer.TryComplete();

                    using (Telemetry.ActivitySource.StartWriteSnapshotTagValuesActivity(adapter.Adapter.Descriptor.Id)) {
                        var outChannel = await adapter.Feature.WriteSnapshotTagValues(adapterCallContext, inChannel, cancellationToken).ConfigureAwait(false);
                        return await outChannel.ReadAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                finally {
                    ctSource.Cancel();
                }
            }
        }


        /// <summary>
        /// Writes a tag value to the specified adapter's history archive.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="item">
        ///   The value to write.
        /// </param>
        /// <returns>
        ///   The write result.
        /// </returns>
        public async Task<WriteTagValueResult> WriteHistoricalTagValue(string adapterId, WriteTagValueItem item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted)) {
                var cancellationToken = ctSource.Token;
                try {
                    var adapter = await ResolveAdapterAndFeature<IWriteHistoricalTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
                    var inChannel = Channel.CreateUnbounded<WriteTagValueItem>();
                    inChannel.Writer.TryWrite(item);
                    inChannel.Writer.TryComplete();

                    using (Telemetry.ActivitySource.StartWriteHistoricalTagValuesActivity(adapter.Adapter.Descriptor.Id)) {
                        var outChannel = await adapter.Feature.WriteHistoricalTagValues(adapterCallContext, inChannel, cancellationToken).ConfigureAwait(false);
                        return await outChannel.ReadAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                finally {
                    ctSource.Cancel();
                }
            }
        }

#endif

        #endregion

    }
}
