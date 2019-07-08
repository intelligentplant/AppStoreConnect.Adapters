using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.Events.Features;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {
    internal class ReadEventMessagesUsingCursorImpl : ProxyAdapterFeature, IReadEventMessagesUsingCursor {

        public ReadEventMessagesUsingCursorImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public ChannelReader<Adapter.Events.Models.EventMessageWithCursorPosition> ReadEventMessages(IAdapterCallContext context, Adapter.Events.Models.ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageChannel<Adapter.Events.Models.EventMessageWithCursorPosition>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<EventsService.EventsServiceClient>();
                var grpcRequest = new GetEventMessagesUsingCursorPositionRequest() {
                    AdapterId = AdapterId,
                    CursorPosition = request.CursorPosition,
                    Direction = request.Direction.ToGrpcReadDirection(),
                    MessageCount = request.MessageCount
                };
                var grpcResponse = client.GetEventMessagesUsingCursorPosition(grpcRequest, GetCallOptions(context, ct));

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
