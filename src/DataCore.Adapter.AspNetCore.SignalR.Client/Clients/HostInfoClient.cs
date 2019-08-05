using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {
    public class HostInfoClient {

        private readonly AdapterSignalRClient _client;


        public HostInfoClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<HostInfo> GetHostInfoAsync(CancellationToken cancellationToken = default) {
            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<HostInfo>(
                "GetHostInfo",
                cancellationToken
            ).ConfigureAwait(false);
        }

    }
}
