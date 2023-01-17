using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.AssetModel;

namespace DataCore.Adapter.Http.Client.Clients {

    /// <summary>
    /// Client for querying adapter asset models.
    /// </summary>
    public class AssetModelBrowserClient {

        /// <summary>
        /// The URL prefix for API calls.
        /// </summary>
        private string UrlPrefix => string.Concat(_client.GetBaseUrl(), "/asset-model");

        /// <summary>
        /// The adapter HTTP client that is used to perform the requests.
        /// </summary>
        private readonly AdapterHttpClient _client;


        /// <summary>
        /// Creates a new <see cref="AssetModelBrowserClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter HTTP client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public AssetModelBrowserClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Browses an adapter's asset model.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the results back to the caller.
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
        public async IAsyncEnumerable<AssetModelNode> BrowseNodesAsync(
            string adapterId, 
            BrowseAssetModelNodesRequest request, 
            RequestMetadata? metadata = null,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterHttpClient.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/browse";

            using (var httpRequest = _client.CreateHttpRequestMessage(HttpMethod.Post, url, request, metadata))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                await httpResponse.ThrowOnErrorResponse().ConfigureAwait(false);

                await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<AssetModelNode>(await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false), _client.JsonSerializerOptions, cancellationToken)) {
                    if (item == null) {
                        continue;
                    }
                    yield return item;
                }
            }
        }


        /// <summary>
        /// Gets specific nodes in an adapter's asset model.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the results back to the caller.
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
        public async IAsyncEnumerable<AssetModelNode> GetNodesAsync(
            string adapterId, GetAssetModelNodesRequest request, 
            RequestMetadata? metadata = null,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterHttpClient.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/get-by-id";

            using (var httpRequest = _client.CreateHttpRequestMessage(HttpMethod.Post, url, request, metadata))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                await httpResponse.ThrowOnErrorResponse().ConfigureAwait(false);

                await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<AssetModelNode>(await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false), _client.JsonSerializerOptions, cancellationToken)) {
                    if (item == null) {
                        continue;
                    }
                    yield return item;
                }
            }
        }


        /// <summary>
        /// Performs a search on an adapter's asset model.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the results back to the caller.
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
        public async IAsyncEnumerable<AssetModelNode> FindNodesAsync(
            string adapterId, 
            FindAssetModelNodesRequest request, 
            RequestMetadata? metadata = null,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterHttpClient.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/find";

            using (var httpRequest = _client.CreateHttpRequestMessage(HttpMethod.Post, url, request, metadata))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false)) {
                await httpResponse.ThrowOnErrorResponse().ConfigureAwait(false);

                await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<AssetModelNode>(await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false), _client.JsonSerializerOptions, cancellationToken)) {
                    if (item == null) {
                        continue;
                    }
                    yield return item;
                }
            }
        }

    }
}
