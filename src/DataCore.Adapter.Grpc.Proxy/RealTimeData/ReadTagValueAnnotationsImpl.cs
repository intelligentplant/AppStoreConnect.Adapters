using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="IReadTagValueAnnotations"/> implementation.
    /// </summary>
    internal class ReadTagValueAnnotationsImpl : ProxyAdapterFeature, IReadTagValueAnnotations {

        /// <summary>
        /// Creates a new <see cref="ReadTagValueAnnotationsImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public ReadTagValueAnnotationsImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public async IAsyncEnumerable<Adapter.RealTimeData.TagValueAnnotationQueryResult> ReadAnnotations(
            IAdapterCallContext context, 
            Adapter.RealTimeData.ReadAnnotationsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new ReadAnnotationsRequest() {
                AdapterId = AdapterId,
                UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcStartTime.ToUniversalTime()),
                UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcEndTime.ToUniversalTime()),
                MaxAnnotationCount = request.AnnotationCount
            };
            grpcRequest.Tags.AddRange(request.Tags);
            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            using (var grpcResponse = client.ReadAnnotations(grpcRequest, GetCallOptions(context, cancellationToken))) {
                while (await grpcResponse.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (grpcResponse.ResponseStream.Current == null) {
                        continue;
                    }
                    yield return grpcResponse.ResponseStream.Current.ToAdapterTagValueAnnotationQueryResult();
                }
            }
        }


        /// <inheritdoc/>
        public async Task<TagValueAnnotationExtended?> ReadAnnotation(IAdapterCallContext context, Adapter.RealTimeData.ReadAnnotationRequest request, CancellationToken cancellationToken) {
            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new ReadAnnotationRequest() {
                AdapterId = AdapterId,
                Tag = request.Tag,
                AnnotationId = request.AnnotationId
            };

            using (var grpcResponse = client.ReadAnnotationAsync(grpcRequest, GetCallOptions(context, cancellationToken))) {
                var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

                return result.ToAdapterTagValueAnnotation();
            }
        }

    }

}
