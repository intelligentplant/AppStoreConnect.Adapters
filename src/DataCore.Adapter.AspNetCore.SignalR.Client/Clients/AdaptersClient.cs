using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {
    public class AdaptersClient {

        private readonly AdapterSignalRClient _client;


        public AdaptersClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<IEnumerable<AdapterDescriptor>> GetAdaptersAsync(CancellationToken cancellationToken = default) {
            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<IEnumerable<AdapterDescriptor>>(
                "GetAdapters", 
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<AdapterDescriptorExtended> GetAdapterAsync(string adapterId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<AdapterDescriptorExtended>(
                "GetAdapter", 
                adapterId, 
                cancellationToken
            ).ConfigureAwait(false);
        }

    }
}
