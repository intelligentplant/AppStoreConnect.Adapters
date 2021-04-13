using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        public async IAsyncEnumerable<TagValueQueryResult> CreateSnapshotTagValueChannel(
            string adapterId,
            CreateSnapshotTagValueSubscriptionRequest request,
            IAsyncEnumerable<TagValueSubscriptionUpdate> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ISnapshotTagValuePush>(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartSnapshotTagValuePushSubscribeActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.Subscribe(adapterCallContext, request, channel, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
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
        public async IAsyncEnumerable<TagValueQueryResult> CreateSnapshotTagValueChannel(
            string adapterId,
            CreateSnapshotTagValueSubscriptionRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ISnapshotTagValuePush>(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartSnapshotTagValuePushSubscribeActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.Subscribe(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
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
        public async IAsyncEnumerable<TagValueQueryResult> ReadSnapshotTagValues(
            string adapterId, 
            ReadSnapshotTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadSnapshotTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartReadSnapshotTagValuesActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.ReadSnapshotTagValues(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
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
        public async IAsyncEnumerable<TagValueQueryResult> ReadRawTagValues(
            string adapterId,
            ReadRawTagValuesRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadRawTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartReadRawTagValuesActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.ReadRawTagValues(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
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
        public async IAsyncEnumerable<TagValueQueryResult> ReadPlotTagValues(
            string adapterId,
            ReadPlotTagValuesRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadPlotTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartReadPlotTagValuesActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.ReadPlotTagValues(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
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
        public async IAsyncEnumerable<TagValueQueryResult> ReadTagValuesAtTimes(
            string adapterId, 
            ReadTagValuesAtTimesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadTagValuesAtTimes>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartReadTagValuesAtTimesActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.ReadTagValuesAtTimes(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
        }


        /// <summary>
        /// Gets the data functions supported by <see cref="ReadProcessedTagValues(string, ReadProcessedTagValuesRequest, CancellationToken)"/> 
        /// queries.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The supported data functions for processed data queries.
        /// </returns>
        public async IAsyncEnumerable<DataFunctionDescriptor> GetSupportedDataFunctions(
            string adapterId, 
            GetSupportedDataFunctionsRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadProcessedTagValues>(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartGetSupportedDataFunctionsActivity(adapter.Adapter.Descriptor.Id)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.GetSupportedDataFunctions(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
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
        public async IAsyncEnumerable<ProcessedTagValueQueryResult> ReadProcessedTagValues(
            string adapterId, 
            ReadProcessedTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadProcessedTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartReadProcessedTagValuesActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.ReadProcessedTagValues(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
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
        /// <param name="request">
        ///   The request.
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
        public async IAsyncEnumerable<WriteTagValueResult> WriteSnapshotTagValues(
            string adapterId, 
            WriteTagValuesRequest request,
            IAsyncEnumerable<WriteTagValueItem> channel, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IWriteSnapshotTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);

            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartWriteSnapshotTagValuesActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.WriteSnapshotTagValues(adapterCallContext, request, channel, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
        }


        /// <summary>
        /// Writes values to the specified adapter's historical archive.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
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
        public async IAsyncEnumerable<WriteTagValueResult> WriteHistoricalTagValues(
            string adapterId, 
            WriteTagValuesRequest request,
            IAsyncEnumerable<WriteTagValueItem> channel, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IWriteHistoricalTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartWriteHistoricalTagValuesActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.WriteHistoricalTagValues(adapterCallContext, request, channel, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
        }

#else

        /// <summary>
        /// Writes a tag value to the specified adapter's snapshot.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="item">
        ///   The value to write.
        /// </param>
        /// <returns>
        ///   The write result.
        /// </returns>
        public async Task<WriteTagValueResult> WriteSnapshotTagValue(
            string adapterId, 
            WriteTagValuesRequest request,
            WriteTagValueItem item
        ) {
            ValidateObject(item);
            ValidateObject(request);
            var adapterCallContext = new SignalRAdapterCallContext(Context);

            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted)) {
                var cancellationToken = ctSource.Token;
                try {
                    var adapter = await ResolveAdapterAndFeature<IWriteSnapshotTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
                    var inChannel = Channel.CreateUnbounded<WriteTagValueItem>();
                    inChannel.Writer.TryWrite(item);
                    inChannel.Writer.TryComplete();

                    using (var activity = Telemetry.ActivitySource.StartWriteSnapshotTagValuesActivity(adapter.Adapter.Descriptor.Id, request)) {
                        var result = await adapter.Feature.WriteSnapshotTagValues(adapterCallContext, request, inChannel.Reader.ReadAllAsync(cancellationToken), cancellationToken).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                        activity.SetResponseItemCountTag(result == null ? 0 : 1);
                        return result!;
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
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="item">
        ///   The value to write.
        /// </param>
        /// <returns>
        ///   The write result.
        /// </returns>
        public async Task<WriteTagValueResult> WriteHistoricalTagValue(
            string adapterId,
            WriteTagValuesRequest request,
            WriteTagValueItem item
        ) {
            ValidateObject(item);
            ValidateObject(request);
            var adapterCallContext = new SignalRAdapterCallContext(Context);

            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted)) {
                var cancellationToken = ctSource.Token;
                try {
                    var adapter = await ResolveAdapterAndFeature<IWriteHistoricalTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
                    var inChannel = Channel.CreateUnbounded<WriteTagValueItem>();
                    inChannel.Writer.TryWrite(item);
                    inChannel.Writer.TryComplete();

                    using (var activity = Telemetry.ActivitySource.StartWriteHistoricalTagValuesActivity(adapter.Adapter.Descriptor.Id, request)) {
                        var result = await adapter.Feature.WriteHistoricalTagValues(adapterCallContext, request, inChannel.Reader.ReadAllAsync(cancellationToken), cancellationToken).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                        activity.SetResponseItemCountTag(result == null ? 0 : 1);
                        return result!;
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
