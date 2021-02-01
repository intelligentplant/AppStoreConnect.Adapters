using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {

    /// <summary>
    /// Client for querying adapter metadata.
    /// </summary>
    public class AdaptersClient {

        /// <summary>
        /// The adapter SignalR client that manages the connection.
        /// </summary>
        private readonly AdapterSignalRClient _client;


        /// <summary>
        /// Creates a new <see cref="AdaptersClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter SignalR client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public AdaptersClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Finds matching adapters in the remote host.
        /// </summary>
        /// <param name="request">
        ///   The search filter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will return information about the available adapters.
        /// </returns>
        public async Task<ChannelReader<AdapterDescriptor>> FindAdaptersAsync(
            FindAdaptersRequest request,
            CancellationToken cancellationToken = default
        ) {
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<AdapterDescriptor>(
                "FindAdapters", 
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets extended information about an adapter in the remote host.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return information about the adapter.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
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


        /// <summary>
        /// Requests health check diagnostics for an adapter in the remote host.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return health check diagnostics for the adapter.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        public async Task<HealthCheckResult> CheckAdapterHealthAsync(string adapterId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<HealthCheckResult>(
                "CheckAdapterHealth",
                adapterId,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Creates a subscription channel to receive health check messages in real-time from the 
        /// specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation. If this token fires, or the connection is
        ///   lost, the channel will be closed.
        /// </param>
        /// <returns>
        ///   A channel that is used to stream the health check messages back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        public async Task<ChannelReader<HealthCheckResult>> CreateAdapterHealthChannelAsync(string adapterId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<HealthCheckResult>(
                "CreateAdapterHealthChannel",
                adapterId,
                cancellationToken
            ).ConfigureAwait(false);
        }

    }
}
