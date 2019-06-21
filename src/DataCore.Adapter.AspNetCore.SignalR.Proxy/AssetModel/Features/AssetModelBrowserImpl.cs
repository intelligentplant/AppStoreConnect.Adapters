using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.AssetModel.Features;
using DataCore.Adapter.AssetModel.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.AssetModel.Features {
    internal class AssetModelBrowserImpl : ProxyAdapterFeature, IAssetModelBrowser {

        public AssetModelBrowserImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        public ChannelReader<AssetModelNode> BrowseAssetModelNodes(IAdapterCallContext context, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateBoundedAssetModelNodeChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var connection = await this.GetAssetModelBrowserHubConnection(ct).ConfigureAwait(false);
                var hubChannel = await connection.StreamAsChannelAsync<AssetModelNode>(
                    "BrowseAssetModelNodes",
                    AdapterId,
                    request,
                    cancellationToken
                ).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, cancellationToken);

            return result;
        }

        public ChannelReader<AssetModelNode> GetAssetModelNodes(IAdapterCallContext context, GetAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateBoundedAssetModelNodeChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var connection = await this.GetTagValuesHubConnection(ct).ConfigureAwait(false);
                var hubChannel = await connection.StreamAsChannelAsync<AssetModelNode>(
                    "GetAssetModelNodes",
                    AdapterId,
                    request,
                    cancellationToken
                ).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, cancellationToken);

            return result;
        }

        public ChannelReader<AssetModelNode> FindAssetModelNodes(IAdapterCallContext context, FindAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateBoundedAssetModelNodeChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var connection = await this.GetTagValuesHubConnection(ct).ConfigureAwait(false);
                var hubChannel = await connection.StreamAsChannelAsync<AssetModelNode>(
                    "FindAssetModelNodes",
                    AdapterId,
                    request,
                    cancellationToken
                ).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, cancellationToken);

            return result;
        }

    }
}
