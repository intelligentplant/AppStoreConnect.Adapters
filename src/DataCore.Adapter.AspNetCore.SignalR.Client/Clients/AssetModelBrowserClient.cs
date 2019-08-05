using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AssetModel.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {
    public class AssetModelBrowserClient {

        private readonly AdapterSignalRClient _client;


        public AssetModelBrowserClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<ChannelReader<AssetModelNode>> BrowseAssetModelNodesAsync(string adapterId, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<AssetModelNode>(
                "BrowseAssetModelNodes",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<AssetModelNode>> GetAssetModelNodesAsync(string adapterId, GetAssetModelNodesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<AssetModelNode>(
                "GetAssetModelNodes",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<AssetModelNode>> FindAssetModelNodesAsync(string adapterId, FindAssetModelNodesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<AssetModelNode>(
                "FindAssetModelNodes",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }

    }
}
