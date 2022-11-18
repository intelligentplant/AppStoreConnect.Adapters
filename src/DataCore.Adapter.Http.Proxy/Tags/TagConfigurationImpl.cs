using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Tags;

namespace DataCore.Adapter.Http.Proxy.Tags {

    /// <summary>
    /// Implements <see cref="ITagConfiguration"/>
    /// </summary>
    internal class TagConfigurationImpl : ProxyAdapterFeature, ITagConfiguration {

        /// <summary>
        /// Creates a new <see cref="TagConfigurationImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public TagConfigurationImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public async Task<JsonElement> GetTagSchemaAsync(IAdapterCallContext context, GetTagSchemaRequest request, CancellationToken cancellationToken) {
            var client = GetClient();
            return await client.TagSearch.GetTagSchemaAsync(AdapterId, request, context.ToRequestMetadata(), cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async Task<TagDefinition> CreateTagAsync(IAdapterCallContext context, CreateTagRequest request, CancellationToken cancellationToken) {
            var client = GetClient();
            return await client.TagSearch.CreateTagAsync(AdapterId, request, context.ToRequestMetadata(), cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async Task<TagDefinition> UpdateTagAsync(IAdapterCallContext context, UpdateTagRequest request, CancellationToken cancellationToken) {
            var client = GetClient();
            return await client.TagSearch.UpdateTagAsync(AdapterId, request, context.ToRequestMetadata(), cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async Task<bool> DeleteTagAsync(IAdapterCallContext context, DeleteTagRequest request, CancellationToken cancellationToken) {
            var client = GetClient();
            return await client.TagSearch.DeleteTagAsync(AdapterId, request, context.ToRequestMetadata(), cancellationToken).ConfigureAwait(false);
        }

    }
}
