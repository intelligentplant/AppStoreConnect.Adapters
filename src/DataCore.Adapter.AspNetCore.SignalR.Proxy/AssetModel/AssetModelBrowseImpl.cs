using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.AssetModel;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.AssetModel.Features {

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
        public AssetModelBrowseImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public async IAsyncEnumerable<AssetModelNode> BrowseAssetModelNodes(
            IAdapterCallContext context, 
            BrowseAssetModelNodesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = GetClient();
            await foreach (var item in client.AssetModel.BrowseAssetModelNodesAsync(AdapterId, request, cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<AssetModelNode> GetAssetModelNodes(
            IAdapterCallContext context, 
            GetAssetModelNodesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = GetClient();
            await foreach (var item in client.AssetModel.GetAssetModelNodesAsync(AdapterId, request, cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
        }


    }
}
