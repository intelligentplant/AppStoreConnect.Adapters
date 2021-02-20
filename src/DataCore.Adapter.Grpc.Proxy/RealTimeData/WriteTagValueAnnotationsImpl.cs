using System;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="IWriteHistoricalTagValues"/> implementation.
    /// </summary>
    internal class WriteTagValueAnnotationsImpl : ProxyAdapterFeature, IWriteTagValueAnnotations {

        /// <summary>
        /// Creates a new <see cref="WriteSnapshotTagValuesImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public WriteTagValueAnnotationsImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async Task<Adapter.RealTimeData.WriteTagValueAnnotationResult> CreateAnnotation(IAdapterCallContext context, Adapter.RealTimeData.CreateAnnotationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, request);

            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new CreateAnnotationRequest() {
                AdapterId = AdapterId,
                Tag = request.Tag ?? string.Empty,
                Annotation = request.Annotation.ToGrpcTagValueAnnotationBase()
            };

            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = client.CreateAnnotationAsync(grpcRequest, GetCallOptions(context, cancellationToken));
            var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

            return result.ToAdapterWriteTagValueAnnotationResult();
        }


        /// <inheritdoc />
        public async Task<Adapter.RealTimeData.WriteTagValueAnnotationResult> UpdateAnnotation(IAdapterCallContext context, Adapter.RealTimeData.UpdateAnnotationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, request);

            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new UpdateAnnotationRequest() {
                AdapterId = AdapterId,
                Tag = request.Tag ?? string.Empty,
                AnnotationId = request.AnnotationId ?? string.Empty,
                Annotation = request.Annotation.ToGrpcTagValueAnnotationBase()
            };

            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = client.UpdateAnnotationAsync(grpcRequest, GetCallOptions(context, cancellationToken));
            var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

            return result.ToAdapterWriteTagValueAnnotationResult();
        }


        /// <inheritdoc />
        public async Task<Adapter.RealTimeData.WriteTagValueAnnotationResult> DeleteAnnotation(IAdapterCallContext context, Adapter.RealTimeData.DeleteAnnotationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, request);

            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new DeleteAnnotationRequest() {
                AdapterId = AdapterId,
                Tag = request.Tag ?? string.Empty,
                AnnotationId = request.AnnotationId ?? string.Empty
            };

            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = client.DeleteAnnotationAsync(grpcRequest, GetCallOptions(context, cancellationToken));
            var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

            return result.ToAdapterWriteTagValueAnnotationResult();
        }

    }

}
