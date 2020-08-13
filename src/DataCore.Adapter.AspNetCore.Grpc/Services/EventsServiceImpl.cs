using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore;
using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Events;
using Grpc.Core;
using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="EventsService.EventsServiceBase"/>.
    /// </summary>
    public class EventsServiceImpl : EventsService.EventsServiceBase {

        /// <summary>
        /// Holds all active subscriptions.
        /// </summary>
        private static readonly ConnectionSubscriptionManager<Events.EventMessage, TopicSubscriptionWrapper<Events.EventMessage>> s_eventTopicSubscriptions = new ConnectionSubscriptionManager<Events.EventMessage, TopicSubscriptionWrapper<Events.EventMessage>>();

        /// <summary>
        /// Indicates if the background cleanup task is running.
        /// </summary>
        private static int s_cleanupTaskIsRunning;

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// The service for registering background task operations.
        /// </summary>
        private readonly IBackgroundTaskService _backgroundTaskService;


        /// <summary>
        /// Class initialiser.
        /// </summary>
        static EventsServiceImpl() {
            AspNetCore.Grpc.Services.HeartbeatServiceImpl.HeartbeatReceived += peer => {
                if (string.IsNullOrWhiteSpace(peer)) {
                    return;
                }

                s_eventTopicSubscriptions.SetHeartbeat(peer, DateTime.UtcNow);
            };
        }


        /// <summary>
        /// Creates a new <see cref="EventsServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The service for registering background task operations.
        /// </param>
        public EventsServiceImpl(IAdapterAccessor adapterAccessor, IBackgroundTaskService backgroundTaskService) {
            _adapterAccessor = adapterAccessor;
            _backgroundTaskService = backgroundTaskService;
        }


        /// <summary>
        /// Periodically removes all subscriptions for any connections that have not sent a recent
        /// heartbeat message.
        /// </summary>
        /// <param name="timeout">
        ///   The heartbeat timeout.
        /// </param>
        internal static void CleanUpStaleSubscriptions(TimeSpan timeout) {
            foreach (var connectionId in s_eventTopicSubscriptions.GetConnectionIds()) {
                if (!s_eventTopicSubscriptions.IsConnectionStale(connectionId, timeout)) {
                    continue;
                }

                s_eventTopicSubscriptions.RemoveAllSubscriptions(connectionId);
            }
        }


        /// <summary>
        /// Creates a general event message subscription to an adapter using the adapter's 
        /// <see cref="IEventMessagePush"/> feature. For topic-based event streams, see 
        /// <see cref="CreateEventTopicPushSubscription"/>.
        /// </summary>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="responseStream">
        ///   The response stream to write event messages to.
        /// </param>
        /// <param name="context">
        ///   The call context.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will publish emitted event messages to the 
        ///   <paramref name="responseStream"/>.
        /// </returns>
        /// <seealso cref="CreateEventTopicPushSubscription"/>
        /// <seealso cref="DeleteEventTopicPushSubscription"/>
        /// <seealso cref="CreateEventTopicPushChannel"/>
        public override async Task CreateEventPushChannel(CreateEventPushChannelRequest request, IServerStreamWriter<EventMessage> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IEventMessagePush>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            using (var subscription = await adapter.Feature.Subscribe(adapterCallContext, new CreateEventMessageSubscriptionRequest() { 
                SubscriptionType = request.SubscriptionType == EventSubscriptionType.Active 
                    ? EventMessageSubscriptionType.Active 
                    : EventMessageSubscriptionType.Passive,
                Properties = new Dictionary<string, string>(request.Properties)
            }).ConfigureAwait(false)) {
                while (!cancellationToken.IsCancellationRequested) {
                    try {
                        var msg = await subscription.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                        await responseStream.WriteAsync(msg.ToGrpcEventMessage()).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                        // Do nothing
                    }
                    catch (ChannelClosedException) { 
                        // Do nothing
                    }
                }
            }
        }


        /// <summary>
        /// Creates a topic-based event subscription for an adapter using the adapter's 
        /// <see cref="IEventMessagePushWithTopics"/> feature. Note that this does not add any 
        /// event topics to the subscription; this must be done separately via calls to 
        /// <see cref="CreateEventTopicPushChannel"/>.
        /// </summary>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="context">
        ///   The call context.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return the response for the operation.
        /// </returns>
        public override async Task<CreateEventTopicPushSubscriptionResponse> CreateEventTopicPushSubscription(
            CreateEventTopicPushSubscriptionRequest request, 
            ServerCallContext context
        ) {
            if (Interlocked.CompareExchange(ref s_cleanupTaskIsRunning, 1, 0) == 0) {
                // Kick off background cleanup of stale subscriptions.
                _backgroundTaskService.QueueBackgroundWorkItem(async ct => {
                    try {
                        var checkInterval = TimeSpan.FromSeconds(10);
                        var staleTimeout = TimeSpan.FromSeconds(30);

                        while (!ct.IsCancellationRequested) {
                            await Task.Delay(checkInterval, ct).ConfigureAwait(false);
                            CleanUpStaleSubscriptions(staleTimeout);
                        }
                    }
                    finally {
                        s_cleanupTaskIsRunning = 0;
                    }
                });
            }

            var adapterCallContext = new GrpcAdapterCallContext(context);
            var cancellationToken = context.CancellationToken;
            var adapterId = request.AdapterId;
            var connectionId = string.IsNullOrWhiteSpace(request.SessionId)
                ? context.Peer
                : string.Concat(context.Peer, "-", request.SessionId);

            var adapter = await Util.ResolveAdapterAndFeature<IEventMessagePushWithTopics>(
                adapterCallContext,
                _adapterAccessor,
                adapterId,
                cancellationToken
            ).ConfigureAwait(false);

#pragma warning disable CA2000 // Dispose objects before losing scope
            var wrappedSubscription = new TopicSubscriptionWrapper<Events.EventMessage>(
                await adapter.Feature.Subscribe(adapterCallContext, new CreateEventMessageSubscriptionRequest() {
                    SubscriptionType = request.SubscriptionType == EventSubscriptionType.Active
                        ? EventMessageSubscriptionType.Active
                        : EventMessageSubscriptionType.Passive,
                    Properties = new Dictionary<string, string>(request.Properties)
                }).ConfigureAwait(false),
                _backgroundTaskService
            );
#pragma warning restore CA2000 // Dispose objects before losing scope

            return new CreateEventTopicPushSubscriptionResponse() {
                SubscriptionId = s_eventTopicSubscriptions.AddSubscription(connectionId, wrappedSubscription)
            };
        }


        /// <summary>
        /// Deletes a topic-based event subscription that was created via a call to 
        /// <see cref="CreateEventTopicPushSubscription"/>.
        /// </summary>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="context">
        ///   The call context.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return the response for the operation.
        /// </returns>
        public override Task<DeleteEventTopicPushSubscriptionResponse> DeleteEventTopicPushSubscription(
            DeleteEventTopicPushSubscriptionRequest request, 
            ServerCallContext context
        ) {
            DeleteEventTopicPushSubscriptionResponse result;

            var connectionId = string.IsNullOrWhiteSpace(request.SessionId)
                ? context.Peer
                : string.Concat(context.Peer, "-", request.SessionId);

            if (string.IsNullOrWhiteSpace(request.SubscriptionId)) {
                s_eventTopicSubscriptions.RemoveAllSubscriptions(connectionId);
                result = new DeleteEventTopicPushSubscriptionResponse() {
                    Success = true
                };
            }
            else {
                result = new DeleteEventTopicPushSubscriptionResponse() {
                    Success = s_eventTopicSubscriptions.RemoveSubscription(connectionId, request.SubscriptionId)
                };
            }
            return Task.FromResult(result);
        }


        /// <summary>
        /// Adds a topic to a topic-based event subscription and streams emitted messages to the 
        /// caller. Subscriptions are created via calls to <see cref="CreateEventTopicPushSubscription"/>.
        /// </summary>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="responseStream">
        ///   The response stream to write event messages to.
        /// </param>
        /// <param name="context">
        ///   The call context.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will publish emitted event messages to the 
        ///   <paramref name="responseStream"/>.
        /// </returns>
        public override async Task CreateEventTopicPushChannel(CreateEventTopicPushChannelRequest request, IServerStreamWriter<EventMessage> responseStream, ServerCallContext context) {
            var connectionId = string.IsNullOrWhiteSpace(request.SessionId)
                ? context.Peer
                : string.Concat(context.Peer, "-", request.SessionId);

            var cancellationToken = context.CancellationToken;

            if (!s_eventTopicSubscriptions.TryGetSubscription(connectionId, request.SubscriptionId, out var subscription)) {
                return;
            }

            var channel = await subscription.CreateTopicChannel(request.Topic).ConfigureAwait(false);
            try {
                await foreach (var val in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
                    await responseStream.WriteAsync(val.ToGrpcEventMessage()).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) {
                // Caller has cancelled the subscription.
            }
            catch (ChannelClosedException) {
                // Channel has been closed due to the connection being terminated.
            }
        }


        /// <inheritdoc/>
        public override async Task GetEventMessagesForTimeRange(GetEventMessagesForTimeRangeRequest request, IServerStreamWriter<EventMessage> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadEventMessagesForTimeRange>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Events.ReadEventMessagesForTimeRangeRequest() {
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                PageSize = request.PageSize,
                Page = request.Page,
                Direction = request.Direction == EventReadDirection.Forwards
                    ? Events.EventReadDirection.Forwards
                    : Events.EventReadDirection.Backwards,
                Topics = request.Topics.ToArray(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = await adapter.Feature.ReadEventMessagesForTimeRange(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var msg) || msg == null) {
                    continue;
                }

                await responseStream.WriteAsync(msg.ToGrpcEventMessage()).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        public override async Task GetEventMessagesUsingCursorPosition(GetEventMessagesUsingCursorPositionRequest request, IServerStreamWriter<EventMessageWithCursorPosition> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadEventMessagesUsingCursor>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Events.ReadEventMessagesUsingCursorRequest() {
                CursorPosition = request.CursorPosition,
                PageSize = request.PageSize,
                Direction = request.Direction == EventReadDirection.Forwards
                    ? Events.EventReadDirection.Forwards
                    : Events.EventReadDirection.Backwards,
                Topic = request.Topic,
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = await adapter.Feature.ReadEventMessagesUsingCursor(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var msg) || msg == null) {
                    continue;
                }

                await responseStream.WriteAsync(msg.ToGrpcEventMessageWithCursorPosition()).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        public override async Task WriteEventMessages(IAsyncStreamReader<WriteEventMessageRequest> requestStream, IServerStreamWriter<WriteEventMessageResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var cancellationToken = context.CancellationToken;

            // For writing values to the target adapters.
            var writeChannels = new Dictionary<string, System.Threading.Channels.Channel<Events.WriteEventMessageItem>>(StringComparer.OrdinalIgnoreCase);

            try {
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    var request = requestStream.Current;
                    if (request == null) {
                        continue;
                    }

                    if (!writeChannels.TryGetValue(request.AdapterId, out var writeChannel)) {
                        // We've not created a write channel to this adapter, so we'll create one now.
                        var adapter = await Util.ResolveAdapterAndFeature<IWriteEventMessages>(adapterCallContext, _adapterAccessor, request.AdapterId, cancellationToken).ConfigureAwait(false);
                        var adapterId = adapter.Adapter.Descriptor.Id;

                        writeChannel = System.Threading.Channels.Channel.CreateUnbounded<Events.WriteEventMessageItem>(new System.Threading.Channels.UnboundedChannelOptions() {
                            AllowSynchronousContinuations = false,
                            SingleReader = true,
                            SingleWriter = true
                        });
                        writeChannels[adapterId] = writeChannel;

                        var resultsChannel = await adapter.Feature.WriteEventMessages(adapterCallContext, writeChannel.Reader, cancellationToken).ConfigureAwait(false);
                        resultsChannel.RunBackgroundOperation(async (ch, ct) => {
                            while (!ct.IsCancellationRequested) {
                                var val = await ch.ReadAsync(ct).ConfigureAwait(false);
                                if (val == null) {
                                    continue;
                                }
                                await responseStream.WriteAsync(val.ToGrpcWriteEventMessageResult(adapterId)).ConfigureAwait(false);
                            }
                        }, _backgroundTaskService, cancellationToken);
                    }

                    var adapterRequest = request.ToAdapterWriteEventMessageItem();
                    Util.ValidateObject(adapterRequest);
                    writeChannel.Writer.TryWrite(adapterRequest);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                foreach (var item in writeChannels) {
                    item.Value.Writer.TryComplete(e);
                }
            }
            finally {
                foreach (var item in writeChannels) {
                    item.Value.Writer.TryComplete();
                }
            }
        }

    }
}
