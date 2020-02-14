using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.Http.Client.Clients {

    /// <summary>
    /// Client for querying adapter alarm and event messages.
    /// </summary>
    public class EventsClient {

        /// <summary>
        /// The URL prefix for API calls.
        /// </summary>
        private const string UrlPrefix = "api/data-core/v1.0/events";

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
        public EventsClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Reads historical event messages from an adapter using a time range.
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
        public async Task<IEnumerable<EventMessage>> ReadEventMessagesAsync(
            string adapterId, 
            ReadEventMessagesForTimeRangeRequest request, 
            RequestMetadata metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/by-time-range";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<ReadEventMessagesForTimeRangeRequest>(request, new JsonMediaTypeFormatter())
            }.AddRequestMetadata(metadata);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<EventMessage>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }


        /// <summary>
        /// Reads historical event messages from an adapter using an initial cursor position.
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
        public async Task<IEnumerable<EventMessageWithCursorPosition>> ReadEventMessagesAsync(
            string adapterId, 
            ReadEventMessagesUsingCursorRequest request, 
            RequestMetadata metadata = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/by-cursor";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<ReadEventMessagesUsingCursorRequest>(request, new JsonMediaTypeFormatter())
            }.AddRequestMetadata(metadata);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<EventMessageWithCursorPosition>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }


        /// <summary>
        /// Writes event messages to an adapter.
        /// </summary>
        /// 
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The events to write.
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
        public async Task<IEnumerable<WriteEventMessageResult>> WriteEventMessagesAsync(
            string adapterId, 
            WriteEventMessagesRequest request, 
            RequestMetadata metadata,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/write";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = new ObjectContent<WriteEventMessagesRequest>(request, new JsonMediaTypeFormatter())
            }.AddRequestMetadata(metadata);

            try {
                using (var httpResponse = await _client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false)) {
                    httpResponse.EnsureSuccessStatusCode();

                    return await httpResponse.Content.ReadAsAsync<IEnumerable<WriteEventMessageResult>>(cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                httpRequest.RemoveStateProperty().Dispose();
            }
        }

    }
}
