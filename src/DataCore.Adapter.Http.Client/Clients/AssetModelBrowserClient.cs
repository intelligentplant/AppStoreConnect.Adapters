using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AssetModel.Models;

namespace DataCore.Adapter.Http.Client.Clients {
    public class AssetModelBrowserClient {

        private const string UrlPrefix = "api/data-core/v1.0/asset-model";

        private readonly AdapterHttpClient _client;


        public AssetModelBrowserClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<IEnumerable<AssetModelNode>> BrowseNodesAsync(string adapterId, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/browse";

            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<AssetModelNode>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<AssetModelNode>> GetNodesAsync(string adapterId, GetAssetModelNodesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/get-by-id";

            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<AssetModelNode>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<AssetModelNode>> FindNodesAsync(string adapterId, FindAssetModelNodesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/find";

            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<AssetModelNode>>(cancellationToken).ConfigureAwait(false);
        }

    }
}
