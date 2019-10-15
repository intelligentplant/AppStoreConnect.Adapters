using System.Threading;
using System.Threading.Channels;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Feature for searching an adapter's asset model hierarchy.
    /// </summary>
    public interface IAssetModelSearch : IAdapterFeature {

        /// <summary>
        /// Searches for nodes in the adapter's asset model.
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
        ChannelReader<AssetModelNode> FindAssetModelNodes(IAdapterCallContext context, FindAssetModelNodesRequest request, CancellationToken cancellationToken);

    }
}
