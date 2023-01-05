using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

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
        public async IAsyncEnumerable<Adapter.AssetModel.AssetModelNode> BrowseAssetModelNodes(
            IAdapterCallContext context, 
            Adapter.AssetModel.BrowseAssetModelNodesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<AssetModelBrowserService.AssetModelBrowserServiceClient>();
            var grpcRequest = new BrowseAssetModelNodesRequest() {
                AdapterId = AdapterId,
                ParentId = request.ParentId ?? string.Empty,
                PageSize = request.PageSize,
                Page = request.Page
            };
            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = client.BrowseAssetModelNodes(grpcRequest, GetCallOptions(context, cancellationToken));

            while (await grpcResponse.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                if (grpcResponse.ResponseStream.Current == null) {
                    continue;
                }
                yield return grpcResponse.ResponseStream.Current.ToAdapterAssetModelNode();
            }
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<Adapter.AssetModel.AssetModelNode> GetAssetModelNodes(
            IAdapterCallContext context, 
            Adapter.AssetModel.GetAssetModelNodesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<AssetModelBrowserService.AssetModelBrowserServiceClient>();
            var grpcRequest = new GetAssetModelNodesRequest() {
                AdapterId = AdapterId
            };
            grpcRequest.Nodes.AddRange(request.Nodes);
            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = client.GetAssetModelNodes(grpcRequest, GetCallOptions(context, cancellationToken));

            while (await grpcResponse.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                if (grpcResponse.ResponseStream.Current == null) {
                    continue;
                }
                yield return grpcResponse.ResponseStream.Current.ToAdapterAssetModelNode();
            }
        }

    }

}
