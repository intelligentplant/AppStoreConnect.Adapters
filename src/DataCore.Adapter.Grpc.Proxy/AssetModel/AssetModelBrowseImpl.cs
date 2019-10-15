using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.AssetModel;

namespace DataCore.Adapter.Grpc.Proxy.AssetModel.Features {

    /// <summary>
    /// <see cref="IAssetModelBrowse"/> implementation.
    /// </summary>
    internal class AssetModelBrowseImpl : ProxyAdapterFeature, IAssetModelBrowse {

        /// <summary>
        /// Creates a new <see cref="AssetModelBrowseImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public AssetModelBrowseImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public ChannelReader<Adapter.AssetModel.AssetModelNode> BrowseAssetModelNodes(IAdapterCallContext context, Adapter.AssetModel.BrowseAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateAssetModelNodeChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<AssetModelBrowserService.AssetModelBrowserServiceClient>();
                var grpcRequest = new BrowseAssetModelNodesRequest() {
                    AdapterId = AdapterId,
                    ParentId = request.ParentId ?? string.Empty,
                    PageSize = request.PageSize,
                    Page = request.Page
                };
                var grpcResponse = client.BrowseAssetModelNodes(grpcRequest, GetCallOptions(context, ct));

                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterAssetModelNode(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcResponse.Dispose();
                }
            }, true, cancellationToken);

            return result;
        }


        /// <inheritdoc/>
        public ChannelReader<Adapter.AssetModel.AssetModelNode> GetAssetModelNodes(IAdapterCallContext context, Adapter.AssetModel.GetAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateAssetModelNodeChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<AssetModelBrowserService.AssetModelBrowserServiceClient>();
                var grpcRequest = new GetAssetModelNodesRequest() {
                    AdapterId = AdapterId
                };
                grpcRequest.Nodes.AddRange(request.Nodes);
                var grpcResponse = client.GetAssetModelNodes(grpcRequest, GetCallOptions(context, ct));

                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterAssetModelNode(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcResponse.Dispose();
                }
            }, true, cancellationToken);

            return result;
        }

    }

}
