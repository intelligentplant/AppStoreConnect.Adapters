using DataCore.Adapter.AspNetCore.Internal;
using DataCore.Adapter.Events;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DataCore.Adapter.AspNetCore.Routing.V2 {
    internal class EventRoutes : IRouteProvider {

        public static void Register(IEndpointRouteBuilder builder) {
            builder.MapPost("/{adapterId}/by-time-range", ReadEventMessagesForTimeRangeAsync);
            builder.MapPost("/{adapterId}/by-cursor", ReadEventMessagesUsingCursorAsync);
            builder.MapPost("/{adapterId}/write", WriteEventMessagesAsync);
        }


        private static async Task<IResult> ReadEventMessagesForTimeRangeAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            ReadEventMessagesForTimeRangeRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IReadEventMessagesForTimeRange>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(resolverResult.Feature.ReadEventMessagesForTimeRange(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> ReadEventMessagesUsingCursorAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            ReadEventMessagesUsingCursorRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IReadEventMessagesUsingCursor>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(resolverResult.Feature.ReadEventMessagesUsingCursor(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> WriteEventMessagesAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            WriteEventMessagesRequestExtended request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IWriteEventMessages>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            var channel = request.Events.PublishToChannel();
            return Results.Ok(resolverResult.Feature.WriteEventMessages(resolverResult.CallContext, request, channel.ReadAllAsync(cancellationToken), cancellationToken));
        }

    }
}
