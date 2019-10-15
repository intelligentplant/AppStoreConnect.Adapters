using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {
    internal class ReadEventMessagesForTimeRangeImpl : ProxyAdapterFeature, IReadEventMessagesForTimeRange {

        public ReadEventMessagesForTimeRangeImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public ChannelReader<Adapter.Events.EventMessage> ReadEventMessages(IAdapterCallContext context, Adapter.Events.ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageChannel<Adapter.Events.EventMessage>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<EventsService.EventsServiceClient>();
                var grpcRequest = new GetEventMessagesForTimeRangeRequest() {
                    AdapterId = AdapterId,
                    UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcStartTime),
                    UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcEndTime),
                    Direction = request.Direction.ToGrpcReadDirection(),
                    MessageCount = request.MessageCount
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
            }, true, cancellationToken);

            return result;
        }
    }
}
