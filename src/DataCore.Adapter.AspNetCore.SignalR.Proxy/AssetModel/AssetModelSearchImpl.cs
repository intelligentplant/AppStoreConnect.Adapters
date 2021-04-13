using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.AssetModel;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.AssetModel.Features {

    /// <summary>
    /// Implements <see cref="IAssetModelBrowse"/>.
    /// </summary>
    internal class AssetModelSearchImpl : ProxyAdapterFeature, IAssetModelSearch {

        /// <summary>
        /// Creates a new <see cref="AssetModelBrowseImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public AssetModelSearchImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<AssetModelNode> FindAssetModelNodes(
            IAdapterCallContext context, 
            FindAssetModelNodesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();
            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                await foreach (var item in client.AssetModel.FindAssetModelNodesAsync(AdapterId, request, ctSource.Token).ConfigureAwait(false)) {
                    yield return item;
                }
            }
        }

    }
}
