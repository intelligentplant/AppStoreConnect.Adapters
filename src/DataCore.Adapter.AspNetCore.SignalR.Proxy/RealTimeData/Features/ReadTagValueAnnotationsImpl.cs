using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {
    internal class ReadTagValueAnnotationsImpl : ProxyAdapterFeature, IReadTagValueAnnotations {

        public ReadTagValueAnnotationsImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        public ChannelReader<TagValueAnnotationQueryResult> ReadAnnotations(IAdapterCallContext context, ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueAnnotationChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var connection = await GetHubConnection(ct).ConfigureAwait(false);
                var hubChannel = await connection.StreamAsChannelAsync<TagValueAnnotationQueryResult>(
                    "ReadTagValueAnnotations",
                    AdapterId,
                    request,
                    cancellationToken
                ).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, cancellationToken);

            return result;
        }

        public async Task<TagValueAnnotation> ReadAnnotation(IAdapterCallContext context, ReadAnnotationRequest request, CancellationToken cancellationToken) {
            var connection = await GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<TagValueAnnotation>(
                "ReadTagValueAnnotation",
                AdapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }
    }
}
