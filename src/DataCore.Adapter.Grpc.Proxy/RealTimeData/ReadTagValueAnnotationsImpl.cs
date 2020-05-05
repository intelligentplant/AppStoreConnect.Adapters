using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="ReadTagValueAnnotationsImpl"/> implementation.
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
        public ChannelReader<Adapter.RealTimeData.TagValueAnnotationQueryResult> ReadAnnotations(IAdapterCallContext context, Adapter.RealTimeData.ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueAnnotationChannel(-1);

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
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterTagValueAnnotationQueryResult(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcResponse.Dispose();
                }
            }, true, TaskScheduler, cancellationToken);

            return result;
        }


        /// <inheritdoc/>
        public async Task<TagValueAnnotationExtended> ReadAnnotation(IAdapterCallContext context, Adapter.RealTimeData.ReadAnnotationRequest request, CancellationToken cancellationToken) {
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
