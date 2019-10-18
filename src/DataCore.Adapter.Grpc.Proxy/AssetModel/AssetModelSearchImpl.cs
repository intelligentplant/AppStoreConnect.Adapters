using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.AssetModel;

namespace DataCore.Adapter.Grpc.Proxy.AssetModel.Features {

    /// <summary>
    /// <see cref="IAssetModelSearch"/> implementation.
    /// </summary>
    internal class AssetModelSearchImpl : ProxyAdapterFeature, IAssetModelSearch {

        /// <summary>
        /// Creates a new <see cref="AssetModelBrowseImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public AssetModelSearchImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public ChannelReader<Adapter.AssetModel.AssetModelNode> FindAssetModelNodes(IAdapterCallContext context, Adapter.AssetModel.FindAssetModelNodesRequest request, CancellationToken cancellationToken) {
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
            }, true, TaskScheduler, cancellationToken);

            return result;
        }

    }

}
