using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Wrapper for <see cref="IAssetModelBrowse"/>.
    /// </summary>
    internal class AssetModelSearchWrapper : AdapterFeatureWrapper<IAssetModelSearch>, IAssetModelSearch {

        /// <summary>
        /// Creates a new <see cref="AssetModelSearchWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal AssetModelSearchWrapper(AdapterCore adapter, IAssetModelSearch innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<AssetModelNode> IAssetModelSearch.FindAssetModelNodes(IAdapterCallContext context, FindAssetModelNodesRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.FindAssetModelNodes, cancellationToken);
        }

    }
}
