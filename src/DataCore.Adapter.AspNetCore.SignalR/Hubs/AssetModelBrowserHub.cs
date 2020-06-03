using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AssetModel;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for asset model browsing.

    public partial class AdapterHub {

        /// <summary>
        /// Browses nodes in an adapter's asset model hierarchy.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching nodes.
        /// </returns>
        public async Task<ChannelReader<AssetModelNode>> BrowseAssetModelNodes(string adapterId, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IAssetModelBrowse>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            return await adapter.Feature.BrowseAssetModelNodes(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets nodes in an adapter's asset model hierarchy by ID.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching nodes.
        /// </returns>
        public async Task<ChannelReader<AssetModelNode>> GetAssetModelNodes(string adapterId, GetAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IAssetModelBrowse>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return await adapter.Feature.GetAssetModelNodes(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Finds nodes in an adapter's asset model hierarchy that match the specified search filters.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching nodes.
        /// </returns>
        public async Task<ChannelReader<AssetModelNode>> FindAssetModelNodes(string adapterId, FindAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IAssetModelSearch>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return await adapter.Feature.FindAssetModelNodes(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }

    }
}
