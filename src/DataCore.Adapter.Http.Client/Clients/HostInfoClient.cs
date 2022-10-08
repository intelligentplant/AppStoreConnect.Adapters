using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.Http.Client.Clients {

    /// <summary>
    /// Client for querying adapter host metadata.
    /// </summary>
    public class HostInfoClient {

        /// <summary>
        /// The URL prefix for API calls.
        /// </summary>
        private string UrlPrefix => string.Concat(_client.GetBaseUrl(), "/host-info");

        /// <summary>
        /// The adapter HTTP client that is used to perform the requests.
        /// </summary>
        private readonly AdapterHttpClient _client;


        /// <summary>
        /// Creates a new <see cref="HostInfoClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter HTTP client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public HostInfoClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Requests information about the remote adapter host.
        /// </summary>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return information about the remote host.
        /// </returns>
        public async Task<HostInfo> GetHostInfoAsync(
            RequestMetadata? metadata = null,
            CancellationToken cancellationToken = default
        ) {
            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Get, UrlPrefix, metadata))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                await httpResponse.ThrowOnErrorResponse().ConfigureAwait(false);

                return (await httpResponse.Content.ReadFromJsonAsync<HostInfo>(_client.JsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            }
        }


        /// <summary>
        /// Gets the APIs that are enabled on the remote host.
        /// </summary>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return information about the available APIs.
        /// </returns>
        public async Task<IEnumerable<ApiDescriptor>> GetAvailableApisAsync(
            RequestMetadata? metadata = null, 
            CancellationToken cancellationToken = default
        ) {
            var url = UrlPrefix + "/available-apis";
            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Get, url, metadata))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                await httpResponse.ThrowOnErrorResponse().ConfigureAwait(false);

                return (await httpResponse.Content.ReadFromJsonAsync<IEnumerable<ApiDescriptor>>(_client.JsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            }
        }

    }
}
