using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {

    /// <summary>
    /// <see cref="IReadEventMessagesForTimeRange"/> implementation.
    /// </summary>
    internal class ReadEventMessagesForTimeRangeImpl : ProxyAdapterFeature, IReadEventMessagesForTimeRange {

        /// <summary>
        /// Creates a new <see cref="ReadEventMessagesForTimeRangeImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public ReadEventMessagesForTimeRangeImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public ChannelReader<Adapter.Events.EventMessage> ReadEventMessages(IAdapterCallContext context, Adapter.Events.ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageChannel<Adapter.Events.EventMessage>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<EventsService.EventsServiceClient>();
                var grpcRequest = new GetEventMessagesForTimeRangeRequest() {
                    AdapterId = AdapterId,
                    UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcStartTime),
                    UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcEndTime),
                    Direction = request.Direction.ToGrpcEventReadDirection(),
                    PageSize = request.PageSize,
                    Page = request.Page
                };
                var grpcResponse = client.GetEventMessagesForTimeRange(grpcRequest, GetCallOptions(context, ct));

                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterEventMessage(), ct).ConfigureAwait(false);
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
