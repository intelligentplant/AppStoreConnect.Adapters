using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.Events;
using DataCore.Adapter.Events.Features;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {
    internal class WriteEventMessagesImpl : ProxyAdapterFeature, IWriteEventMessages {

        public WriteEventMessagesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public ChannelReader<Adapter.Events.Models.WriteEventMessageResult> WriteEventMessages(IAdapterCallContext context, ChannelReader<Adapter.Events.Models.WriteEventMessageItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageWriteResultChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<EventsService.EventsServiceClient>();
                var grpcStream = client.WriteEventMessages(GetCallOptions(context, ct));

                channel.RunBackgroundOperation(async (ch2, ct2) => {
                    try {
                        while (await ch2.WaitToReadAsync(ct2).ConfigureAwait(false)) {
                            if (ch2.TryRead(out var item) && item != null) {
                                await grpcStream.RequestStream.WriteAsync(item.ToGrpcWriteEventMessageItem(AdapterId)).ConfigureAwait(false);
                            }
                        }
                    }
                    finally {
                        await grpcStream.RequestStream.CompleteAsync().ConfigureAwait(false);
                    }
                }, ct);

                try {
                    while (await grpcStream.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcStream.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcStream.ResponseStream.Current.ToAdapterWriteEventMessageResult(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcStream.Dispose();
                }
            }, true, cancellationToken);

            return result;
        }
    }
}
