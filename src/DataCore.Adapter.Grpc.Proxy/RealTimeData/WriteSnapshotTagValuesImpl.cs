using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class WriteSnapshotTagValuesImpl : ProxyAdapterFeature, IWriteSnapshotTagValues {

        public WriteSnapshotTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }

        public ChannelReader<Adapter.RealTimeData.WriteTagValueResult> WriteSnapshotTagValues(IAdapterCallContext context, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueWriteResultChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagValuesService.TagValuesServiceClient>();
                var grpcStream = client.WriteSnapshotTagValues(GetCallOptions(context, ct));

                channel.RunBackgroundOperation(async (ch2, ct2) => {
                    try {
                        while (await ch2.WaitToReadAsync(ct2).ConfigureAwait(false)) {
                            if (ch2.TryRead(out var item) && item != null) {
                                await grpcStream.RequestStream.WriteAsync(item.ToGrpcWriteTagValueItem(AdapterId)).ConfigureAwait(false);
                            }
                        }
                    }
                    finally {
                        await grpcStream.RequestStream.CompleteAsync().ConfigureAwait(false);
                    }
                }, TaskScheduler, ct);

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
            }, true, TaskScheduler, cancellationToken);

            return result;
        }
    }
}
