﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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


        /// <inheritdoc/>
        public override async Task CreateEventPushChannel(CreateEventPushChannelRequest request, IServerStreamWriter<EventMessage> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IEventMessagePush>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            using (var subscription = await adapter.Feature.Subscribe(adapterCallContext, request.SubscriptionType == EventSubscriptionType.Active ? EventMessageSubscriptionType.Active : EventMessageSubscriptionType.Passive).ConfigureAwait(false)) {
                while (!cancellationToken.IsCancellationRequested) {
                    try {
                        var msg = await subscription.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                        await responseStream.WriteAsync(msg.ToGrpcEventMessage()).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                        // Do nothing
                    }
                    catch (System.Threading.Channels.ChannelClosedException) { 
                        // Do nothing
                    }
                }
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
                    : Events.EventReadDirection.Backwards
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
                    : Events.EventReadDirection.Backwards
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
