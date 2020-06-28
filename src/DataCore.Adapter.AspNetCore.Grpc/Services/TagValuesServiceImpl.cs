using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore;
using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.AspNetCore.Grpc.Services;
using DataCore.Adapter.RealTimeData;

using Grpc.Core;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="TagValuesService.TagValuesServiceBase"/>.
    /// </summary>
    public class TagValuesServiceImpl : TagValuesService.TagValuesServiceBase {

        /// <summary>
        /// Holds all active subscriptions.
        /// </summary>
        private static readonly ConnectionSubscriptionManager<RealTimeData.TagValueQueryResult, TopicSubscriptionWrapper<RealTimeData.TagValueQueryResult>> s_snapshotSubscriptions = new ConnectionSubscriptionManager<RealTimeData.TagValueQueryResult, TopicSubscriptionWrapper<RealTimeData.TagValueQueryResult>>();

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
        static TagValuesServiceImpl() {
            HeartbeatServiceImpl.HeartbeatReceived += peer => { 
                if (string.IsNullOrWhiteSpace(peer)) {
                    return;
                }

                s_snapshotSubscriptions.SetHeartbeat(peer, DateTime.UtcNow);
            };
        }


        /// <summary>
        /// Creates a new <see cref="TagValuesServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The service for registering background task operations.
        /// </param>
        public TagValuesServiceImpl(IAdapterAccessor adapterAccessor, IBackgroundTaskService backgroundTaskService) {
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
        internal void CleanUpStaleSubscriptions(TimeSpan timeout) {
            foreach (var connectionId in s_snapshotSubscriptions.GetConnectionIds()) {
                if (!s_snapshotSubscriptions.IsConnectionStale(connectionId, timeout)) {
                    continue;
                }

                s_snapshotSubscriptions.RemoveAllSubscriptions(connectionId);
            }
        }


        /// <inheritdoc/>
        public override async Task<CreateSnapshotSubscriptionResponse> CreateSnapshotSubscription(
            CreateSnapshotSubscriptionRequest request, 
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

            var adapter = await Util.ResolveAdapterAndFeature<ISnapshotTagValuePush>(
                adapterCallContext,
                _adapterAccessor,
                adapterId,
                cancellationToken
            ).ConfigureAwait(false);

#pragma warning disable CA2000 // Dispose objects before losing scope
            var wrappedSubscription = new TopicSubscriptionWrapper<RealTimeData.TagValueQueryResult>(
                await adapter.Feature.Subscribe(adapterCallContext, new CreateSnapshotTagValueSubscriptionRequest() { 
                    PublishInterval = request.PublishInterval.ToTimeSpan(),
                    Properties = new Dictionary<string, string>(request.Properties)
                }).ConfigureAwait(false),
                _backgroundTaskService
            );
#pragma warning restore CA2000 // Dispose objects before losing scope

            return new CreateSnapshotSubscriptionResponse() { 
                SubscriptionId = s_snapshotSubscriptions.AddSubscription(context.Peer, wrappedSubscription)
            };
        }


        /// <inheritdoc/>
        public override Task<DeleteSnapshotSubscriptionResponse> DeleteSnapshotSubscription(DeleteSnapshotSubscriptionRequest request, ServerCallContext context) {
            DeleteSnapshotSubscriptionResponse result;

            if (string.IsNullOrWhiteSpace(request.SubscriptionId)) {
                s_snapshotSubscriptions.RemoveAllSubscriptions(context.Peer);
                result = new DeleteSnapshotSubscriptionResponse() {
                    Success = true
                };
            }
            else {
                result = new DeleteSnapshotSubscriptionResponse() {
                    Success = s_snapshotSubscriptions.RemoveSubscription(context.Peer, request.SubscriptionId)
                };
            }
            return Task.FromResult(result);
        }


        /// <inheritdoc/>
        public override async Task CreateSnapshotPushChannel(
            CreateSnapshotPushChannelRequest request, 
            IServerStreamWriter<TagValueQueryResult> responseStream, 
            ServerCallContext context
        ) {
            var cancellationToken = context.CancellationToken;

            if (!s_snapshotSubscriptions.TryGetSubscription(context.Peer, request.SubscriptionId, out var subscription)) {
                return;
            }

            var channel = await subscription.CreateTopicChannel(request.Tag).ConfigureAwait(false);
            try {
                await foreach (var val in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
                    await responseStream.WriteAsync(val.ToGrpcTagValueQueryResult(TagValueQueryType.SnapshotPush)).ConfigureAwait(false);
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
        public override async Task ReadSnapshotTagValues(ReadSnapshotTagValuesRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadSnapshotTagValues>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.RealTimeData.ReadSnapshotTagValuesRequest() {
                Tags = request.Tags.ToArray(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = await adapter.Feature.ReadSnapshotTagValues(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcTagValueQueryResult(TagValueQueryType.SnapshotPoll)).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        public override async Task ReadRawTagValues(ReadRawTagValuesRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadRawTagValues>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.ReadRawTagValuesRequest() {
                Tags = request.Tags.ToArray(),
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                SampleCount = request.SampleCount,
                BoundaryType = request.BoundaryType.ToAdapterRawDataBoundaryType(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = await adapter.Feature.ReadRawTagValues(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcTagValueQueryResult(TagValueQueryType.Raw)).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        public override async Task ReadPlotTagValues(ReadPlotTagValuesRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadPlotTagValues>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.ReadPlotTagValuesRequest() {
                Tags = request.Tags.ToArray(),
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                Intervals = request.Intervals,
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = await adapter.Feature.ReadPlotTagValues(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcTagValueQueryResult(TagValueQueryType.Plot)).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        public override async Task ReadTagValuesAtTimes(ReadTagValuesAtTimesRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadTagValuesAtTimes>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.ReadTagValuesAtTimesRequest() {
                Tags = request.Tags.ToArray(),
                UtcSampleTimes = request.UtcSampleTimes.Select(x => x.ToDateTime()).ToArray(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = await adapter.Feature.ReadTagValuesAtTimes(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcTagValueQueryResult(TagValueQueryType.ValuesAtTimes)).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        public override async Task GetSupportedDataFunctions(GetSupportedDataFunctionsRequest request, IServerStreamWriter<DataFunctionDescriptor> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadProcessedTagValues>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var reader = await adapter.Feature.GetSupportedDataFunctions(adapterCallContext, cancellationToken).ConfigureAwait(false);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcDataFunctionDescriptor()).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        public override async Task ReadProcessedTagValues(ReadProcessedTagValuesRequest request, IServerStreamWriter<ProcessedTagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadProcessedTagValues>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.ReadProcessedTagValuesRequest() {
                Tags = request.Tags.ToArray(),
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                SampleInterval = request.SampleInterval.ToTimeSpan(),
                DataFunctions = request.DataFunctions.ToArray(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = await adapter.Feature.ReadProcessedTagValues(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcProcessedTagValueQueryResult(TagValueQueryType.Processed)).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        public override async Task WriteSnapshotTagValues(IAsyncStreamReader<WriteTagValueRequest> requestStream, IServerStreamWriter<WriteTagValueResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var cancellationToken = context.CancellationToken;

            // For writing values to the target adapters.
            var writeChannels = new Dictionary<string, System.Threading.Channels.Channel<RealTimeData.WriteTagValueItem>>(StringComparer.OrdinalIgnoreCase);

            try {
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    var request = requestStream.Current;
                    if (request == null) {
                        continue;
                    }

                    if (!writeChannels.TryGetValue(request.AdapterId, out var writeChannel)) {
                        // We've not created a write channel to this adapter, so we'll create one now.
                        var adapter = await Util.ResolveAdapterAndFeature<IWriteSnapshotTagValues>(adapterCallContext, _adapterAccessor, request.AdapterId, cancellationToken).ConfigureAwait(false);
                        var adapterId = adapter.Adapter.Descriptor.Id;

                        writeChannel = ChannelExtensions.CreateTagValueWriteChannel();
                        writeChannels[adapterId] = writeChannel;

                        var resultsChannel = await adapter.Feature.WriteSnapshotTagValues(adapterCallContext, writeChannel.Reader, cancellationToken).ConfigureAwait(false);
                        resultsChannel.RunBackgroundOperation(async (ch, ct) => {
                            while (!ct.IsCancellationRequested) {
                                var val = await ch.ReadAsync(ct).ConfigureAwait(false);
                                if (val == null) {
                                    continue;
                                }
                                await responseStream.WriteAsync(val.ToGrpcWriteTagValueResult(adapterId)).ConfigureAwait(false);
                            }
                        }, _backgroundTaskService, cancellationToken);
                    }

                    var adapterRequest = request.ToAdapterWriteTagValueItem();
                    Util.ValidateObject(adapterRequest);
                    await writeChannel.Writer.WriteAsync(adapterRequest, cancellationToken).ConfigureAwait(false);
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


        /// <inheritdoc/>
        public override async Task WriteHistoricalTagValues(IAsyncStreamReader<WriteTagValueRequest> requestStream, IServerStreamWriter<WriteTagValueResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var cancellationToken = context.CancellationToken;

            // For writing values to the target adapters.
            var writeChannels = new Dictionary<string, System.Threading.Channels.Channel<RealTimeData.WriteTagValueItem>>(StringComparer.OrdinalIgnoreCase);

            try {
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    var request = requestStream.Current;
                    if (request == null) {
                        continue;
                    }

                    if (!writeChannels.TryGetValue(request.AdapterId, out var writeChannel)) {
                        // We've not created a write channel to this adapter, so we'll create one now.
                        var adapter = await Util.ResolveAdapterAndFeature<IWriteHistoricalTagValues>(adapterCallContext, _adapterAccessor, request.AdapterId, cancellationToken).ConfigureAwait(false);
                        var adapterId = adapter.Adapter.Descriptor.Id;

                        writeChannel = ChannelExtensions.CreateTagValueWriteChannel();
                        writeChannels[adapterId] = writeChannel;

                        var resultsChannel = await adapter.Feature.WriteHistoricalTagValues(adapterCallContext, writeChannel.Reader, cancellationToken).ConfigureAwait(false);
                        resultsChannel.RunBackgroundOperation(async (ch, ct) => {
                            while (!ct.IsCancellationRequested) {
                                var val = await ch.ReadAsync(ct).ConfigureAwait(false);
                                if (val == null) {
                                    continue;
                                }
                                await responseStream.WriteAsync(val.ToGrpcWriteTagValueResult(adapterId)).ConfigureAwait(false);
                            }
                        }, _backgroundTaskService, cancellationToken);
                    }

                    var adapterRequest = request.ToAdapterWriteTagValueItem();
                    Util.ValidateObject(adapterRequest);
                    await writeChannel.Writer.WriteAsync(adapterRequest, cancellationToken).ConfigureAwait(false);
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
