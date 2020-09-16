﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Common;
using DataCore.Adapter.Events;

using Grpc.Core;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="EventsService.EventsServiceBase"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Arguments are passed by gRPC framework")]
    public class EventsServiceImpl : EventsService.EventsServiceBase {

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// The service for registering background task operations.
        /// </summary>
        private readonly IBackgroundTaskService _backgroundTaskService;


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
        /// Creates a general event message subscription to an adapter using the adapter's 
        /// <see cref="IEventMessagePush"/> feature. For topic-based event streams, see 
        /// <see cref="CreateEventTopicPushChannel"/>.
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
        /// <seealso cref="CreateEventTopicPushChannel"/>
        public override async Task CreateEventPushChannel(CreateEventPushChannelRequest request, IServerStreamWriter<EventMessage> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IEventMessagePush>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var subscription = await adapter.Feature.Subscribe(adapterCallContext, new CreateEventMessageSubscriptionRequest() {
                SubscriptionType = request.SubscriptionType == EventSubscriptionType.Active
                    ? EventMessageSubscriptionType.Active
                    : EventMessageSubscriptionType.Passive,
                Properties = new Dictionary<string, string>(request.Properties)
            }, cancellationToken).ConfigureAwait(false);

            try {
                while (await subscription.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    while (subscription.TryRead(out var msg) && msg != null) {
                        await responseStream.WriteAsync(msg.ToGrpcEventMessage()).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) {
                // Do nothing
            }
            catch (ChannelClosedException) {
                // Do nothing
            }
        }


        /// <summary>
        /// Creates a topic-based event subscription stream to an adapter using the adapter's 
        /// <see cref="IEventMessagePushWithTopics"/> feature. For an event stream that does not 
        /// use topics, see <see cref="CreateEventPushChannel"/>.
        /// </summary>
        /// <param name="requestStream">
        ///   The request stream.
        /// </param>
        /// <param name="responseStream">
        ///   The response stream.
        /// </param>
        /// <param name="context">
        ///   The call context.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will publish emitted event messages to the <paramref name="responseStream"/>.
        /// </returns>
        /// <seealso cref="CreateEventPushChannel"/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Error is written to channel")]
        public override async Task CreateEventTopicPushChannel(IAsyncStreamReader<CreateEventTopicPushChannelRequest> requestStream, IServerStreamWriter<EventMessage> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var cancellationToken = context.CancellationToken;

            var updateChannel = Channel.CreateUnbounded<EventMessageSubscriptionUpdate>();

            try {
                // Keep reading from the request stream until we get an item that allows us to create 
                // the subscription.
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (requestStream.Current.OperationCase != CreateEventTopicPushChannelRequest.OperationOneofCase.Create) {
                        continue;
                    }

                    // We received a create request!

                    var adapter = await Util.ResolveAdapterAndFeature<IEventMessagePushWithTopics>(adapterCallContext, _adapterAccessor, requestStream.Current.Create.AdapterId, cancellationToken).ConfigureAwait(false);

                    var subscription = await adapter.Feature.Subscribe(adapterCallContext, new CreateEventMessageTopicSubscriptionRequest() {
                        SubscriptionType = requestStream.Current.Create.SubscriptionType == EventSubscriptionType.Active
                                    ? EventMessageSubscriptionType.Active
                                    : EventMessageSubscriptionType.Passive,
                        Topics = requestStream.Current.Create.Topics.ToArray(),
                        Properties = new Dictionary<string, string>(requestStream.Current.Create.Properties)
                    }, updateChannel, cancellationToken).ConfigureAwait(false);

                    subscription.RunBackgroundOperation(async (ch, ct) => {
                        while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                            while (ch.TryRead(out var val)) {
                                if (val == null) {
                                    continue;
                                }
                                await responseStream.WriteAsync(val.ToGrpcEventMessage()).ConfigureAwait(false);
                            }
                        }
                    }, _backgroundTaskService, cancellationToken);

                    // Break out of the initial loop; we'll handle subscription updates below!
                    break;
                }

                // Now keep reading subscription changes.
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (requestStream.Current.OperationCase != CreateEventTopicPushChannelRequest.OperationOneofCase.Update) {
                        continue;
                    }

                    updateChannel.Writer.TryWrite(new EventMessageSubscriptionUpdate() { 
                        Action = requestStream.Current.Update.Action == SubscriptionUpdateAction.Subscribe
                            ? Common.SubscriptionUpdateAction.Subscribe
                            : Common.SubscriptionUpdateAction.Unsubscribe,
                        Topics = requestStream.Current.Update.Topics.ToArray()
                    });
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) {
                updateChannel.Writer.TryComplete(e);
            }
            finally {
                updateChannel.Writer.TryComplete();
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Error is written to channel")]
        public override async Task WriteEventMessages(IAsyncStreamReader<WriteEventMessageRequest> requestStream, IServerStreamWriter<WriteEventMessageResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var cancellationToken = context.CancellationToken;

            var valueChannel = Channel.CreateUnbounded<Events.WriteEventMessageItem>();

            try {
                // Keep reading from the request stream until we get an item that allows us to create 
                // the subscription.
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (requestStream.Current.OperationCase != WriteEventMessageRequest.OperationOneofCase.Init) {
                        continue;
                    }

                    // We received a create request!

                    var adapter = await Util.ResolveAdapterAndFeature<IWriteEventMessages>(adapterCallContext, _adapterAccessor, requestStream.Current.Init.AdapterId, cancellationToken).ConfigureAwait(false);

                    var subscription = await adapter.Feature.WriteEventMessages(adapterCallContext, valueChannel, cancellationToken).ConfigureAwait(false);

                    subscription.RunBackgroundOperation(async (ch, ct) => {
                        while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                            while (ch.TryRead(out var val)) {
                                if (val == null) {
                                    continue;
                                }
                                await responseStream.WriteAsync(val.ToGrpcWriteEventMessageResult()).ConfigureAwait(false);
                            }
                        }
                    }, _backgroundTaskService, cancellationToken);

                    // Break out of the initial loop; we'll handle the actual writes below!
                    break;
                }

                // Now handle write requests.
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (requestStream.Current.OperationCase != WriteEventMessageRequest.OperationOneofCase.Write) {
                        continue;
                    }

                    valueChannel.Writer.TryWrite(requestStream.Current.Write.ToAdapterWriteEventMessageItem());
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) {
                valueChannel.Writer.TryComplete(e);
            }
            finally {
                valueChannel.Writer.TryComplete();
            }
        }

    }
}
