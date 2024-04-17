using DataCore.Adapter.AspNetCore.Internal;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DataCore.Adapter.AspNetCore.Routing.V2 {
    internal class TagValueRoutes : IRouteProvider {

        /// <summary>
        /// Default query time range to use in a historical query if a start or end time is not 
        /// specified on a route that accepts the time range as query string parameters.
        /// </summary>
        public static TimeSpan DefaultHistoricalQueryDuration { get; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Default number of samples or intervals to request in a historical query if this is not 
        /// specified on a route that accepts this value as a query string parameter.
        /// </summary>
        public const int DefaultSampleOrIntervalCount = 100;


        public static void Register(IEndpointRouteBuilder builder) {
            builder.MapGet($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/snapshot", ReadSnapshotTagValuesGetAsync)
                .Produces<IAsyncEnumerable<TagValueQueryResult>>()
                .ProducesDefaultErrors();

            builder.MapPost($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/snapshot", ReadSnapshotTagValuesPostAsync)
                .Produces<IAsyncEnumerable<TagValueQueryResult>>()
                .ProducesDefaultErrors();

            builder.MapGet($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/raw", ReadRawTagValuesGetAsync)
                .Produces<IAsyncEnumerable<TagValueQueryResult>>()
                .ProducesDefaultErrors();

            builder.MapPost($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/raw", ReadRawTagValuesPostAsync)
                .Produces<IAsyncEnumerable<TagValueQueryResult>>()
                .ProducesDefaultErrors();

            builder.MapGet($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/plot", ReadPlotTagValuesGetAsync)
                .Produces<IAsyncEnumerable<TagValueQueryResult>>()
                .ProducesDefaultErrors();

            builder.MapPost($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/plot", ReadPlotTagValuesPostAsync)
                .Produces<IAsyncEnumerable<TagValueQueryResult>>()
                .ProducesDefaultErrors();

            builder.MapGet($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/values-at-times", ReadTagValuesAtTimesGetAsync)
                .Produces<IAsyncEnumerable<TagValueQueryResult>>()
                .ProducesDefaultErrors();

            builder.MapPost($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/values-at-times", ReadTagValuesAtTimesPostAsync)
                .Produces<IAsyncEnumerable<TagValueQueryResult>>()
                .ProducesDefaultErrors();

            builder.MapGet($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/supported-aggregations", GetSupportedAggregationsGetAsync)
                .Produces<IAsyncEnumerable<DataFunctionDescriptor>>()
                .ProducesDefaultErrors();

            builder.MapPost($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/supported-aggregations", GetSupportedAggregationsPostAsync)
                .Produces<IAsyncEnumerable<DataFunctionDescriptor>>()
                .ProducesDefaultErrors();

            builder.MapGet($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/processed", ReadProcessedTagValuesGetAsync)
                .Produces<IAsyncEnumerable<ProcessedTagValueQueryResult>>()
                .ProducesDefaultErrors();

            builder.MapPost($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/processed", ReadProcessedTagValuesPostAsync)
                .Produces<IAsyncEnumerable<ProcessedTagValueQueryResult>>()
                .ProducesDefaultErrors();

            builder.MapPost($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/write/snapshot", WriteSnapshotTagValuesAsync)
                .Produces<IAsyncEnumerable<WriteTagValueResult>>()
                .ProducesDefaultErrors();

            builder.MapPost($"{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/write/history", WriteHistoricalTagValuesAsync)
                .Produces<IAsyncEnumerable<WriteTagValueResult>>()
                .ProducesDefaultErrors();
        }


        private static async Task<IResult> ReadSnapshotTagValuesGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            string[] tag,
            CancellationToken cancellationToken = default
        ) {
            return await ReadSnapshotTagValuesPostAsync(context, adapterAccessor, adapterId, new ReadSnapshotTagValuesRequest() { 
                Tags = tag
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> ReadSnapshotTagValuesPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            ReadSnapshotTagValuesRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IReadSnapshotTagValues>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.ReadSnapshotTagValues(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> ReadRawTagValuesGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            string[] tag,
            DateTime? start = null,
            DateTime? end = null,
            int count = DefaultSampleOrIntervalCount,
            RawDataBoundaryType boundary = RawDataBoundaryType.Inside,
            CancellationToken cancellationToken = default
        ) {
            var now = DateTime.UtcNow;
            if (start == null && end == null) {
                end = now;
                start = now.Subtract(DefaultHistoricalQueryDuration);
            }
            else if (start == null) {
                start = end!.Value.Subtract(DefaultHistoricalQueryDuration);
            }
            else if (end == null) {
                end = start.Value.Add(DefaultHistoricalQueryDuration);
            }

            return await ReadRawTagValuesPostAsync(context, adapterAccessor, adapterId, new ReadRawTagValuesRequest() {
                Tags = tag,
                UtcStartTime = Utils.ConvertToUniversalTime(start.Value),
                UtcEndTime = Utils.ConvertToUniversalTime(end.Value),
                SampleCount = count,
                BoundaryType = boundary
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> ReadRawTagValuesPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            ReadRawTagValuesRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IReadRawTagValues>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.ReadRawTagValues(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> ReadPlotTagValuesGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            string[] tag,
            DateTime? start = null,
            DateTime? end = null,
            int count = DefaultSampleOrIntervalCount,
            CancellationToken cancellationToken = default
        ) {
            var now = DateTime.UtcNow;
            if (start == null && end == null) {
                end = now;
                start = now.Subtract(DefaultHistoricalQueryDuration);
            }
            else if (start == null) {
                start = end!.Value.Subtract(DefaultHistoricalQueryDuration);
            }
            else if (end == null) {
                end = start.Value.Add(DefaultHistoricalQueryDuration);
            }

            return await ReadPlotTagValuesPostAsync(context, adapterAccessor, adapterId, new ReadPlotTagValuesRequest() {
                Tags = tag,
                UtcStartTime = Utils.ConvertToUniversalTime(start.Value),
                UtcEndTime = Utils.ConvertToUniversalTime(end.Value),
                Intervals = count,
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> ReadPlotTagValuesPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            ReadPlotTagValuesRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IReadPlotTagValues>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.ReadPlotTagValues(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> ReadTagValuesAtTimesGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            string[] tag,
            DateTime[] time,
            CancellationToken cancellationToken = default
        ) {
            return await ReadTagValuesAtTimesPostAsync(context, adapterAccessor, adapterId, new ReadTagValuesAtTimesRequest() {
                Tags = tag,
                UtcSampleTimes = time?.Select(Utils.ConvertToUniversalTime)?.ToArray()!
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> ReadTagValuesAtTimesPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            ReadTagValuesAtTimesRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IReadTagValuesAtTimes>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.ReadTagValuesAtTimes(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> GetSupportedAggregationsGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            CancellationToken cancellationToken = default
        ) {
            return await GetSupportedAggregationsPostAsync(context, adapterAccessor, adapterId, new GetSupportedDataFunctionsRequest(), cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> GetSupportedAggregationsPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            GetSupportedDataFunctionsRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IReadProcessedTagValues>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.GetSupportedDataFunctions(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> ReadProcessedTagValuesGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            string[] tag,
            DateTime? start = null,
            DateTime? end = null,
            int count = DefaultSampleOrIntervalCount,
            string[]? function = null,
            CancellationToken cancellationToken = default
        ) {
            var now = DateTime.UtcNow;
            if (start == null && end == null) {
                end = now;
                start = now.Subtract(DefaultHistoricalQueryDuration);
            }
            else if (start == null) {
                start = end!.Value.Subtract(DefaultHistoricalQueryDuration);
            }
            else if (end == null) {
                end = start.Value.Add(DefaultHistoricalQueryDuration);
            }

            if (count < 1) {
                count = 1;
            }

            var interval = TimeSpan.FromSeconds((end.Value - start.Value).TotalSeconds / count);

            return await ReadProcessedTagValuesPostAsync(context, adapterAccessor, adapterId, new ReadProcessedTagValuesRequest() {
                Tags = tag,
                UtcStartTime = Utils.ConvertToUniversalTime(start.Value),
                UtcEndTime = Utils.ConvertToUniversalTime(end.Value),
                SampleInterval = interval,
                DataFunctions = function!
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> ReadProcessedTagValuesPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            ReadProcessedTagValuesRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IReadProcessedTagValues>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.ReadProcessedTagValues(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> WriteSnapshotTagValuesAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            WriteTagValuesRequestExtended request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IWriteSnapshotTagValues>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            var channel = request.Values.PublishToChannel();
            return Results.Ok(resolverResult.Feature.WriteSnapshotTagValues(resolverResult.CallContext, request, channel.ReadAllAsync(cancellationToken), cancellationToken));
        }


        private static async Task<IResult> WriteHistoricalTagValuesAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            WriteTagValuesRequestExtended request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IWriteHistoricalTagValues>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            var channel = request.Values.PublishToChannel();
            return Results.Ok(resolverResult.Feature.WriteHistoricalTagValues(resolverResult.CallContext, request, channel.ReadAllAsync(cancellationToken), cancellationToken));
        }

    }
}
