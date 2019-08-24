using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Http.Client.Clients {

    /// <summary>
    /// Client for querying adapter tag values.
    /// </summary>
    public class TagValuesClient {

        /// <summary>
        /// The URL prefix for API calls.
        /// </summary>
        private const string UrlPrefix = "api/data-core/v1.0/tag-values";

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
        /// <param name="principal">
        ///   The principal to associate with the outgoing request.
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
        public async Task<IEnumerable<TagValueQueryResult>> ReadSnapshotTagValuesAsync(string adapterId, ReadSnapshotTagValuesRequest request, ClaimsPrincipal principal = null, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/snapshot";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<ReadSnapshotTagValuesRequest>(request, new JsonMediaTypeFormatter())
            }.AddStateProperty(principal);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<TagValueQueryResult>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
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
        /// <param name="principal">
        ///   The principal to associate with the outgoing request.
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
        public async Task<IEnumerable<TagValueQueryResult>> ReadRawTagValuesAsync(string adapterId, ReadRawTagValuesRequest request, ClaimsPrincipal principal = null, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/raw";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<ReadRawTagValuesRequest>(request, new JsonMediaTypeFormatter())
            }.AddStateProperty(principal);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<TagValueQueryResult>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
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
        /// <param name="principal">
        ///   The principal to associate with the outgoing request.
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
        public async Task<IEnumerable<TagValueQueryResult>> ReadPlotTagValuesAsync(string adapterId, ReadPlotTagValuesRequest request, ClaimsPrincipal principal = null, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/plot";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<ReadPlotTagValuesRequest>(request, new JsonMediaTypeFormatter())
            }.AddStateProperty(principal);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<TagValueQueryResult>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }


        /// <summary>
        /// Polls an adapter for interpolated tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="principal">
        ///   The principal to associate with the outgoing request.
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
        public async Task<IEnumerable<TagValueQueryResult>> ReadInterpolatedTagValuesAsync(string adapterId, ReadInterpolatedTagValuesRequest request, ClaimsPrincipal principal = null, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/interpolated";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<ReadInterpolatedTagValuesRequest>(request, new JsonMediaTypeFormatter())
            }.AddStateProperty(principal);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<TagValueQueryResult>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
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
        /// <param name="principal">
        ///   The principal to associate with the outgoing request.
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
        public async Task<IEnumerable<TagValueQueryResult>> ReadTagValuesAtTimesAsync(string adapterId, ReadTagValuesAtTimesRequest request, ClaimsPrincipal principal = null, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/values-at-times";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<ReadTagValuesAtTimesRequest>(request, new JsonMediaTypeFormatter())
            }.AddStateProperty(principal);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<TagValueQueryResult>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }


        /// <summary>
        /// Gets the data functions that an adapter supports when calling 
        /// <see cref="ReadProcessedTagValuesAsync"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="principal">
        ///   The principal to associate with the outgoing request.
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
        public async Task<IEnumerable<DataFunctionDescriptor>> GetSupportedDataFunctionsAsync(string adapterId, ClaimsPrincipal principal = null, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/supported-aggregations";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url).AddStateProperty(principal);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<DataFunctionDescriptor>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
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
        /// <param name="principal">
        ///   The principal to associate with the outgoing request.
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
        public async Task<IEnumerable<ProcessedTagValueQueryResult>> ReadProcessedTagValuesAsync(string adapterId, ReadProcessedTagValuesRequest request, ClaimsPrincipal principal = null, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/processed";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<ReadProcessedTagValuesRequest>(request, new JsonMediaTypeFormatter())
            }.AddStateProperty(principal);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<ProcessedTagValueQueryResult>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }


        /// <summary>
        /// Writes a stream of tag values to an adapter's snapshot.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="values">
        ///   The items to write.
        /// </param>
        /// <param name="principal">
        ///   The principal to associate with the outgoing request.
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
        ///   <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        public async Task<IEnumerable<WriteTagValueResult>> WriteSnapshotValuesAsync(string adapterId, IEnumerable<WriteTagValueItem> values, ClaimsPrincipal principal = null, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }

            values = values.Where(x => x != null).ToArray();
            if (!values.Any()) {
                return new WriteTagValueResult[0];
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/write/snapshot";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<IEnumerable<WriteTagValueItem>>(values, new JsonMediaTypeFormatter())
            }.AddStateProperty(principal);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<WriteTagValueResult>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }


        /// <summary>
        /// Writes a stream of tag values to an adapter's history archive.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="values">
        ///   The items to write.
        /// </param>
        /// <param name="principal">
        ///   The principal to associate with the outgoing request.
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
        ///   <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        public async Task<IEnumerable<WriteTagValueResult>> WriteHistoricalValuesAsync(string adapterId, IEnumerable<WriteTagValueItem> values, ClaimsPrincipal principal = null, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }

            values = values.Where(x => x != null).ToArray();
            if (!values.Any()) {
                return new WriteTagValueResult[0];
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/write/snapshot";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<IEnumerable<WriteTagValueItem>>(values, new JsonMediaTypeFormatter())
            }.AddStateProperty(principal);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<WriteTagValueResult>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }

    }
}
