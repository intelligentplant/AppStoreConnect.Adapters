using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
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
        /// <param name="request">
        ///   The request.
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
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async Task<InvocationResponse> InvokeExtensionAsync(
            string adapterId,
            InvocationRequest request,
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<InvocationResponse>(
                "InvokeExtension",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Invokes a streaming extension feature on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
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
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async IAsyncEnumerable<InvocationResponse> InvokeStreamingExtensionAsync(
            string adapterId,
            InvocationRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            await foreach (var item in connection.StreamAsync<InvocationResponse>(
                "InvokeStreamingExtension",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Invokes a duplex streaming extension feature on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
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
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public async IAsyncEnumerable<InvocationResponse> InvokeDuplexStreamingExtensionAsync(
            string adapterId,
            DuplexStreamInvocationRequest request,
            IAsyncEnumerable<InvocationStreamItem> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            if (_client.CompatibilityLevel != CompatibilityLevel.AspNetCore2) {
                // We are using ASP.NET Core 3.0+ so we can use bidirectional streaming.
                await foreach (var item in connection.StreamAsync<InvocationResponse>(
                    "InvokeDuplexStreamingExtension",
                    adapterId,
                    request,
                    channel,
                    cancellationToken
                ).ConfigureAwait(false)) {
                    yield return item;
                }
                yield break;
            }

            // We are using ASP.NET Core 2.x, so we cannot use bidirectional streaming. Instead, 
            // we will read the intput stream ourselves and make an invocation call for every value.

            var response = await connection.InvokeAsync<InvocationResponse>(
                "InvokeDuplexStreamingExtension",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);

            yield return response;

            await foreach (var item in channel.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                response = await connection.InvokeAsync<InvocationResponse>(
                    "InvokeDuplexStreamingExtension",
                    adapterId,
                    request,
                    new InvocationStreamItem() {
                        Arguments = item.Arguments
                    },
                    cancellationToken
                ).ConfigureAwait(false);

                yield return response;
            }
        }

    }
}
