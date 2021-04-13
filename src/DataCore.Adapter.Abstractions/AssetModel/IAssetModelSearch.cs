using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Feature for searching an adapter's asset model hierarchy.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.AssetModel.AssetModelSearch,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_AssetModelSearch),
        Description = nameof(AbstractionsResources.Description_AssetModelSearch)
    )]
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
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the matching asset model nodes.
        /// </returns>
        IAsyncEnumerable<AssetModelNode> FindAssetModelNodes(
            IAdapterCallContext context, 
            FindAssetModelNodesRequest request, 
            CancellationToken cancellationToken
        );

    }
}
