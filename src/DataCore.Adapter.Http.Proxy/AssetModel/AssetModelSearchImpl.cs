﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.AssetModel.Features;
using DataCore.Adapter.AssetModel.Models;

namespace DataCore.Adapter.Http.Proxy.AssetModel {
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
        public AssetModelSearchImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public ChannelReader<AssetModelNode> FindAssetModelNodes(IAdapterCallContext context, FindAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateAssetModelNodeChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.AssetModel.FindNodesAsync(AdapterId, request, context?.User, ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, cancellationToken);

            return result;
        }

    }
}
