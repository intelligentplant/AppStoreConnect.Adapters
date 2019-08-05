using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.Http.Client.Clients {
    public class AdaptersClient {

        private const string UrlPrefix = "api/data-core/v1.0/adapters";

        private readonly AdapterHttpClient _client;


        public AdaptersClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<IEnumerable<AdapterDescriptor>> GetAdaptersAsync(CancellationToken cancellationToken = default) {
            var response = await _client.HttpClient.GetAsync(UrlPrefix, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<AdapterDescriptor>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<AdapterDescriptorExtended> GetAdapterAsync(string adapterId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}";
            var response = await _client.HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<AdapterDescriptorExtended>(cancellationToken).ConfigureAwait(false);
        }

    }
}
