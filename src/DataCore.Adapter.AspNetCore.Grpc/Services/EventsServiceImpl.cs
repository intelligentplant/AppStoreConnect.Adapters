using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Events;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {
    public class EventsServiceImpl : EventsService.EventsServiceBase {

        private readonly IAdapterCallContext _adapterCallContext;

        private readonly IAdapterAccessor _adapterAccessor;

        private readonly IBackgroundTaskService _backgroundTaskService;

        private static readonly ConcurrentDictionary<string, Events.IEventMessageSubscription> s_subscriptions = new ConcurrentDictionary<string, Events.IEventMessageSubscription>();


        public EventsServiceImpl(IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor, IBackgroundTaskService backgroundTaskService) {
            _adapterCallContext = adapterCallContext;
            _adapterAccessor = adapterAccessor;
            _backgroundTaskService = backgroundTaskService;
        }


        public override async Task CreateEventPushChannel(CreateEventPushChannelRequest request, IServerStreamWriter<EventMessage> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IEventMessagePush>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var key = $"{_adapterCallContext.ConnectionId}:{nameof(EventsServiceImpl)}:{adapter.Adapter.Descriptor.Id}:{request.SubscriptionType}".ToUpperInvariant();
            if (s_subscriptions.TryGetValue(key, out var _)) {
                throw new RpcException(new Status(StatusCode.AlreadyExists, string.Format(Resources.Error_DuplicateEventSubscriptionAlreadyExists, adapterId)));
            }

            using (var subscription = await adapter.Feature.Subscribe(_adapterCallContext, request.SubscriptionType == EventSubscriptionType.Active ? Events.EventMessageSubscriptionType.Active : Events.EventMessageSubscriptionType.Passive, cancellationToken).ConfigureAwait(false)) {
                try {
                    s_subscriptions[key] = subscription;
                    while (!cancellationToken.IsCancellationRequested) {
                        try {
                            var msg = await subscription.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                            await responseStream.WriteAsync(msg.ToGrpcEventMessage()).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) {
                            // Do nothing
                        }
                    }
                }
                finally {
                    s_subscriptions.TryRemove(key, out var _);
                }
            }
        }


        public override async Task GetEventMessagesForTimeRange(GetEventMessagesForTimeRangeRequest request, IServerStreamWriter<EventMessage> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadEventMessagesForTimeRange>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Events.ReadEventMessagesForTimeRangeRequest() {
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                MessageCount = request.MessageCount,
                Direction = request.Direction == EventReadDirection.Forwards
                    ? Events.EventReadDirection.Forwards
                    : Events.EventReadDirection.Backwards
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.ReadEventMessages(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var msg) || msg == null) {
                    continue;
                }

                await responseStream.WriteAsync(msg.ToGrpcEventMessage()).ConfigureAwait(false);
            }
        }


        public override async Task GetEventMessagesUsingCursorPosition(GetEventMessagesUsingCursorPositionRequest request, IServerStreamWriter<EventMessageWithCursorPosition> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadEventMessagesUsingCursor>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Events.ReadEventMessagesUsingCursorRequest() {
                CursorPosition = request.CursorPosition,
                MessageCount = request.MessageCount,
                Direction = request.Direction == EventReadDirection.Forwards
                    ? Events.EventReadDirection.Forwards
                    : Events.EventReadDirection.Backwards
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.ReadEventMessages(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var msg) || msg == null) {
                    continue;
                }

                await responseStream.WriteAsync(new EventMessageWithCursorPosition() {
                    CursorPosition = msg.CursorPosition,
                    EventMessage = msg.ToGrpcEventMessage()
                }).ConfigureAwait(false);
            }
        }


        public override async Task WriteEventMessages(IAsyncStreamReader<WriteEventMessageRequest> requestStream, IServerStreamWriter<WriteEventMessageResult> responseStream, ServerCallContext context) {
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
                        var adapter = await Util.ResolveAdapterAndFeature<IWriteEventMessages>(_adapterCallContext, _adapterAccessor, request.AdapterId, cancellationToken).ConfigureAwait(false);
                        var adapterId = adapter.Adapter.Descriptor.Id;

                        writeChannel = System.Threading.Channels.Channel.CreateUnbounded<Events.WriteEventMessageItem>(new System.Threading.Channels.UnboundedChannelOptions() {
                            AllowSynchronousContinuations = true,
                            SingleReader = true,
                            SingleWriter = true
                        });
                        writeChannels[adapterId] = writeChannel;

                        var resultsChannel = adapter.Feature.WriteEventMessages(_adapterCallContext, writeChannel.Reader, cancellationToken);
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
            catch (Exception e) {
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
