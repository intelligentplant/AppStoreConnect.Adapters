using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.AssetModel;
using DataCore.Adapter.AssetModel.Features;

namespace DataCore.Adapter.Grpc.Proxy.AssetModel.Features {

    /// <summary>
    /// <see cref="IAssetModelBrowser"/> implementation.
    /// </summary>
    internal class AssetModelBrowserImpl : ProxyAdapterFeature, IAssetModelBrowser {

        /// <summary>
        /// Creates a new <see cref="AssetModelBrowserImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public AssetModelBrowserImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public ChannelReader<Adapter.AssetModel.Models.AssetModelNode> BrowseAssetModelNodes(IAdapterCallContext context, Adapter.AssetModel.Models.BrowseAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateAssetModelNodeChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<AssetModelBrowserService.AssetModelBrowserServiceClient>();
                var grpcRequest = new BrowseAssetModelNodesRequest() {
                    AdapterId = AdapterId,
                    ParentId = request.ParentId ?? string.Empty,
                    Depth = request.Depth,
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
        public ChannelReader<Adapter.AssetModel.Models.AssetModelNode> GetAssetModelNodes(IAdapterCallContext context, Adapter.AssetModel.Models.GetAssetModelNodesRequest request, CancellationToken cancellationToken) {
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


        /// <inheritdoc/>
        public ChannelReader<Adapter.AssetModel.Models.AssetModelNode> FindAssetModelNodes(IAdapterCallContext context, Adapter.AssetModel.Models.FindAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateAssetModelNodeChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<AssetModelBrowserService.AssetModelBrowserServiceClient>();
                var grpcRequest = new FindAssetModelNodesRequest() {
                    AdapterId = AdapterId,
                    Name = request.Name ?? string.Empty,
                    Description = request.Description ?? string.Empty,
                    PageSize = request.PageSize,
                    Page = request.Page
                };
                var grpcResponse = client.FindAssetModelNodes(grpcRequest, GetCallOptions(context, ct));

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
