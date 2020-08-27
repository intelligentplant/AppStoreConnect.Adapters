using System.Threading;
using System.Threading.Channels;
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
        public async Task<ChannelReader<AssetModelNode>> BrowseAssetModelNodes(IAdapterCallContext context, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken) {
            SignalRAdapterProxy.ValidateObject(request);

            var client = GetClient();
            var hubChannel = await client.AssetModel.BrowseAssetModelNodesAsync(
                AdapterId, 
                request, 
                cancellationToken
            ).ConfigureAwait(false);

            var result = ChannelExtensions.CreateAssetModelNodeChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await hubChannel.Forward(ch, ct).ConfigureAwait(false);
            }, true, TaskScheduler, cancellationToken);

            return result;
        }

        /// <inheritdoc />
        public async Task<ChannelReader<AssetModelNode>> GetAssetModelNodes(IAdapterCallContext context, GetAssetModelNodesRequest request, CancellationToken cancellationToken) {
            SignalRAdapterProxy.ValidateObject(request);

            var client = GetClient();
            var hubChannel = await client.AssetModel.GetAssetModelNodesAsync(
                AdapterId, 
                request, 
                cancellationToken
            ).ConfigureAwait(false);

            var result = ChannelExtensions.CreateAssetModelNodeChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await hubChannel.Forward(ch, ct).ConfigureAwait(false);
            }, true, TaskScheduler, cancellationToken);

            return result;
        }


    }
}
