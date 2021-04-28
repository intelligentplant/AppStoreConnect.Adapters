using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Http.Client.Clients {

    /// <summary>
    /// Client for querying adapter tag values.
    /// </summary>
    public class TagValuesClient {

        /// <summary>
        /// The URL prefix for API calls.
        /// </summary>
        private string UrlPrefix => string.Concat(_client.GetBaseUrl(), "/tag-values");

        /// <summary>
        /// The adapter HTTP client that is used to perform the requests.
        /// </summary>
        private readonly AdapterHttpClient _client;


        /// <summary>
        /// Creates a new <see cref="TagValuesClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter HTTP client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public TagValuesClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Polls an adapter for snapshot tag values.
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
        public async Task<IEnumerable<TagValueQueryResult>> ReadSnapshotTagValuesAsync(
            string adapterId, 
            ReadSnapshotTagValuesRequest request, 
            RequestMetadata? metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterHttpClient.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/snapshot";

            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Post, url, request, metadata, _client.JsonSerializerOptions))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return (await httpResponse.Content.ReadFromJsonAsync<IEnumerable<TagValueQueryResult>>(_client.JsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            }
        }


        /// <summary>
        /// Polls an adapter for raw tag values.
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
        public async Task<IEnumerable<TagValueQueryResult>> ReadRawTagValuesAsync(
            string adapterId, 
            ReadRawTagValuesRequest request, 
            RequestMetadata? metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterHttpClient.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/raw";

            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Post, url, request, metadata, _client.JsonSerializerOptions))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return (await httpResponse.Content.ReadFromJsonAsync<IEnumerable<TagValueQueryResult>>(_client.JsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            }
        }


        /// <summary>
        /// Polls an adapter for visualisation-friendly tag values.
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
        public async Task<IEnumerable<TagValueQueryResult>> ReadPlotTagValuesAsync(
            string adapterId, 
            ReadPlotTagValuesRequest request, 
            RequestMetadata? metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterHttpClient.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/plot";

            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Post, url, request, metadata, _client.JsonSerializerOptions))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return (await httpResponse.Content.ReadFromJsonAsync<IEnumerable<TagValueQueryResult>>(_client.JsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            }
        }


        /// <summary>
        /// Polls an adapter for tag values at specific timestamps.
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
        public async Task<IEnumerable<TagValueQueryResult>> ReadTagValuesAtTimesAsync(
            string adapterId, 
            ReadTagValuesAtTimesRequest request, 
            RequestMetadata? metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterHttpClient.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/values-at-times";

            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Post, url, request, metadata, _client.JsonSerializerOptions))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return (await httpResponse.Content.ReadFromJsonAsync<IEnumerable<TagValueQueryResult>>(_client.JsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            }
        }


        /// <summary>
        /// Gets the data functions that an adapter supports when calling 
        /// <see cref="ReadProcessedTagValuesAsync"/>.
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
        public async Task<IEnumerable<DataFunctionDescriptor>> GetSupportedDataFunctionsAsync(
            string adapterId, 
            GetSupportedDataFunctionsRequest request,
            RequestMetadata? metadata = null, 
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterHttpClient.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/supported-aggregations";

            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Post, url, request, metadata, _client.JsonSerializerOptions))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return (await httpResponse.Content.ReadFromJsonAsync<IEnumerable<DataFunctionDescriptor>>(_client.JsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            }
        }


        /// <summary>
        /// Polls an adapter for processed/aggregated tag values.
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
        public async Task<IEnumerable<ProcessedTagValueQueryResult>> ReadProcessedTagValuesAsync(
            string adapterId, 
            ReadProcessedTagValuesRequest request, 
            RequestMetadata? metadata = null, 
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterHttpClient.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/processed";

            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Post, url, request, metadata, _client.JsonSerializerOptions))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return (await httpResponse.Content.ReadFromJsonAsync<IEnumerable<ProcessedTagValueQueryResult>>(_client.JsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            }
        }


        /// <summary>
        /// Writes tag values to an adapter's snapshot.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The write request.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the write result for each tag value back to the caller.
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
        public async Task<IEnumerable<WriteTagValueResult>> WriteSnapshotValuesAsync(
            string adapterId, 
            WriteTagValuesRequestExtended request, 
            RequestMetadata? metadata = null, 
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterHttpClient.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/write/snapshot";

            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Post, url, request, metadata, _client.JsonSerializerOptions))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return (await httpResponse.Content.ReadFromJsonAsync<IEnumerable<WriteTagValueResult>>(_client.JsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            }
        }


        /// <summary>
        /// Writes tag values to an adapter's history archive.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The write request.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the outgoing request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the write result for each tag value back to the caller.
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
        public async Task<IEnumerable<WriteTagValueResult>> WriteHistoricalValuesAsync(
            string adapterId, 
            WriteTagValuesRequestExtended request, 
            RequestMetadata? metadata = null, 
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterHttpClient.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/write/history";

            using (var httpRequest = AdapterHttpClient.CreateHttpRequestMessage(HttpMethod.Post, url, request, metadata, _client.JsonSerializerOptions))
            using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                httpResponse.EnsureSuccessStatusCode();

                return (await httpResponse.Content.ReadFromJsonAsync<IEnumerable<WriteTagValueResult>>(_client.JsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            }
        }

    }
}
