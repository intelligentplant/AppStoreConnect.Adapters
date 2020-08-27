using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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
        public Task<ChannelReader<Adapter.Events.EventMessageWithCursorPosition>> ReadEventMessagesUsingCursor(IAdapterCallContext context, Adapter.Events.ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            GrpcAdapterProxy.ValidateObject(request);

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
            var grpcResponse = client.GetEventMessagesUsingCursorPosition(grpcRequest, GetCallOptions(context, cancellationToken));

            var result = ChannelExtensions.CreateEventMessageChannel<Adapter.Events.EventMessageWithCursorPosition>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterEventMessageWithCursorPosition(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcResponse.Dispose();
                }
            }, true, TaskScheduler, cancellationToken);

            return Task.FromResult(result.Reader);
        }
    }
}
