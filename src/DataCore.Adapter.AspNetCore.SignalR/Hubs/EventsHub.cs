using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Events;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for querying event messages, including pushing event messages to 
    // subscribers. Event message push is only supported on adapters that implement the 
    // IEventMessagePush feature.

    public partial class AdapterHub {

        #region [ Subscription Management ]

#if NET48 == false

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
        public async IAsyncEnumerable<EventMessage> CreateEventMessageTopicChannel(
            string adapterId, 
            CreateEventMessageTopicSubscriptionRequest request, 
            IAsyncEnumerable<EventMessageSubscriptionUpdate> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IEventMessagePushWithTopics>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartEventMessagePushWithTopicsSubscribeActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.Subscribe(adapterCallContext, request, channel, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
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
        public async IAsyncEnumerable<EventMessage> CreateEventMessageTopicChannel(
            string adapterId,
            CreateEventMessageTopicSubscriptionRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IEventMessagePushWithTopics>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartEventMessagePushWithTopicsSubscribeActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.Subscribe(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
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
        public async IAsyncEnumerable<EventMessage> CreateEventMessageChannel(
            string adapterId,
            CreateEventMessageSubscriptionRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IEventMessagePush>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartEventMessagePushSubscribeActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.Subscribe(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
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
        public async IAsyncEnumerable<EventMessage> ReadEventMessagesForTimeRange(
            string adapterId, 
            ReadEventMessagesForTimeRangeRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadEventMessagesForTimeRange>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartReadEventMessagesForTimeRangeActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.ReadEventMessagesForTimeRange(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
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
        public async IAsyncEnumerable<EventMessageWithCursorPosition> ReadEventMessagesUsingCursor(
            string adapterId, 
            ReadEventMessagesUsingCursorRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadEventMessagesUsingCursor>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartReadEventMessagesUsingCursorActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.ReadEventMessagesUsingCursor(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
        }

        #endregion

        #region [ Write Event Messages ]

#if NET48 == false

        /// <summary>
        /// Writes event messages to the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The write request.
        /// </param>
        /// <param name="channel">
        ///   An <see cref="IAsyncEnumerable{T}"/> that will provide the event messages to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the write results.
        /// </returns>
        public async IAsyncEnumerable<WriteEventMessageResult> WriteEventMessages(
            string adapterId,
            WriteEventMessagesRequest request,
            IAsyncEnumerable<WriteEventMessageItem> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IWriteEventMessages>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartWriteEventMessagesActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.WriteEventMessages(adapterCallContext, request, channel, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
        }

#else

        /// <summary>
        /// Writes an event message to the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The write request.
        /// </param>
        /// <param name="item">
        ///   The event message to write.
        /// </param>
        /// <returns>
        ///   The write result.
        /// </returns>
        public async Task<WriteEventMessageResult> WriteEventMessage(
            string adapterId, 
            WriteEventMessagesRequest request, 
            WriteEventMessageItem item
        ) {
            ValidateObject(item);
            ValidateObject(request);
            var adapterCallContext = new SignalRAdapterCallContext(Context);

            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted)) {
                var cancellationToken = ctSource.Token;
                try {
                    var adapter = await ResolveAdapterAndFeature<IWriteEventMessages>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
                    var inChannel = Channel.CreateUnbounded<WriteEventMessageItem>();
                    inChannel.Writer.TryWrite(item);
                    inChannel.Writer.TryComplete();

                    using (var activity = Telemetry.ActivitySource.StartWriteEventMessagesActivity(adapter.Adapter.Descriptor.Id, request)) {
                        var result = await adapter.Feature.WriteEventMessages(adapterCallContext, request, inChannel.Reader.ReadAllAsync(cancellationToken), cancellationToken).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                        activity.SetResponseItemCountTag(result == null ? 0 : 1);
                        return result!;
                    }
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
