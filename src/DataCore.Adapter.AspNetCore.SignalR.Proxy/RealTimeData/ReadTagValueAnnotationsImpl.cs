using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

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
        public async IAsyncEnumerable<TagValueAnnotationQueryResult> ReadAnnotations(
            IAdapterCallContext context,
            ReadAnnotationsRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = GetClient();
            await foreach (var item in client.TagValueAnnotations.ReadAnnotationsAsync(
                AdapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }

        /// <inheritdoc/>
        public async Task<TagValueAnnotationExtended?> ReadAnnotation(IAdapterCallContext context, ReadAnnotationRequest request, CancellationToken cancellationToken) {
            var client = GetClient();
            return await client.TagValueAnnotations.ReadAnnotationAsync(
                AdapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }
    }
}
