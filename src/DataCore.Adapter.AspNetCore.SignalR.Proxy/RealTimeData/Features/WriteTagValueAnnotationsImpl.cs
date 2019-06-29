using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {

    internal class WriteTagValueAnnotationsImpl : ProxyAdapterFeature, IWriteTagValueAnnotations {

        public WriteTagValueAnnotationsImpl(SignalRAdapterProxy proxy) : base(proxy) { }

        public async Task<WriteTagValueAnnotationResult> CreateAnnotation(IAdapterCallContext context, CreateAnnotationRequest request, CancellationToken cancellationToken) {
            var connection = await GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<WriteTagValueAnnotationResult>(
                "CreateAnnotation",
                AdapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }

        public async Task<WriteTagValueAnnotationResult> UpdateAnnotation(IAdapterCallContext context, UpdateAnnotationRequest request, CancellationToken cancellationToken) {
            var connection = await GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<WriteTagValueAnnotationResult>(
                "UpdateAnnotation",
                AdapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }

        public async Task<WriteTagValueAnnotationResult> DeleteAnnotation(IAdapterCallContext context, DeleteAnnotationRequest request, CancellationToken cancellationToken) {
            var connection = await GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<WriteTagValueAnnotationResult>(
                "DeleteAnnotation",
                AdapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }
    }
}
