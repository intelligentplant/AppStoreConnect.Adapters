using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Http.Client.Clients {

    /// <summary>
    /// Client for managing adapter tag value annotations.
    /// </summary>
    public class TagValueAnnotationsClient {

        /// <summary>
        /// The URL prefix for API calls.
        /// </summary>
        private const string UrlPrefix = "api/data-core/v1.0/tag-annotations";

        /// <summary>
        /// The adapter HTTP client that is used to perform the requests.
        /// </summary>
        private readonly AdapterHttpClient _client;


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationsClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter HTTP client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public TagValueAnnotationsClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Reads a single tag value annotation from an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the annotation.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async Task<TagValueAnnotationExtended> ReadAnnotationAsync(
            string adapterId, 
            ReadAnnotationRequest request, 
            RequestMetadata metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/{Uri.EscapeDataString(request.Tag)}/{Uri.EscapeDataString(request.AnnotationId)}";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url).AddRequestMetadata(metadata);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<TagValueAnnotationExtended>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }


        /// <summary>
        /// Reads tag value annotations from an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the results back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async Task<IEnumerable<TagValueAnnotationQueryResult>> ReadAnnotationsAsync(
            string adapterId, 
            ReadAnnotationsRequest request, 
            RequestMetadata metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<ReadAnnotationsRequest>(request, new JsonMediaTypeFormatter())
            }.AddRequestMetadata(metadata);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<TagValueAnnotationQueryResult>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }


        /// <summary>
        /// Creates a tag value annotation.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the result of the operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async Task<WriteTagValueAnnotationResult> CreateAnnotationAsync(
            string adapterId, 
            CreateAnnotationRequest request, 
            RequestMetadata metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/{Uri.EscapeDataString(request.Tag)}/create";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<TagValueAnnotation>(request.Annotation, new JsonMediaTypeFormatter())
            }.AddRequestMetadata(metadata);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<WriteTagValueAnnotationResult>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }


        /// <summary>
        /// Updates a tag value annotation.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the result of the operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async Task<WriteTagValueAnnotationResult> UpdateAnnotationAsync(
            string adapterId, 
            UpdateAnnotationRequest request, 
            RequestMetadata metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/{Uri.EscapeDataString(request.Tag)}/{Uri.EscapeDataString(request.AnnotationId)}";

            var httpRequest = new HttpRequestMessage(HttpMethod.Put, url) {
                Content = new ObjectContent<TagValueAnnotation>(request.Annotation, new JsonMediaTypeFormatter())
            }.AddRequestMetadata(metadata);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<WriteTagValueAnnotationResult>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }


        /// <summary>
        /// Deletes a tag value annotation.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the result of the operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async Task<WriteTagValueAnnotationResult> DeleteAnnotationAsync(
            string adapterId, 
            DeleteAnnotationRequest request, 
            RequestMetadata metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/{Uri.EscapeDataString(request.Tag)}/{Uri.EscapeDataString(request.AnnotationId)}";

            var httpRequest = new HttpRequestMessage(HttpMethod.Delete, url).AddRequestMetadata(metadata);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<WriteTagValueAnnotationResult>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }

    }
}
