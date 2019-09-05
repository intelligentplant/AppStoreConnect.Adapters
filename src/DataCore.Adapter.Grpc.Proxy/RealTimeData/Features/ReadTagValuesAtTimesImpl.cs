using System.Linq;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData.Features;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class ReadTagValuesAtTimesImpl : ProxyAdapterFeature, IReadTagValuesAtTimes {

        public ReadTagValuesAtTimesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public ChannelReader<Adapter.RealTimeData.Models.TagValueQueryResult> ReadTagValuesAtTimes(IAdapterCallContext context, Adapter.RealTimeData.Models.ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<Adapter.RealTimeData.Models.TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagValuesService.TagValuesServiceClient>();
                var grpcRequest = new ReadTagValuesAtTimesRequest() {
                    AdapterId = AdapterId
                };
                grpcRequest.Tags.AddRange(request.Tags);
                grpcRequest.UtcSampleTimes.AddRange(request.UtcSampleTimes.Select(x => Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(x)));

                var grpcResponse = client.ReadTagValuesAtTimes(grpcRequest, GetCallOptions(context, ct));
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
            }, true, cancellationToken);

            return result;
        }
    }
}
