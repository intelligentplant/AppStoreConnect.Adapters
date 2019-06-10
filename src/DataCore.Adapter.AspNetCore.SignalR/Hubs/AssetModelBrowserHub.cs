using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AssetModel.Features;
using DataCore.Adapter.AssetModel.Models;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.AspNetCore.Hubs {

    /// <summary>
    /// SignalR hub that is used for browsing an adapter's asset model hierarchy.
    /// </summary>
    public class AssetModelBrowserHub : AdapterHubBase {

        /// <summary>
        /// Creates a new <see cref="AssetModelBrowserHub"/> object.
        /// </summary>
        /// <param name="hostInfo">
        ///   The host information.
        /// </param>
        /// <param name="adapterCallContext">
        ///   The adapter call context describing the calling user.
        /// </param>
        /// <param name="adapterAccessor">
        ///   For accessing runtime adapters.
        /// </param>
        public AssetModelBrowserHub(HostInfo hostInfo, IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor)
            : base(hostInfo, adapterCallContext, adapterAccessor) { }


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
            var adapter = await ResolveAdapterAndFeature<IAssetModelBrowser>(adapterId, cancellationToken).ConfigureAwait(false);
            return adapter.Feature.BrowseAssetModelNodes(AdapterCallContext, request, cancellationToken);
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
            var adapter = await ResolveAdapterAndFeature<IAssetModelBrowser>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.GetAssetModelNodes(AdapterCallContext, request, cancellationToken);
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
            var adapter = await ResolveAdapterAndFeature<IAssetModelBrowser>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.FindAssetModelNodes(AdapterCallContext, request, cancellationToken);
        }

    }
}
