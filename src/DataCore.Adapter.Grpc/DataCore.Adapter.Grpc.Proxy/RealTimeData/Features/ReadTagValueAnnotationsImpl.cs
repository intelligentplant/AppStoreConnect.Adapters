using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class ReadTagValueAnnotationsImpl : ProxyAdapterFeature, IReadTagValueAnnotations {

        public ReadTagValueAnnotationsImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public ChannelReader<Adapter.RealTimeData.Models.TagValueAnnotationQueryResult> ReadAnnotations(IAdapterCallContext context, Adapter.RealTimeData.Models.ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueAnnotationChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
                var grpcRequest = new ReadAnnotationsRequest() {
                    AdapterId = AdapterId,
                    UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcStartTime),
                    UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcEndTime)
                };
                grpcRequest.Tags.AddRange(request.Tags);

                var grpcResponse = client.ReadAnnotations(grpcRequest, cancellationToken: ct);
                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterAnnotationQueryResult(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcResponse.Dispose();
                }
            }, true, cancellationToken);

            return result;
        }

        public async Task<Adapter.RealTimeData.Models.TagValueAnnotation> ReadAnnotation(IAdapterCallContext context, Adapter.RealTimeData.Models.ReadAnnotationRequest request, CancellationToken cancellationToken) {
            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new ReadAnnotationRequest() {
                AdapterId = AdapterId,
                TagId = request.TagId,
                AnnotationId = request.AnnotationId
            };

            var grpcResponse = client.ReadAnnotationAsync(grpcRequest, cancellationToken: cancellationToken);
            var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

            return result.ToAdapterTagValueAnnotation();
        }
    }
}
