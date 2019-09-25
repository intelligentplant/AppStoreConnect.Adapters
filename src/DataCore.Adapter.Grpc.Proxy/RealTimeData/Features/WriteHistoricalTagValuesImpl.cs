using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class WriteHistoricalTagValuesImpl : ProxyAdapterFeature, IWriteHistoricalTagValues {

        public WriteHistoricalTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }

        public ChannelReader<Adapter.RealTimeData.Models.WriteTagValueResult> WriteHistoricalTagValues(IAdapterCallContext context, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueWriteResultChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagValuesService.TagValuesServiceClient>();
                var grpcStream = client.WriteHistoricalTagValues(GetCallOptions(context, ct));

                channel.RunBackgroundOperation(async (ch2, ct2) => {
                    try {
                        while (await ch2.WaitToReadAsync(ct2).ConfigureAwait(false)) {
                            if (ch2.TryRead(out var item) && item != null) {
                                await grpcStream.RequestStream.WriteAsync(item.ToGrpcWriteTagValueRequest(AdapterId)).ConfigureAwait(false);
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
                        await ch.WriteAsync(grpcStream.ResponseStream.Current.ToAdapterWriteTagValueResult(), ct).ConfigureAwait(false);
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
