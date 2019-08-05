using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {
    public class TagValueAnnotationsClient {

        private readonly AdapterSignalRClient _client;


        public TagValueAnnotationsClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<TagValueAnnotation> ReadAnnotationAsync(string adapterId, ReadAnnotationRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<TagValueAnnotation>(
                "ReadAnnotation",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<TagValueAnnotationQueryResult>> ReadAnnotationsAsync(string adapterId, ReadAnnotationsRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueAnnotationQueryResult>(
                "ReadAnnotations",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<WriteTagValueAnnotationResult> CreateAnnotationAsync(string adapterId, CreateAnnotationRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<WriteTagValueAnnotationResult>(
                "CreateAnnotation",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<WriteTagValueAnnotationResult> UpdateAnnotationAsync(string adapterId, UpdateAnnotationRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<WriteTagValueAnnotationResult>(
                "UpdateAnnotation",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<WriteTagValueAnnotationResult> DeleteAnnotationAsync(string adapterId, DeleteAnnotationRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<WriteTagValueAnnotationResult>(
                "DeleteAnnotation",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }

    }
}
