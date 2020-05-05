using System.Threading;
using System.Threading.Channels;
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
        public ChannelReader<Adapter.Events.EventMessageWithCursorPosition> ReadEventMessages(IAdapterCallContext context, Adapter.Events.ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageChannel<Adapter.Events.EventMessageWithCursorPosition>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<EventsService.EventsServiceClient>();
                var grpcRequest = new GetEventMessagesUsingCursorPositionRequest() {
                    AdapterId = AdapterId,
                    CursorPosition = request.CursorPosition ?? string.Empty,
                    Direction = request.Direction.ToGrpcEventReadDirection(),
                    PageSize = request.PageSize
                };
                var grpcResponse = client.GetEventMessagesUsingCursorPosition(grpcRequest, GetCallOptions(context, ct));

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

            return result;
        }
    }
}
