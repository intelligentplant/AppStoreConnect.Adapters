using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Tags.Features {

    /// <summary>
    /// Implements <see cref="ITagSearch"/> (and <see cref="ITagInfo"/>).
    /// </summary>
    internal class TagSearchImpl : ProxyAdapterFeature, ITagSearch {

        /// <summary>
        /// Creates a new <see cref="TagSearchImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public TagSearchImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<TagDefinition> FindTags(
            IAdapterCallContext context, 
            FindTagsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = GetClient();
            await foreach (var item in client.TagSearch.FindTagsAsync(
                AdapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <inheritdoc />
        public async IAsyncEnumerable<TagDefinition> GetTags(
            IAdapterCallContext context, 
            GetTagsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = GetClient();
            await foreach (var item in client.TagSearch.GetTagsAsync(
                AdapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <inheritdoc />
        public async IAsyncEnumerable<AdapterProperty> GetTagProperties(
            IAdapterCallContext context, 
            GetTagPropertiesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = GetClient();
            await foreach (var item in client.TagSearch.GetTagPropertiesAsync(
                AdapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }

    }
}
