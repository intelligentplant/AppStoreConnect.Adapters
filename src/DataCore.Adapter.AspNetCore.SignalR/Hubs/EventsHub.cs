using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for querying event messages, including pushing event messages to 
    // subscribers. Event message push is only supported on adapters that implement the 
    // IEventMessagePush feature.

    public partial class AdapterHub {

        #region [ Subscription Management ]

#if NETSTANDARD2_0 == false

        /// <summary>
        /// Creates a channel that will receive event messages from the specified adapter using 
        /// the <see cref="IEventMessagePushWithTopics"/> feature.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The subscription request.
        /// </param>
        /// <param name="channel">
        ///   A channel that can be used to publish changes to the channel's subscription topics.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that the subscriber can observe to receive new event messages.
        /// </returns>
        public async Task<ChannelReader<EventMessage>> CreateEventMessageTopicChannel(
            string adapterId, 
            CreateEventMessageTopicSubscriptionRequest request, 
            ChannelReader<EventMessageSubscriptionUpdate> channel,
            CancellationToken cancellationToken
        ) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IEventMessagePushWithTopics>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);

            // Create the subscription.
            return await adapter.Feature.Subscribe(adapterCallContext, request, channel, cancellationToken).ConfigureAwait(false);
        }

#else

        /// <summary>
        /// Creates a channel that will receive event messages from the specified adapter using 
        /// the <see cref="IEventMessagePushWithTopics"/> feature.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The subscription request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that the subscriber can observe to receive new event messages.
        /// </returns>
        public async Task<ChannelReader<EventMessage>> CreateEventMessageTopicChannel(
            string adapterId,
            CreateEventMessageTopicSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IEventMessagePushWithTopics>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);

            // Create the subscription.
            return await adapter.Feature.Subscribe(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }

#endif


        /// <summary>
        /// Creates a channel that will receive event messages from the specified adapter using 
        /// the <see cref="IEventMessagePush"/> feature.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The subscription request.
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
            return await adapter.Feature.Subscribe(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
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

#if NETSTANDARD2_0 == false

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

#else

        /// <summary>
        /// Writes an event message to the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="item">
        ///   The event message to write.
        /// </param>
        /// <returns>
        ///   The write result.
        /// </returns>
        public async Task<WriteEventMessageResult> WriteEventMessage(string adapterId, WriteEventMessageItem item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted)) {
                var cancellationToken = ctSource.Token;
                try {
                    var adapter = await ResolveAdapterAndFeature<IWriteEventMessages>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
                    var inChannel = Channel.CreateUnbounded<WriteEventMessageItem>();
                    inChannel.Writer.TryWrite(item);
                    inChannel.Writer.TryComplete();

                    var outChannel = await adapter.Feature.WriteEventMessages(adapterCallContext, inChannel, cancellationToken).ConfigureAwait(false);
                    return await outChannel.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
                finally {
                    ctSource.Cancel();
                }
            }
        }

#endif

        #endregion

    }
}
