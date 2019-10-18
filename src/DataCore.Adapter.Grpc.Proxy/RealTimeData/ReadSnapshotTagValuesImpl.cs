using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class ReadSnapshotTagValuesImpl : ProxyAdapterFeature, IReadSnapshotTagValues {

        public ReadSnapshotTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public ChannelReader<Adapter.RealTimeData.TagValueQueryResult> ReadSnapshotTagValues(IAdapterCallContext context, Adapter.RealTimeData.ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<Adapter.RealTimeData.TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagValuesService.TagValuesServiceClient>();
                var grpcRequest = new ReadSnapshotTagValuesRequest() {
                    AdapterId = AdapterId
                };
                grpcRequest.Tags.AddRange(request.Tags);

                var grpcResponse = client.ReadSnapshotTagValues(grpcRequest, GetCallOptions(context, ct));
                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterTagValueQueryResult(), ct).ConfigureAwait(false);
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
