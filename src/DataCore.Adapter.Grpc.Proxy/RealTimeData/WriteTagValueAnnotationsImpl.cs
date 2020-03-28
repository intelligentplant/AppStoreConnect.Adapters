using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class WriteTagValueAnnotationsImpl : ProxyAdapterFeature, IWriteTagValueAnnotations {

        public WriteTagValueAnnotationsImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public async Task<Adapter.RealTimeData.WriteTagValueAnnotationResult> CreateAnnotation(IAdapterCallContext context, Adapter.RealTimeData.CreateAnnotationRequest request, CancellationToken cancellationToken) {
            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new CreateAnnotationRequest() {
                AdapterId = AdapterId,
                Tag = request.Tag ?? string.Empty,
                Annotation = request.Annotation.ToGrpcTagValueAnnotationBase()
            };
            var grpcResponse = client.CreateAnnotationAsync(grpcRequest, GetCallOptions(context, cancellationToken));
            var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

            return result.ToAdapterWriteTagValueAnnotationResult();
        }


        public async Task<Adapter.RealTimeData.WriteTagValueAnnotationResult> UpdateAnnotation(IAdapterCallContext context, Adapter.RealTimeData.UpdateAnnotationRequest request, CancellationToken cancellationToken) {
            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new UpdateAnnotationRequest() {
                AdapterId = AdapterId,
                Tag = request.Tag ?? string.Empty,
                AnnotationId = request.AnnotationId ?? string.Empty,
                Annotation = request.Annotation.ToGrpcTagValueAnnotationBase()
            };
            var grpcResponse = client.UpdateAnnotationAsync(grpcRequest, GetCallOptions(context, cancellationToken));
            var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

            return result.ToAdapterWriteTagValueAnnotationResult();
        }


        public async Task<Adapter.RealTimeData.WriteTagValueAnnotationResult> DeleteAnnotation(IAdapterCallContext context, Adapter.RealTimeData.DeleteAnnotationRequest request, CancellationToken cancellationToken) {
            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new DeleteAnnotationRequest() {
                AdapterId = AdapterId,
                Tag = request.Tag ?? string.Empty,
                AnnotationId = request.AnnotationId ?? string.Empty
            };
            var grpcResponse = client.DeleteAnnotationAsync(grpcRequest, GetCallOptions(context, cancellationToken));
            var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

            return result.ToAdapterWriteTagValueAnnotationResult();
        }
    }
}
