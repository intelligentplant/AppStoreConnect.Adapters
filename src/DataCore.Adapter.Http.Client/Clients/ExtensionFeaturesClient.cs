﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Http.Client.Clients {

    /// <summary>
    /// Client for querying adapter extension features.
    /// </summary>
    public class ExtensionFeaturesClient {

        /// <summary>
        /// The URL prefix for API calls.
        /// </summary>
        private const string UrlPrefix = "api/data-core/v1.0/extensions";

        /// <summary>
        /// The adapter HTTP client that is used to perform the requests.
        /// </summary>
        private readonly AdapterHttpClient _client;


        /// <summary>
        /// Creates a new <see cref="EventsClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter HTTP client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public ExtensionFeaturesClient(AdapterHttpClient client) {
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
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
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
            RequestMetadata metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (featureUri == null) {
                throw new ArgumentNullException(nameof(featureUri));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/descriptor?id={Uri.EscapeDataString(featureUri.ToString())}";

            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Get, url, metadata))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return await httpResponse.Content.ReadAsAsync<FeatureDescriptor>(cancellationToken).ConfigureAwait(false);
            }
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
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
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
            RequestMetadata metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (featureUri == null) {
                throw new ArgumentNullException(nameof(featureUri));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/operations?id={Uri.EscapeDataString(featureUri.ToString())}";

            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Get, url, metadata))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return await httpResponse.Content.ReadAsAsync<IEnumerable<ExtensionFeatureOperationDescriptor>>(cancellationToken).ConfigureAwait(false);
            }
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
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
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
            RequestMetadata metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (operationUri == null) {
                throw new ArgumentNullException(nameof(operationUri));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/operations/invoke?id={Uri.EscapeDataString(operationUri.ToString())}";

            using (var formData = new FormUrlEncodedContent(new Dictionary<string, string>() { [nameof(argument)] = argument }))
            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Post, url, formData, metadata))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

    }
}