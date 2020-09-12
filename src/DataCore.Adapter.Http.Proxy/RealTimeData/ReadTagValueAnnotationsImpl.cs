using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

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
        public Task<ChannelReader<TagValueAnnotationQueryResult>> ReadAnnotations(IAdapterCallContext context, ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            HttpAdapterProxy.ValidateObject(request);

            var result = ChannelExtensions.CreateTagValueAnnotationChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.TagValueAnnotations.ReadAnnotationsAsync(AdapterId, request, context?.ToRequestMetadata(), ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, TaskScheduler, cancellationToken);

            return Task.FromResult(result.Reader);
        }

        /// <inheritdoc/>
        public async Task<TagValueAnnotationExtended> ReadAnnotation(IAdapterCallContext context, ReadAnnotationRequest request, CancellationToken cancellationToken) {
            HttpAdapterProxy.ValidateObject(request);

            var client = GetClient();
            return await client.TagValueAnnotations.ReadAnnotationAsync(AdapterId, request, context?.ToRequestMetadata(), cancellationToken).ConfigureAwait(false);
        }
    }

}
