using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for requesting tag values, including pushing real-time snapshot value 
    // changes to subscribers. Snapshot push is only supported on adapters that implement the 
    // ISnapshotTagValuePush feature.

    public partial class AdapterHub {

        #region [ Snapshot Subscription Management ]

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
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<ISnapshotTagValuePush>(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            ValidateObject(request);

            await foreach (var item in adapter.Feature.Subscribe(adapterCallContext, request, channel, cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
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
        public async IAsyncEnumerable<TagValueQueryResult> ReadSnapshotTagValues(
            string adapterId, 
            ReadSnapshotTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<IReadSnapshotTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            await foreach (var item in adapter.Feature.ReadSnapshotTagValues(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                yield return item;
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
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<IReadRawTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            await foreach (var item in adapter.Feature.ReadRawTagValues(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                yield return item;
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
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<IReadPlotTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            await foreach (var item in adapter.Feature.ReadPlotTagValues(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                yield return item;
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
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<IReadTagValuesAtTimes>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            await foreach (var item in adapter.Feature.ReadTagValuesAtTimes(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
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
        public IAsyncEnumerable<DataFunctionDescriptor> GetSupportedDataFunctions(
            string adapterId,
            CancellationToken cancellationToken
        ) {
            return GetSupportedDataFunctionsWithRequest(adapterId, new GetSupportedDataFunctionsRequest(), cancellationToken);
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
        public async IAsyncEnumerable<DataFunctionDescriptor> GetSupportedDataFunctionsWithRequest(
            string adapterId, 
            GetSupportedDataFunctionsRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            // NOTE: this hub method is not called GetSupportedDataFunctions because ASP.NET Core
            // SignalR does not allow overloads of hub methods, so a different method name must be
            // used.

            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<IReadProcessedTagValues>(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            ValidateObject(request);

            await foreach (var item in adapter.Feature.GetSupportedDataFunctions(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                yield return item;
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
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<IReadProcessedTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            await foreach (var item in adapter.Feature.ReadProcessedTagValues(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
        }

        #endregion

        #region [ Tag Value Write ]

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
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<IWriteSnapshotTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);

            ValidateObject(request);

            await foreach (var item in adapter.Feature.WriteSnapshotTagValues(adapterCallContext, request, channel, cancellationToken).ConfigureAwait(false)) {
                yield return item;
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
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<IWriteHistoricalTagValues>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            await foreach (var item in adapter.Feature.WriteHistoricalTagValues(adapterCallContext, request, channel, cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
        }

        #endregion

    }
}
