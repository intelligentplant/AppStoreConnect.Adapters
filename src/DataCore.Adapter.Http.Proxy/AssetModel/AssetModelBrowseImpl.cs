using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using DataCore.Adapter.AssetModel;

namespace DataCore.Adapter.Http.Proxy.AssetModel {
    /// <summary>
    /// Implements <see cref="IAssetModelBrowse"/>.
    /// </summary>
    internal class AssetModelBrowseImpl : ProxyAdapterFeature, IAssetModelBrowse {

        /// <summary>
        /// Creates a new <see cref="AssetModelBrowseImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public AssetModelBrowseImpl(HttpAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public async IAsyncEnumerable<AssetModelNode> BrowseAssetModelNodes(
            IAdapterCallContext context, 
            BrowseAssetModelNodesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var clientResponse = await client.AssetModel.BrowseNodesAsync(AdapterId, request, context.ToRequestMetadata(), ctSource.Token).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    yield return item;
                }
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<AssetModelNode> GetAssetModelNodes(
            IAdapterCallContext context, 
            GetAssetModelNodesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var clientResponse = await client.AssetModel.GetNodesAsync(AdapterId, request, context.ToRequestMetadata(), ctSource.Token).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    yield return item;
                }
            }
        }

    }
}
