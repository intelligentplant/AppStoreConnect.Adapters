using System;
using System.Threading;
using System.Threading.Channels;
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
        public Task<ChannelReader<Adapter.RealTimeData.TagValueAnnotationQueryResult>> ReadAnnotations(IAdapterCallContext context, Adapter.RealTimeData.ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, request);

            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new ReadAnnotationsRequest() {
                AdapterId = AdapterId,
                UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcStartTime),
                UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcEndTime),
                MaxAnnotationCount = request.AnnotationCount
            };
            grpcRequest.Tags.AddRange(request.Tags);
            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = client.ReadAnnotations(grpcRequest, GetCallOptions(context, cancellationToken));

            var result = ChannelExtensions.CreateTagValueAnnotationChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterTagValueAnnotationQueryResult(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcResponse.Dispose();
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc/>
        public async Task<TagValueAnnotationExtended> ReadAnnotation(IAdapterCallContext context, Adapter.RealTimeData.ReadAnnotationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, request);

            var client = CreateClient<TagValueAnnotationsService.TagValueAnnotationsServiceClient>();
            var grpcRequest = new ReadAnnotationRequest() {
                AdapterId = AdapterId,
                Tag = request.Tag,
                AnnotationId = request.AnnotationId
            };

            var grpcResponse = client.ReadAnnotationAsync(grpcRequest, GetCallOptions(context, cancellationToken));
            var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

            return result.ToAdapterTagValueAnnotation();
        }

    }

}
