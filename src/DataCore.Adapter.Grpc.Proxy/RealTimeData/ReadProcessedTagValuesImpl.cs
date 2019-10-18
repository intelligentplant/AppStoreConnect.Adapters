using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class ReadProcessedTagValuesImpl : ProxyAdapterFeature, IReadProcessedTagValues {

        public ReadProcessedTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public async Task<IEnumerable<Adapter.RealTimeData.DataFunctionDescriptor>> GetSupportedDataFunctions(IAdapterCallContext context, CancellationToken cancellationToken) {
            var client = CreateClient<TagValuesService.TagValuesServiceClient>();
            var response = client.GetSupportedDataFunctionsAsync(new GetSupportedDataFunctionsRequest() {
                AdapterId = AdapterId
            }, cancellationToken: cancellationToken);

            var result = await response.ResponseAsync.ConfigureAwait(false);
            return result.DataFunctions.Where(x => x != null).Select(x => x.ToAdapterDataFunctionDescriptor()).ToArray();
        }


        public ChannelReader<Adapter.RealTimeData.ProcessedTagValueQueryResult> ReadProcessedTagValues(IAdapterCallContext context, Adapter.RealTimeData.ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<Adapter.RealTimeData.ProcessedTagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagValuesService.TagValuesServiceClient>();
                var grpcRequest = new ReadProcessedTagValuesRequest() {
                    AdapterId = AdapterId,
                    UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcStartTime),
                    UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcEndTime),
                    SampleInterval = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(request.SampleInterval)
                };
                grpcRequest.Tags.AddRange(request.Tags);
                grpcRequest.DataFunctions.AddRange(request.DataFunctions);

                var grpcResponse = client.ReadProcessedTagValues(grpcRequest, GetCallOptions(context, ct));
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
