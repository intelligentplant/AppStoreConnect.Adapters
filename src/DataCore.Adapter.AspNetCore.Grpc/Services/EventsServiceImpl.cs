using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Events;
using DataCore.Adapter.Events;
using DataCore.Adapter.RealTimeData;

using Grpc.Core;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="EventsService.EventsServiceBase"/>.
    /// </summary>
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

            var adapterRequest = new CreateEventMessageSubscriptionRequest() {
                SubscriptionType = request.SubscriptionType == EventSubscriptionType.Active
                    ? EventMessageSubscriptionType.Active
                    : EventMessageSubscriptionType.Passive,
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            await foreach (var msg in adapter.Feature.Subscribe(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false)) {
                if (msg == null) {
                    continue;
                }

                await responseStream.WriteAsync(msg.ToGrpcEventMessage()).ConfigureAwait(false);
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
        public override async Task CreateEventTopicPushChannel(IAsyncStreamReader<CreateEventTopicPushChannelRequest> requestStream, IServerStreamWriter<EventMessage> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var cancellationToken = context.CancellationToken;

            var updateChannel = Channel.CreateUnbounded<EventMessageSubscriptionUpdate>();

            try {
                ResolvedAdapterFeature<IEventMessagePushWithTopics> adapter = default;
                CreateEventMessageTopicSubscriptionRequest adapterRequest = default!;

                // Keep reading from the request stream until we get an item that allows us to create 
                // the subscription.
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (requestStream.Current.OperationCase != CreateEventTopicPushChannelRequest.OperationOneofCase.Create) {
                        continue;
                    }

                    // We received a create request!

                    adapter = await Util.ResolveAdapterAndFeature<IEventMessagePushWithTopics>(adapterCallContext, _adapterAccessor, requestStream.Current.Create.AdapterId, cancellationToken).ConfigureAwait(false);
                    adapterRequest = new CreateEventMessageTopicSubscriptionRequest() {
                        SubscriptionType = requestStream.Current.Create.SubscriptionType == EventSubscriptionType.Active
                            ? EventMessageSubscriptionType.Active
                            : EventMessageSubscriptionType.Passive,
                        Topics = requestStream.Current.Create.Topics.ToArray(),
                        Properties = new Dictionary<string, string>(requestStream.Current.Create.Properties)
                    };
                    Util.ValidateObject(adapterRequest);

                    // Start a background task to process the subscription changes.
                    _backgroundTaskService.QueueBackgroundWorkItem(async ct => {
                        while (await requestStream.MoveNext(ct).ConfigureAwait(false)) {
                            if (requestStream.Current.OperationCase != CreateEventTopicPushChannelRequest.OperationOneofCase.Update) {
                                continue;
                            }

                            await updateChannel.Writer.WriteAsync(new EventMessageSubscriptionUpdate() {
                                Action = requestStream.Current.Update.Action == SubscriptionUpdateAction.Subscribe
                                    ? Common.SubscriptionUpdateAction.Subscribe
                                    : Common.SubscriptionUpdateAction.Unsubscribe,
                                Topics = requestStream.Current.Update.Topics.ToArray()
                            }, ct);
                        }
                    }, cancellationToken);

                    // Break out of the initial loop; we'll handle reading results below!
                    break;
                }

                if (adapter.Feature == null) {
                    return;
                }

                await foreach (var val in adapter.Feature.Subscribe(adapterCallContext, adapterRequest, updateChannel.Reader.ReadAllAsync(cancellationToken), cancellationToken).ConfigureAwait(false)) {
                    if (val == null) {
                        continue;
                    }
                    await responseStream.WriteAsync(val.ToGrpcEventMessage()).ConfigureAwait(false);
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
        public override async Task CreateFixedEventTopicPushChannel(CreateEventTopicPushChannelMessage request, IServerStreamWriter<EventMessage> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var cancellationToken = context.CancellationToken;

            var adapter = await Util.ResolveAdapterAndFeature<IEventMessagePushWithTopics>(adapterCallContext, _adapterAccessor, request.AdapterId, cancellationToken).ConfigureAwait(false);
            var adapterRequest = new CreateEventMessageTopicSubscriptionRequest() {
                SubscriptionType = request.SubscriptionType == EventSubscriptionType.Active
                    ? EventMessageSubscriptionType.Active
                    : EventMessageSubscriptionType.Passive,
                Topics = request.Topics.ToArray(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            await foreach (var item in adapter.Feature.Subscribe(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false)) {
                if (item == null) {
                    continue;
                }
                await responseStream.WriteAsync(item.ToGrpcEventMessage()).ConfigureAwait(false);
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

            await foreach (var msg in adapter.Feature.ReadEventMessagesForTimeRange(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false)) {
                if (msg == null) {
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

            await foreach (var msg in adapter.Feature.ReadEventMessagesUsingCursor(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false)) {
                if (msg == null) {
                    continue;
                }

                await responseStream.WriteAsync(msg.ToGrpcEventMessageWithCursorPosition()).ConfigureAwait(false);
            }
        }



        /// <inheritdoc/>
        public override async Task WriteEventMessages(
            IAsyncStreamReader<WriteEventMessageRequest> requestStream, 
            IServerStreamWriter<WriteEventMessageResult> responseStream, 
            ServerCallContext context
        ) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var cancellationToken = context.CancellationToken;

            var valueChannel = Channel.CreateUnbounded<Events.WriteEventMessageItem>();

            try {
                ResolvedAdapterFeature<IWriteEventMessages> adapter = default;
                Events.WriteEventMessagesRequest adapterRequest = null!;

                // Keep reading from the request stream until we get an item that allows us to create 
                // the subscription.
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (requestStream.Current.OperationCase != WriteEventMessageRequest.OperationOneofCase.Init) {
                        continue;
                    }

                    // We received a create request!

                    adapter = await Util.ResolveAdapterAndFeature<IWriteEventMessages>(adapterCallContext, _adapterAccessor, requestStream.Current.Init.AdapterId, cancellationToken).ConfigureAwait(false);
                    adapterRequest = new Adapter.Events.WriteEventMessagesRequest() {
                        Properties = new Dictionary<string, string>(requestStream.Current.Init.Properties)
                    };

                    // Break out of the initial loop; we'll handle the actual writes below!
                    break;
                }

                if (adapterRequest == null) {
                    return;
                }

                // Start a background task to forward additional writes to the adapter
                _backgroundTaskService.QueueBackgroundWorkItem(async ct => {
                    try {
                        while (await requestStream.MoveNext(ct).ConfigureAwait(false)) {
                            if (requestStream.Current.OperationCase != WriteEventMessageRequest.OperationOneofCase.Write) {
                                continue;
                            }

                            await valueChannel.Writer.WriteAsync(requestStream.Current.Write.ToAdapterWriteEventMessageItem(), ct).ConfigureAwait(false);
                        }
                    }
                    catch (Exception e) {
                        valueChannel.Writer.TryComplete(e);
                    }
                    finally {
                        valueChannel.Writer.TryComplete();
                    }
                }, cancellationToken);

                // Stream results back to caller.
                await foreach (var val in adapter.Feature.WriteEventMessages(adapterCallContext, adapterRequest, valueChannel.Reader.ReadAllAsync(cancellationToken), cancellationToken).ConfigureAwait(false)) {
                    if (val == null) {
                        continue;
                    }
                    await responseStream.WriteAsync(val.ToGrpcWriteEventMessageResult()).ConfigureAwait(false);
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


        /// <inheritdoc/>
        public override async Task<WriteEventMessagesResponse> WriteFixedEventMessages(WriteEventMessagesRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IWriteEventMessages>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Events.WriteEventMessagesRequest() {
                Properties = new Dictionary<string, string>(request.Properties)
            };

            var valueChannel = Channel.CreateUnbounded<Events.WriteEventMessageItem>();
            foreach (var item in request.Messages) {
                await valueChannel.Writer.WriteAsync(item.ToAdapterWriteEventMessageItem(), cancellationToken).ConfigureAwait(false);
            }

            var response = new WriteEventMessagesResponse();

            await foreach (var val in adapter.Feature.WriteEventMessages(adapterCallContext, adapterRequest, valueChannel.Reader.ReadAllAsync(cancellationToken), cancellationToken).ConfigureAwait(false)) {
                if (val == null) {
                    continue;
                }
                response.Results.Add(val.ToGrpcWriteEventMessageResult());
            }

            return response;
        }

    }
}
