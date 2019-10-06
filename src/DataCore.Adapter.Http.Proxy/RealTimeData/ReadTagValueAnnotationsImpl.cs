﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Http.Proxy.RealTimeData {
    /// <summary>
    /// Implements <see cref="IReadTagValueAnnotations"/>.
    /// </summary>
    internal class ReadTagValueAnnotationsImpl : ProxyAdapterFeature, IReadTagValueAnnotations {

        /// <summary>
        /// Creates a new <see cref="ReadTagValueAnnotationsImpl"/> object.
        /// </summary>
        /// <param name="proxy"></param>
        public ReadTagValueAnnotationsImpl(HttpAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public ChannelReader<TagValueAnnotationQueryResult> ReadAnnotations(IAdapterCallContext context, ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueAnnotationChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.TagValueAnnotations.ReadAnnotationsAsync(AdapterId, request, context?.User, ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, cancellationToken);

            return result;
        }

        /// <inheritdoc/>
        public async Task<TagValueAnnotation> ReadAnnotation(IAdapterCallContext context, ReadAnnotationRequest request, CancellationToken cancellationToken) {
            var client = GetClient();
            return await client.TagValueAnnotations.ReadAnnotationAsync(AdapterId, request, context?.User, cancellationToken).ConfigureAwait(false);
        }
    }

}
