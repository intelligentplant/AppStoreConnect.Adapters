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

    /// <summary>
    /// Implements <see cref="IReadTagValueAnnotations"/>.
    /// </summary>
    internal class ReadTagValueAnnotationsImpl : ProxyAdapterFeature, IReadTagValueAnnotations {

        /// <summary>
        /// Creates a new <see cref="ReadTagValueAnnotationsImpl"/> object.
        /// </summary>
        /// <param name="proxy"></param>
        public ReadTagValueAnnotationsImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public ChannelReader<TagValueAnnotationQueryResult> ReadAnnotations(IAdapterCallContext context, ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueAnnotationChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var hubChannel = await client.TagValueAnnotations.ReadAnnotationsAsync(AdapterId, request, ct).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, cancellationToken);

            return result;
        }

        /// <inheritdoc/>
        public async Task<TagValueAnnotation> ReadAnnotation(IAdapterCallContext context, ReadAnnotationRequest request, CancellationToken cancellationToken) {
            var client = GetClient();
            return await client.TagValueAnnotations.ReadAnnotationAsync(AdapterId, request, cancellationToken).ConfigureAwait(false);
        }
    }
}
