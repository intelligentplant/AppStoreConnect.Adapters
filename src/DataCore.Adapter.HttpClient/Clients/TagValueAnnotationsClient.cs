using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Http.Clients {
    public class TagValueAnnotationsClient {

        private const string UrlPrefix = "api/data-core/v1.0/tag-annotations";

        private readonly AdapterHttpClient _client;


        public TagValueAnnotationsClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<IEnumerable<TagValueAnnotationQueryResult>> ReadAnnotationsAsync(string adapterId, ReadAnnotationsRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}";

            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<TagValueAnnotationQueryResult>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<TagValueAnnotation> ReadAnnotationAsync(string adapterId, string tagId, string annotationId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (string.IsNullOrWhiteSpace(tagId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(tagId));
            }
            if (string.IsNullOrWhiteSpace(annotationId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(annotationId));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/{Uri.EscapeDataString(tagId)}/{Uri.EscapeDataString(annotationId)}";

            var response = await _client.HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<TagValueAnnotation>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<WriteTagValueAnnotationResult> CreateAnnotationAsync(string adapterId, string tagId, TagValueAnnotationBase annotation, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (string.IsNullOrWhiteSpace(tagId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(tagId));
            }
            if (annotation == null) {
                throw new ArgumentNullException(nameof(annotation));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/{Uri.EscapeDataString(tagId)}/create";

            var response = await _client.HttpClient.PostAsJsonAsync(url, annotation, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<WriteTagValueAnnotationResult>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<WriteTagValueAnnotationResult> UpdateAnnotationAsync(string adapterId, string tagId, string annotationId, TagValueAnnotationBase annotation, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (string.IsNullOrWhiteSpace(tagId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(tagId));
            }
            if (string.IsNullOrWhiteSpace(annotationId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(annotationId));
            }
            if (annotation == null) {
                throw new ArgumentNullException(nameof(annotation));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/{Uri.EscapeDataString(tagId)}/{Uri.EscapeDataString(annotationId)}";

            var response = await _client.HttpClient.PutAsJsonAsync(url, annotation, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<WriteTagValueAnnotationResult>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<WriteTagValueAnnotationResult> DeleteAnnotationAsync(string adapterId, string tagId, string annotationId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (string.IsNullOrWhiteSpace(tagId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(tagId));
            }
            if (string.IsNullOrWhiteSpace(annotationId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(annotationId));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/{Uri.EscapeDataString(tagId)}/{Uri.EscapeDataString(annotationId)}";

            var response = await _client.HttpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<WriteTagValueAnnotationResult>(cancellationToken).ConfigureAwait(false);
        }

    }
}
