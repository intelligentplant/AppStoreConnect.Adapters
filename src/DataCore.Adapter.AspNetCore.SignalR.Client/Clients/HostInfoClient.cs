using System;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {

    /// <summary>
    /// Client for querying adapter host metadata.
    /// </summary>
    public class HostInfoClient {

        /// <summary>
        /// The adapter SignalR client that manages the connection.
        /// </summary>
        private readonly AdapterSignalRClient _client;


        /// <summary>
        /// Creates a new <see cref="HostInfoClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter SignalR client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public HostInfoClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Requests information about the remote adapter host.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return information about the remote host.
        /// </returns>
        public async Task<HostInfo> GetHostInfoAsync(CancellationToken cancellationToken = default) {
            var connection = await _client.GetHubConnectionAsync(cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<HostInfo>(
                "GetHostInfo",
                cancellationToken
            ).ConfigureAwait(false);
        }

    }
}
