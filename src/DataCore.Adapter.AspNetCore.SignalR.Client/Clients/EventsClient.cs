using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Events.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {
    public class EventsClient {

        private readonly AdapterSignalRClient _client;


        public EventsClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<ChannelReader<EventMessage>> CreateEventMessageChannelAsync(string adapterId, bool active, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<EventMessage>(
                "CreateEventMessageChannel",
                adapterId,
                active,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<EventMessage>> ReadEventMessagesAsync(string adapterId, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<EventMessage>(
                "ReadEventMessagesForTimeRange",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<EventMessageWithCursorPosition>> ReadEventMessagesAsync(string adapterId, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<EventMessageWithCursorPosition>(
                "ReadEventMessagesUsingCursor",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<WriteEventMessageResult>> WriteEventMessagesAsync(string adapterId, ChannelReader<WriteEventMessageItem> channel, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<WriteEventMessageResult>(
                "WriteEventMessages",
                adapterId,
                channel,
                cancellationToken
            ).ConfigureAwait(false);
        }

    }
}
