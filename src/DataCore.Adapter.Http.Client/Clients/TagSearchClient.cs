using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Http.Client.Clients {

    /// <summary>
    /// Client for performing adapter tag searches.
    /// </summary>
    public class TagSearchClient {

        /// <summary>
        /// The URL prefix for API calls.
        /// </summary>
        private const string UrlPrefix = "api/data-core/v1.0/tags";

        /// <summary>
        /// The adapter HTTP client that is used to perform the requests.
        /// </summary>
        private readonly AdapterHttpClient _client;


        /// <summary>
        /// Creates a new <see cref="TagSearchClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter HTTP client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public TagSearchClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Performs a tag search.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="principal">
        ///   The principal to associate with the outgoing request.
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
        public async Task<IEnumerable<TagDefinition>> FindTagsAsync(string adapterId, FindTagsRequest request, ClaimsPrincipal principal = null, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/find";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<FindTagsRequest>(request, new JsonMediaTypeFormatter())
            }.AddStateProperty(principal);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<TagDefinition>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }


        /// <summary>
        /// Gets tags by ID or name.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="principal">
        ///   The principal to associate with the outgoing request.
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
        public async Task<IEnumerable<TagDefinition>> GetTagsAsync(string adapterId, GetTagsRequest request, ClaimsPrincipal principal = null, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/get-by-id";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<GetTagsRequest>(request, new JsonMediaTypeFormatter())
            }.AddStateProperty(principal);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<TagDefinition>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }

    }
}
