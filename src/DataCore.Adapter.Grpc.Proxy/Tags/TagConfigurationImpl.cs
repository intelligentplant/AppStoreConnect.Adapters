using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Tags;

namespace DataCore.Adapter.Grpc.Proxy.Tags {

    /// <summary>
    /// <see cref="ITagConfiguration"/> implementation.
    /// </summary>
    internal class TagConfigurationImpl : ProxyAdapterFeature, ITagConfiguration {

        /// <summary>
        /// Creates a new <see cref="TagConfigurationImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public TagConfigurationImpl(GrpcAdapterProxy proxy) 
            : base(proxy) { }


        /// <inheritdoc/>
        public async Task<JsonElement> GetTagSchemaAsync(
            IAdapterCallContext context, 
            Adapter.Tags.GetTagSchemaRequest request, 
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<TagSearchService.TagSearchServiceClient>();

            var grpcRequest = new GetTagSchemaRequest() {
                AdapterId = AdapterId
            };

            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = await client.GetTagSchemaAsync(grpcRequest, GetCallOptions(context, cancellationToken)).ConfigureAwait(false);
            return grpcResponse.Schema.ToJsonElement()!.Value;
        }


        /// <inheritdoc/>
        public async Task<Adapter.Tags.TagDefinition> CreateTagAsync(
            IAdapterCallContext context, 
            Adapter.Tags.CreateTagRequest request, 
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<TagSearchService.TagSearchServiceClient>();

            var grpcRequest = new CreateTagRequest() {
                AdapterId = AdapterId,
                Body = request.Body.ToProtoValue()
            };

            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = await client.CreateTagAsync(grpcRequest, GetCallOptions(context, cancellationToken)).ConfigureAwait(false);
            return grpcResponse.ToAdapterTagDefinition();
        }


        /// <inheritdoc/>
        public async Task<Adapter.Tags.TagDefinition> UpdateTagAsync(
            IAdapterCallContext context, 
            Adapter.Tags.UpdateTagRequest request, 
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<TagSearchService.TagSearchServiceClient>();

            var grpcRequest = new UpdateTagRequest() {
                AdapterId = AdapterId,
                Body = request.Body.ToProtoValue()
            };

            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = await client.UpdateTagAsync(grpcRequest, GetCallOptions(context, cancellationToken)).ConfigureAwait(false);
            return grpcResponse.ToAdapterTagDefinition();
        }


        /// <inheritdoc/>
        public async Task<bool> DeleteTagAsync(
            IAdapterCallContext context, 
            Adapter.Tags.DeleteTagRequest request, 
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<TagSearchService.TagSearchServiceClient>();

            var grpcRequest = new DeleteTagRequest() {
                AdapterId = AdapterId,
                Tag = request.Tag
            };

            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = await client.DeleteTagAsync(grpcRequest, GetCallOptions(context, cancellationToken)).ConfigureAwait(false);
            return grpcResponse.Success;
        }

    }
}
