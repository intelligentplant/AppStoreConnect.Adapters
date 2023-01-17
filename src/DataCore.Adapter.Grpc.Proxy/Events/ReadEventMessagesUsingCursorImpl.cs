using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {

    /// <summary>
    /// <see cref="IReadEventMessagesUsingCursor"/> implementation.
    /// </summary>
    internal class ReadEventMessagesUsingCursorImpl : ProxyAdapterFeature, IReadEventMessagesUsingCursor {

        /// <summary>
        /// Creates a new <see cref="ReadEventMessagesUsingCursorImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public ReadEventMessagesUsingCursorImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public async IAsyncEnumerable<Adapter.Events.EventMessageWithCursorPosition> ReadEventMessagesUsingCursor(
            IAdapterCallContext context, 
            ReadEventMessagesUsingCursorRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<EventsService.EventsServiceClient>();
            var grpcRequest = new GetEventMessagesUsingCursorPositionRequest() {
                AdapterId = AdapterId,
                CursorPosition = request.CursorPosition ?? string.Empty,
                Direction = request.Direction.ToGrpcEventReadDirection(),
                PageSize = request.PageSize,
                Topic = request.Topic ?? string.Empty
            };
            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            using (var grpcResponse = client.GetEventMessagesUsingCursorPosition(grpcRequest, GetCallOptions(context, cancellationToken))) {
                while (await grpcResponse.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (grpcResponse.ResponseStream.Current == null) {
                        continue;
                    }
                    yield return grpcResponse.ResponseStream.Current.ToAdapterEventMessageWithCursorPosition();
                }
            }
        }
    }
}
