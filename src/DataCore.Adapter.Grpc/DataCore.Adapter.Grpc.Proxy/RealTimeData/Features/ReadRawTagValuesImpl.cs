using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class ReadRawTagValuesImpl : ProxyAdapterFeature, IReadRawTagValues {

        public ReadRawTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public ChannelReader<Adapter.RealTimeData.Models.TagValueQueryResult> ReadRawTagValues(IAdapterCallContext context, Adapter.RealTimeData.Models.ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<Adapter.RealTimeData.Models.TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagValuesService.TagValuesServiceClient>();
                var grpcRequest = new ReadRawTagValuesRequest() {
                    AdapterId = AdapterId,
                    UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcStartTime),
                    UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcEndTime),
                    SampleCount = request.SampleCount,
                    BoundaryType = request.BoundaryType.ToGrpcRawDataBoundaryType()
                };
                grpcRequest.Tags.AddRange(request.Tags);

                var grpcResponse = client.ReadRawTagValues(grpcRequest, GetCallOptions(context, ct));
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
