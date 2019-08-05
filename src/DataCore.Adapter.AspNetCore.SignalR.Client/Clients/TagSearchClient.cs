using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {
    public class TagSearchClient {

        private readonly AdapterSignalRClient _client;


        public TagSearchClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<ChannelReader<TagDefinition>> FindTagsAsync(string adapterId, FindTagsRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagDefinition>(
                "FindTags",
                adapterId,
                request, 
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<TagDefinition>> GetTagsAsync(string adapterId, GetTagsRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagDefinition>(
                "GetTags",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }

    }
}
