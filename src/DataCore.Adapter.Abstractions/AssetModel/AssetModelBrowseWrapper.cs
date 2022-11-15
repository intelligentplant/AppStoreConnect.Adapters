using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Wrapper for <see cref="IAssetModelBrowse"/>.
    /// </summary>
    internal class AssetModelBrowseWrapper : AdapterFeatureWrapper<IAssetModelBrowse>, IAssetModelBrowse {

        /// <summary>
        /// Creates a new <see cref="AssetModelBrowseWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal AssetModelBrowseWrapper(AdapterCore adapter, IAssetModelBrowse innerFeature) 
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<AssetModelNode> IAssetModelBrowse.BrowseAssetModelNodes(IAdapterCallContext context, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.BrowseAssetModelNodes, cancellationToken);
        }


        /// <inheritdoc/>
        IAsyncEnumerable<AssetModelNode> IAssetModelBrowse.GetAssetModelNodes(IAdapterCallContext context, GetAssetModelNodesRequest request,CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.GetAssetModelNodes, cancellationToken);
        }

    }
}
