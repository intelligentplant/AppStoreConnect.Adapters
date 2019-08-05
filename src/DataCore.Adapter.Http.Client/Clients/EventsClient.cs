using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events.Models;

namespace DataCore.Adapter.Http.Client.Clients {
    public class EventsClient {

        private const string UrlPrefix = "api/data-core/v1.0/events";

        private readonly AdapterHttpClient _client;


        public EventsClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<IEnumerable<EventMessage>> ReadEventMessagesAsync(string adapterId, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/by-time-range";

            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<EventMessage>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<EventMessageWithCursorPosition>> ReadEventMessagesAsync(string adapterId, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/by-cursor";

            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<EventMessageWithCursorPosition>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<WriteEventMessageResult>> WriteEventMessagesAsync(string adapterId, IEnumerable<WriteEventMessageItem> events, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }

            events = events.Where(x => x != null).ToArray();
            if (!events.Any()) {
                return new WriteEventMessageResult[0];
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/write";

            var response = await _client.HttpClient.PostAsJsonAsync(url, events, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<WriteEventMessageResult>>(cancellationToken).ConfigureAwait(false);
        }

    }
}
