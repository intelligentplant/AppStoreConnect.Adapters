using System.Threading;
using System.Threading.Channels;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Feature for browsing an adapter's asset model hierarchy.
    /// </summary>
    public interface IAssetModelBrowse : IAdapterFeature {

        /// <summary>
        /// Browses nodes in the adapter's asset model.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The asset model query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching asset model nodes.
        /// </returns>
        ChannelReader<AssetModelNode> BrowseAssetModelNodes(IAdapterCallContext context, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Gets specific nodes from the adapter's asset model.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The asset model query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching asset model nodes.
        /// </returns>
        ChannelReader<AssetModelNode> GetAssetModelNodes(IAdapterCallContext context, GetAssetModelNodesRequest request, CancellationToken cancellationToken);

    }
}
