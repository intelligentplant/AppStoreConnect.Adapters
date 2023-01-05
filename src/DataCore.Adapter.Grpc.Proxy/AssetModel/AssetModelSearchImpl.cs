using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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
        public async IAsyncEnumerable<Adapter.AssetModel.AssetModelNode> FindAssetModelNodes(
            IAdapterCallContext context, 
            Adapter.AssetModel.FindAssetModelNodesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<AssetModelBrowserService.AssetModelBrowserServiceClient>();
            var grpcRequest = new FindAssetModelNodesRequest() {
                AdapterId = AdapterId,
                Name = request.Name ?? string.Empty,
                Description = request.Description ?? string.Empty,
                PageSize = request.PageSize,
                Page = request.Page
            };
            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = client.FindAssetModelNodes(grpcRequest, GetCallOptions(context, cancellationToken));

            while (await grpcResponse.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                if (grpcResponse.ResponseStream.Current == null) {
                    continue;
                }
                yield return grpcResponse.ResponseStream.Current.ToAdapterAssetModelNode();
            }
        }

    }

}
