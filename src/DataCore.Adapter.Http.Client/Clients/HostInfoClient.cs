using System;
using System.Net.Http;
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
        private const string UrlPrefix = "api/data-core/v1.0/host-info";

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
            RequestMetadata metadata = null,
            CancellationToken cancellationToken = default
        ) {
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, UrlPrefix).AddRequestMetadata(metadata);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<HostInfo>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }

    }
}
