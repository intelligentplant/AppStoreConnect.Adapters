using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.AssetModel;

namespace DataCore.Adapter.Http.Proxy.AssetModel {
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
        public AssetModelBrowseImpl(HttpAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public Task<ChannelReader<AssetModelNode>> BrowseAssetModelNodes(IAdapterCallContext context, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            HttpAdapterProxy.ValidateObject(request);
            
            var result = ChannelExtensions.CreateAssetModelNodeChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.AssetModel.BrowseNodesAsync(AdapterId, request, context.ToRequestMetadata(), ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, TaskScheduler, cancellationToken);

            return Task.FromResult(result.Reader);
        }

        /// <inheritdoc />
        public Task<ChannelReader<AssetModelNode>> GetAssetModelNodes(IAdapterCallContext context, GetAssetModelNodesRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            HttpAdapterProxy.ValidateObject(request);

            var result = ChannelExtensions.CreateAssetModelNodeChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.AssetModel.GetNodesAsync(AdapterId, request, context?.ToRequestMetadata(), ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, TaskScheduler, cancellationToken);

            return Task.FromResult(result.Reader);
        }

    }
}
