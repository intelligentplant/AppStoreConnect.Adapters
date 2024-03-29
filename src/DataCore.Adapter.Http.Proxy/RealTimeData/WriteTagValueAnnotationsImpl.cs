﻿using System;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Http.Proxy.RealTimeData {
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
        public WriteTagValueAnnotationsImpl(HttpAdapterProxy proxy) : base(proxy) { }

        /// <inheritdoc />
        public async Task<WriteTagValueAnnotationResult> CreateAnnotation(IAdapterCallContext context, CreateAnnotationRequest request, CancellationToken cancellationToken) {
            var client = GetClient();
            return await client.TagValueAnnotations.CreateAnnotationAsync(AdapterId, request, context?.ToRequestMetadata(), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<WriteTagValueAnnotationResult> UpdateAnnotation(IAdapterCallContext context, UpdateAnnotationRequest request, CancellationToken cancellationToken) {
            var client = GetClient();
            return await client.TagValueAnnotations.UpdateAnnotationAsync(AdapterId, request, context?.ToRequestMetadata(), cancellationToken).ConfigureAwait(false);

        }

        /// <inheritdoc />
        public async Task<WriteTagValueAnnotationResult> DeleteAnnotation(IAdapterCallContext context, DeleteAnnotationRequest request, CancellationToken cancellationToken) {
            var client = GetClient();
            return await client.TagValueAnnotations.DeleteAnnotationAsync(AdapterId, request, context?.ToRequestMetadata(), cancellationToken).ConfigureAwait(false);
        }
    }
}
