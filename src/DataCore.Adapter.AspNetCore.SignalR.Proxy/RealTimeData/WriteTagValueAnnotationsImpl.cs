using System;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {

    /// <summary>
    /// Implements <see cref="IWriteTagValueAnnotations"/>.
    /// </summary>
    internal class WriteTagValueAnnotationsImpl : ProxyAdapterFeature, IWriteTagValueAnnotations {

        /// <summary>
        /// Creates a new <see cref="WriteTagValueAnnotationsImpl"/>.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public WriteTagValueAnnotationsImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public async Task<WriteTagValueAnnotationResult> CreateAnnotation(IAdapterCallContext context, CreateAnnotationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();
            return await client.TagValueAnnotations.CreateAnnotationAsync(AdapterId, request, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<WriteTagValueAnnotationResult> UpdateAnnotation(IAdapterCallContext context, UpdateAnnotationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();
            return await client.TagValueAnnotations.UpdateAnnotationAsync(AdapterId, request, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<WriteTagValueAnnotationResult> DeleteAnnotation(IAdapterCallContext context, DeleteAnnotationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();
            return await client.TagValueAnnotations.DeleteAnnotationAsync(AdapterId, request, cancellationToken).ConfigureAwait(false);
        }
    }
}
