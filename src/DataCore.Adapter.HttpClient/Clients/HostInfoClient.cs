using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.Http.Clients {
    public class HostInfoClient {

        private const string UrlPrefix = "api/data-core/v1.0/host-info";

        private readonly AdapterHttpClient _client;


        public HostInfoClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<HostInfo> GetHostInfoAsync(CancellationToken cancellationToken = default) { 
            var response = await _client.HttpClient.GetAsync(UrlPrefix, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<HostInfo>(cancellationToken).ConfigureAwait(false);
        }

    }
}
