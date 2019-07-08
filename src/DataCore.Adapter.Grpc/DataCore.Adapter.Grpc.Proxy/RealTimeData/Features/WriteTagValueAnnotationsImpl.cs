using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class WriteTagValueAnnotationsImpl : ProxyAdapterFeature, IWriteTagValueAnnotations {

        public WriteTagValueAnnotationsImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public async Task<Adapter.RealTimeData.Models.WriteTagValueAnnotationResult> CreateAnnotation(IAdapterCallContext context, Adapter.RealTimeData.Models.CreateAnnotationRequest request, CancellationToken cancellationToken) {
            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new CreateAnnotationRequest() {
                AdapterId = AdapterId,
                TagId = request.TagId,
                Annotation = request.Annotation.ToGrpcTagValueAnnotationBase()
            };
            var grpcResponse = client.CreateAnnotationAsync(grpcRequest, GetCallOptions(context, cancellationToken));
            var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

            return result.ToAdapterWriteTagValueAnnotationResult();
        }

        public async Task<Adapter.RealTimeData.Models.WriteTagValueAnnotationResult> UpdateAnnotation(IAdapterCallContext context, Adapter.RealTimeData.Models.UpdateAnnotationRequest request, CancellationToken cancellationToken) {
            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new UpdateAnnotationRequest() {
                AdapterId = AdapterId,
                TagId = request.TagId,
                AnnotationId = request.AnnotationId,
                Annotation = request.Annotation.ToGrpcTagValueAnnotationBase()
            };
            var grpcResponse = client.UpdateAnnotationAsync(grpcRequest, GetCallOptions(context, cancellationToken));
            var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

            return result.ToAdapterWriteTagValueAnnotationResult();
        }

        public async Task<Adapter.RealTimeData.Models.WriteTagValueAnnotationResult> DeleteAnnotation(IAdapterCallContext context, Adapter.RealTimeData.Models.DeleteAnnotationRequest request, CancellationToken cancellationToken) {
            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new DeleteAnnotationRequest() {
                AdapterId = AdapterId,
                TagId = request.TagId,
                AnnotationId = request.AnnotationId
            };
            var grpcResponse = client.DeleteAnnotationAsync(grpcRequest, GetCallOptions(context, cancellationToken));
            var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

            return result.ToAdapterWriteTagValueAnnotationResult();
        }
    }
}
