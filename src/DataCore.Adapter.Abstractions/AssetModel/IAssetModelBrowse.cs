using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Feature for browsing an adapter's asset model hierarchy.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.AssetModel.AssetModelBrowse,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_AssetModelBrowse),
        Description = nameof(AbstractionsResources.Description_AssetModelBrowse)
    )]
    public interface IAssetModelBrowse : IAdapterFeature {

        /// <summary>
        /// Browses nodes in the adapter's asset model.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The asset model query. If a <see cref="BrowseAssetModelNodesRequest.ParentId"/> 
        ///   value is specified, this node should not be returned in the result.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the matching asset model nodes.
        /// </returns>
        IAsyncEnumerable<AssetModelNode> BrowseAssetModelNodes(
            IAdapterCallContext context, 
            BrowseAssetModelNodesRequest request, 
            CancellationToken cancellationToken
        );

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
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the matching asset model nodes.
        /// </returns>
        IAsyncEnumerable<AssetModelNode> GetAssetModelNodes(
            IAdapterCallContext context, 
            GetAssetModelNodesRequest request, 
            CancellationToken cancellationToken
        );

    }
}
