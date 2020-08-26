using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for querying event messages, including pushing event messages to 
    // subscribers. Event message push is only supported on adapters that implement the 
    // IEventMessagePush feature.

    public partial class AdapterHub {

        #region [ Subscription Management ]

        /// <summary>
        /// Holds topic-based event subscriptions for all connections.
        /// </summary>
        private static readonly ConnectionSubscriptionManager<EventMessage, TopicSubscriptionWrapper<EventMessage>> s_eventTopicSubscriptions = new ConnectionSubscriptionManager<EventMessage, TopicSubscriptionWrapper<EventMessage>>();


        /// <summary>
        /// Invoked when a client disconnects.
        /// </summary>
        partial void OnEventsHubDisconnection() {
            s_eventTopicSubscriptions.RemoveAllSubscriptions(Context.ConnectionId);
        }


        /// <summary>
        /// Creates a topic-based event subscription for an adapter using the adapter's 
        /// <see cref="IEventMessagePushWithTopics"/> feature. Note that this does not add any 
        /// event topics to the subscription; this must be done separately via calls to 
        /// <see cref="CreateEventMessageTopicChannel"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to subscribe to.
        /// </param>
        /// <param name="request">
        ///   The subscription request parameters.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return the ID for the subscription.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Subscription lifecycle is managed externally to this method")]
        public async Task<string> CreateEventMessageTopicSubscription(string adapterId, CreateEventMessageSubscriptionRequest request) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IEventMessagePushWithTopics>(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);

            var wrappedSubscription = new TopicSubscriptionWrapper<EventMessage>(
                await adapter.Feature.Subscribe(adapterCallContext, request).ConfigureAwait(false),
                TaskScheduler
            );
            return s_eventTopicSubscriptions.AddSubscription(Context.ConnectionId, wrappedSubscription);
        }


        /// <summary>
        /// Subscribes to receive event messages for the specified event topic.
        /// </summary>
        /// <param name="subscriptionId">
        ///   The subscription ID to add the event topic to. Subscriptions are created via calls to 
        ///   <see cref="CreateEventMessageTopicSubscription"/>.
        /// </param>
        /// <param name="topicName">
        ///   The topic name to subscribe to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a channel that emits event messages 
        ///   for the topic.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exceptions are written to response channel")]
        public Task<ChannelReader<EventMessage>> CreateEventMessageTopicChannel(
            string subscriptionId,
            string topicName,
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(subscriptionId)) {
                throw new ArgumentException(Resources.Error_SubscriptionIdRequired, nameof(subscriptionId));
            }
            if (string.IsNullOrWhiteSpace(topicName)) {
                throw new ArgumentException(Resources.Error_EventTopicNameRequired, nameof(topicName));
            }

            if (!s_eventTopicSubscriptions.TryGetSubscription(Context.ConnectionId, subscriptionId, out var subscription)) {
                throw new ArgumentException(Resources.Error_SubscriptionDoesNotExist, nameof(subscriptionId));
            }

            var result = Channel.CreateUnbounded<EventMessage>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var topicChannel = await subscription.CreateTopicChannel(topicName).ConfigureAwait(false);
                try {
                    while (!ct.IsCancellationRequested) {
                        try {
                            var val = await topicChannel.Reader.ReadAsync(ct).ConfigureAwait(false);
                            await ch.WriteAsync(val, ct).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { }
                        catch (ChannelClosedException) { }
                    }
                }
                catch (Exception e) {
                    topicChannel.Writer.TryComplete(e);
                }
                finally {
                    await topicChannel.DisposeAsync().ConfigureAwait(false);
                }
            }, true, TaskScheduler, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <summary>
        /// Creates a channel that will receive event messages from the specified adapter using 
        /// the <see cref="IEventMessagePush"/> feature.
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
        ///   A channel reader that the subscriber can observe to receive new event messages.
        /// </returns>
        public async Task<ChannelReader<EventMessage>> CreateEventMessageChannel(string adapterId, CreateEventMessageSubscriptionRequest request, CancellationToken cancellationToken) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IEventMessagePush>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);

            // Create the subscription.
            return await adapter.Feature.Subscribe(adapterCallContext, request ?? new CreateEventMessageSubscriptionRequest(), cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region [ Polling Queries ]

        /// <summary>
        /// Reads event messages occurring inside the specified time range.
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
        ///   The matching event messages.
        /// </returns>
        public async Task<ChannelReader<EventMessage>> ReadEventMessagesForTimeRange(string adapterId, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadEventMessagesForTimeRange>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return await adapter.Feature.ReadEventMessagesForTimeRange(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads event messages starting at the specified cursor position.
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
        ///   The matching event messages.
        /// </returns>
        public async Task<ChannelReader<EventMessageWithCursorPosition>> ReadEventMessagesUsingCursor(string adapterId, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadEventMessagesUsingCursor>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return await adapter.Feature.ReadEventMessagesUsingCursor(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region [ Write Event Messages ]

        /// <summary>
        /// Writes event messages to the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="channel">
        ///   A channel that will provide the event messages to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the write results.
        /// </returns>
        public async Task<ChannelReader<WriteEventMessageResult>> WriteEventMessages(string adapterId, ChannelReader<WriteEventMessageItem> channel, CancellationToken cancellationToken) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IWriteEventMessages>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            return await adapter.Feature.WriteEventMessages(adapterCallContext, channel, cancellationToken).ConfigureAwait(false);
        }

        #endregion

    }
}
