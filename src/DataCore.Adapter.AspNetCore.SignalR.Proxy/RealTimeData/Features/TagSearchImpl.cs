using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {
    internal class TagSearchImpl : ProxyAdapterFeature, ITagSearch {

        public TagSearchImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        public ChannelReader<TagDefinition> FindTags(IAdapterCallContext context, FindTagsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagDefinitionChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var connection = await GetHubConnection(ct).ConfigureAwait(false);
                var hubChannel = await connection.StreamAsChannelAsync<TagDefinition>(
                    "FindTags",
                    AdapterId,
                    request,
                    cancellationToken
                ).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, cancellationToken);

            return result;
        }

        public ChannelReader<TagDefinition> GetTags(IAdapterCallContext context, GetTagsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagDefinitionChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var connection = await GetHubConnection(ct).ConfigureAwait(false);
                var hubChannel = await connection.StreamAsChannelAsync<TagDefinition>(
                    "GetTags",
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
