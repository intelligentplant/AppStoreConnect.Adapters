using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;

using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {

    /// <summary>
    /// Client for querying adapter extension features.
    /// </summary>
    public class ExtensionFeaturesClient {

        /// <summary>
        /// The adapter SignalR client that manages the connection.
        /// </summary>
        private readonly AdapterSignalRClient _client;


        /// <summary>
        /// Creates a new <see cref="ExtensionFeaturesClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter SignalR client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public ExtensionFeaturesClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Gets the descriptor for the specified extension feature.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="featureUri">
        ///   The extension feature URI to retrieve the descriptor for.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The feature descriptor.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="featureUri"/> is <see langword="null"/>.
        /// </exception>
        public async Task<FeatureDescriptor> GetDescriptorAsync(
            string adapterId,
            Uri featureUri,
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (featureUri == null) {
                throw new ArgumentNullException(nameof(featureUri));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<FeatureDescriptor>(
                "GetDescriptor",
                adapterId,
                featureUri,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the available operations for the specified extension feature.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="featureUri">
        ///   The extension feature URI to retrieve the operation descriptors for.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The available operations.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="featureUri"/> is <see langword="null"/>.
        /// </exception>
        public async Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperationsAsync(
            string adapterId,
            Uri featureUri,
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (featureUri == null) {
                throw new ArgumentNullException(nameof(featureUri));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<IEnumerable<ExtensionFeatureOperationDescriptor>>(
                "GetExtensionOperations",
                adapterId,
                featureUri,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Invokes an extension feature on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="operationUri">
        ///   The URI of the operation to invoke.
        /// </param>
        /// <param name="argument">
        ///   The argument for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The operation result.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="operationUri"/> is <see langword="null"/>.
        /// </exception>
        public async Task<string> InvokeExtensionAsync(
            string adapterId,
            Uri operationUri,
            string argument,
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (operationUri == null) {
                throw new ArgumentNullException(nameof(operationUri));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<string>(
                "InvokeExtension",
                adapterId,
                operationUri,
                argument,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Invokes a streaming extension feature on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="operationUri">
        ///   The URI of the operation to invoke.
        /// </param>
        /// <param name="argument">
        ///   The argument for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will stream the operation results back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="operationUri"/> is <see langword="null"/>.
        /// </exception>
        public async Task<ChannelReader<string>> InvokeStreamingExtensionAsync(
            string adapterId,
            Uri operationUri,
            string argument,
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (operationUri == null) {
                throw new ArgumentNullException(nameof(operationUri));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<string>(
                "InvokeStreamingExtension",
                adapterId,
                operationUri,
                argument,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Invokes a duplex streaming extension feature on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="operationUri">
        ///   The URI of the operation to invoke.
        /// </param>
        /// <param name="channel">
        ///   The input channel for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will stream the operation results back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="operationUri"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public async Task<ChannelReader<string>> InvokeDuplexStreamingExtensionAsync(
            string adapterId,
            Uri operationUri,
            ChannelReader<string> channel,
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (operationUri == null) {
                throw new ArgumentNullException(nameof(operationUri));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<string>(
                "InvokeDuplexStreamingExtension",
                adapterId,
                operationUri,
                channel,
                cancellationToken
            ).ConfigureAwait(false);
        }

    }
}
