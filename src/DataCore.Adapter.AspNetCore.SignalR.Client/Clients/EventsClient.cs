using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Events;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {

    /// <summary>
    /// Client for querying adapter alarm and event messages.
    /// </summary>
    public class EventsClient {

        /// <summary>
        /// The adapter SignalR client that manages the connection.
        /// </summary>
        private readonly AdapterSignalRClient _client;


        /// <summary>
        /// Creates a new <see cref="EventsClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter SignalR client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public EventsClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Creates a channel to receive event messages in real-time from the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   Additional subscription properties.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation. If this token fires, or the connection is
        ///   lost, the channel will be closed.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a channel that is used to stream the 
        ///   event messages back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        public async Task<ChannelReader<EventMessage>> CreateEventMessageChannelAsync(string adapterId, CreateEventMessageSubscriptionRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<EventMessage>(
                "CreateEventMessageChannel",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Creates a topic-based event subscription that can be used to subscribe to specific 
        /// event topics on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   Additional subscription properties.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return the subscription ID.
        /// </returns>
        /// <seealso cref="DeleteEventMessageTopicSubscriptionAsync"/>
        /// <seealso cref="CreateEventMessageTopicChannelAsync"/>
        public async Task<string> CreateEventMessageTopicSubscriptionAsync(string adapterId, CreateEventMessageSubscriptionRequest request, CancellationToken cancellationToken) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<string>(
                "CreateEventMessageTopicSubscription",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a topic-based event subscription that was created via a call to 
        /// <see cref="CreateEventMessageTopicSubscriptionAsync"/>.
        /// </summary>
        /// <param name="subscriptionId">
        ///   The subscription ID. Specify <see langword="null"/> or white space to delete all active 
        ///   event topic subscriptions for the connection.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that returns a flag indicating if the operation was 
        ///   successful.
        /// </returns>
        /// <seealso cref="CreateEventMessageTopicSubscriptionAsync"/>
        /// <seealso cref="CreateEventMessageTopicChannelAsync"/>
        public async Task<bool> DeleteEventMessageTopicSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken) {
            if (string.IsNullOrWhiteSpace(subscriptionId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(subscriptionId));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<bool>(
                "DeleteEventMessageTopicSubscription",
                subscriptionId,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Creates a channel to receive event messages in real-time from a topic-based event 
        /// subscription created via a call to <see cref="CreateEventMessageTopicSubscriptionAsync"/>.
        /// </summary>
        /// <param name="subscriptionId">
        ///   The subscription ID.
        /// </param>
        /// <param name="topic">
        ///   The event topic to subscribe to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation. If this token fires, or the connection is
        ///   lost, the channel will be closed.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a channel that is used to stream the 
        ///   event messages back to the caller.
        /// </returns>
        /// <seealso cref="CreateEventMessageTopicSubscriptionAsync"/>
        /// <seealso cref="CreatDeleteEventMessageTopicSubscriptionAsync"/>
        public async Task<ChannelReader<EventMessage>> CreateEventMessageTopicChannelAsync(string subscriptionId, string topic, CancellationToken cancellationToken) {
            if (string.IsNullOrWhiteSpace(subscriptionId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(subscriptionId));
            }
            if (string.IsNullOrWhiteSpace(topic)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(topic));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<EventMessage>(
                "CreateEventMessageTopicChannel",
                subscriptionId,
                topic,
                cancellationToken
            ).ConfigureAwait(false);
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the event messages back to 
        ///   the caller.
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


        /// <summary>
        /// Reads historical event messages from an adapter using an initial cursor position.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the event messages back to 
        ///   the caller.
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


        /// <summary>
        /// Writes event messages to an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="channel">
        ///   The channel that will emit the items to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the write result for each 
        ///   event message back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
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
