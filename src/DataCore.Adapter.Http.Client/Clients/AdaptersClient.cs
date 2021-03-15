using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;

namespace DataCore.Adapter.Http.Client.Clients {

    /// <summary>
    /// Client for querying adapter metadata.
    /// </summary>
    public class AdaptersClient {

        /// <summary>
        /// The URL prefix for API calls.
        /// </summary>
        private string UrlPrefix => _client.CompatibilityVersion == CompatibilityVersion.Version_1_0 
            ? "api/data-core/v1.0/adapters" 
            : "api/app-store-connect/v1.0/adapters";

        /// <summary>
        /// The adapter HTTP client that is used to perform the requests.
        /// </summary>
        private readonly AdapterHttpClient _client;


        /// <summary>
        /// Creates a new <see cref="AdaptersClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter HTTP client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public AdaptersClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Finds matching adapters in the remote host.
        /// </summary>
        /// <param name="request">
        ///   The search filter.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return information about the available adapters.
        /// </returns>
        public async Task<IEnumerable<AdapterDescriptor>> FindAdaptersAsync(
            FindAdaptersRequest request,
            RequestMetadata? metadata = null,
            CancellationToken cancellationToken = default
        ) {
            AdapterHttpClient.ValidateObject(request);

            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Post, UrlPrefix, request, metadata, _client.JsonSerializerOptions))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return (await httpResponse.Content.ReadFromJsonAsync<IEnumerable<AdapterDescriptor>>(_client.JsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            }
        }



        /// <summary>
        /// Gets extended information about the an adapter in the remote host.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return information about the adapter.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        public async Task<AdapterDescriptorExtended> GetAdapterAsync(
            string adapterId,
            RequestMetadata? metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}";

            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Get, url, metadata))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return (await httpResponse.Content.ReadFromJsonAsync<AdapterDescriptorExtended>(_client.JsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            }
        }


        /// <summary>
        /// Gets the current health check status for an adapter in the remote host.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the adapter health status.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        public async Task<HealthCheckResult> CheckAdapterHealthAsync(
            string adapterId, 
            RequestMetadata? metadata = null, 
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/health-status";

            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Get, url, metadata))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return (await httpResponse.Content.ReadFromJsonAsync<HealthCheckResult>(_client.JsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            }
        }

    }
}
