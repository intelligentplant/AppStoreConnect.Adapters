using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;

using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {

    /// <summary>
    /// Client for performing adapter configuration chaneg monitoring.
    /// </summary>
    public class ConfigurationChangesClient {

        /// <summary>
        /// The adapter SignalR client that manages the connection.
        /// </summary>
        private readonly AdapterSignalRClient _client;


        /// <summary>
        /// Creates a new <see cref="TagSearchClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter SignalR client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public ConfigurationChangesClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Creates a subscription channel that will receive configuration changes from the 
        /// specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The subscription request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation. If this token fires, or the connection is
        ///   lost, the channel will be closed.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the configuration change 
        ///   notifications back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        public async IAsyncEnumerable<ConfigurationChange> CreateConfigurationChangesChannelAsync(
            string adapterId, 
            ConfigurationChangesSubscriptionRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnectionAsync(cancellationToken).ConfigureAwait(false);
            await foreach (var item in connection.StreamAsync<ConfigurationChange>(
                "CreateConfigurationChangesChannel",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }

    }
}
