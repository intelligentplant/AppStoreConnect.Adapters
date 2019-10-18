using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class ReadInterpolatedTagValuesImpl : ProxyAdapterFeature, IReadInterpolatedTagValues {

        public ReadInterpolatedTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public ChannelReader<Adapter.RealTimeData.TagValueQueryResult> ReadInterpolatedTagValues(IAdapterCallContext context, Adapter.RealTimeData.ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<Adapter.RealTimeData.TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagValuesService.TagValuesServiceClient>();
                var grpcRequest = new ReadInterpolatedTagValuesRequest() {
                    AdapterId = AdapterId,
                    UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcStartTime),
                    UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcEndTime),
                    SampleInterval = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(request.SampleInterval)
                };
                grpcRequest.Tags.AddRange(request.Tags);

                var grpcResponse = client.ReadInterpolatedTagValues(grpcRequest, GetCallOptions(context, ct));
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
