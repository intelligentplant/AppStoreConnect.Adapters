using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Events;
using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for querying event messages, including pushing event messages to 
    // subscribers. Event message push is only supported on adapters that implement the 
    // IEventMessagePush feature.

    public partial class AdapterHub {

        #region [ Subscription Management ]

        /// <summary>
        /// Creates a channel that will receive event messages from the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="subscriptionType">
        ///   Specifies if an active or passive subscription should be created.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that the subscriber can observe to receive new tag values.
        /// </returns>
        public async Task<ChannelReader<EventMessage>> CreateEventMessageChannel(string adapterId, EventMessageSubscriptionType subscriptionType, CancellationToken cancellationToken) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IEventMessagePush>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);

            // Create the subscription.
            var subscription = await adapter.Feature.Subscribe(adapterCallContext, subscriptionType).ConfigureAwait(false);

            var result = Channel.CreateUnbounded<EventMessage>();

            // Run background operation to dispose of the subscription when the cancellation token 
            // fires.
            TaskScheduler.QueueBackgroundWorkItem(async ct => {
                try {
                    while (await subscription.Reader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!subscription.Reader.TryRead(out var item) || item == null) {
                            continue;
                        }

                        await result.Writer.WriteAsync(item, ct).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
                finally {
                    subscription.Dispose();
                }
            }, null, cancellationToken);

            return result;
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
            return await adapter.Feature.ReadEventMessages(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
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
            return await adapter.Feature.ReadEventMessages(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
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
