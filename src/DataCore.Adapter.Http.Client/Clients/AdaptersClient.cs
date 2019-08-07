using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.Http.Client.Clients {

    /// <summary>
    /// Client for querying adapter metadata.
    /// </summary>
    public class AdaptersClient {

        /// <summary>
        /// The URL prefix for API calls.
        /// </summary>
        private const string UrlPrefix = "api/data-core/v1.0/adapters";

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
        /// Gets information about the available adapters in the remote host.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return information about the available adapters.
        /// </returns>
        public async Task<IEnumerable<AdapterDescriptor>> GetAdaptersAsync(CancellationToken cancellationToken = default) {
            using (var response = await _client.HttpClient.GetAsync(UrlPrefix, cancellationToken).ConfigureAwait(false)) {
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsAsync<IEnumerable<AdapterDescriptor>>(cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Gets extended information about the an adapter in the remote host.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
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
        public async Task<AdapterDescriptorExtended> GetAdapterAsync(string adapterId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}";
            using (var response = await _client.HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false)) {
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsAsync<AdapterDescriptorExtended>(cancellationToken).ConfigureAwait(false);
            }
        }

    }
}
