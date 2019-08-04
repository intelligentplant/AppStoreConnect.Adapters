using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Http.Clients {
    public class TagSearchClient {

        private const string UrlPrefix = "api/data-core/v1.0/tags";

        private readonly AdapterHttpClient _client;


        public TagSearchClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<IEnumerable<TagDefinition>> FindTagsAsync(string adapterId, FindTagsRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (request != null) {
                throw new ArgumentNullException(nameof(request));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/find";
            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<TagDefinition>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<TagDefinition>> GetTagsAsync(string adapterId, GetTagsRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (request != null) {
                throw new ArgumentNullException(nameof(request));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/get-by-id";
            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<TagDefinition>>(cancellationToken).ConfigureAwait(false);
        }

    }
}
