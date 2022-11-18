using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.Http.Proxy.Tags {
    /// <summary>
    /// Implements <see cref="ITagSearch"/>.
    /// </summary>
    internal class TagSearchImpl : ProxyAdapterFeature, ITagSearch {

        /// <summary>
        /// Creates a new <see cref="TagSearchImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public TagSearchImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<TagDefinition> FindTags(
            IAdapterCallContext context, 
            FindTagsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var client = GetClient();
                await foreach (var item in client.TagSearch.FindTagsAsync(AdapterId, request, context?.ToRequestMetadata(), ctSource.Token).ConfigureAwait(false)) {
                    yield return item;
                }
            }
        }


        /// <inheritdoc />
        public async IAsyncEnumerable<TagDefinition> GetTags(
            IAdapterCallContext context, 
            GetTagsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var client = GetClient();
                await foreach (var item in client.TagSearch.GetTagsAsync(AdapterId, request, context?.ToRequestMetadata(), ctSource.Token).ConfigureAwait(false)) {
                    yield return item;
                }
            }
        }


        /// <inheritdoc />
        public async IAsyncEnumerable<AdapterProperty> GetTagProperties(
            IAdapterCallContext context, 
            GetTagPropertiesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var client = GetClient();
                await foreach (var item in client.TagSearch.GetTagPropertiesAsync(AdapterId, request, context?.ToRequestMetadata(), ctSource.Token).ConfigureAwait(false)) {
                    yield return item;
                }
            }
        }

    }
}
