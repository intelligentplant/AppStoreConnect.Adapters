using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.RealTimeData;
using DataCore.Adapter.RealTimeData;

using Grpc.Core;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="TagValuesService.TagValuesServiceBase"/>.
    /// </summary>
    public class TagValuesServiceImpl : TagValuesService.TagValuesServiceBase {

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// The service for registering background task operations.
        /// </summary>
        private readonly IBackgroundTaskService _backgroundTaskService;


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


        /// <inheritdoc/>
        public override async Task CreateSnapshotPushChannel(IAsyncStreamReader<CreateSnapshotPushChannelRequest> requestStream, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var cancellationToken = context.CancellationToken;

            var updateChannel = Channel.CreateUnbounded<TagValueSubscriptionUpdate>();
            Activity? activity = null;

            try {
                // Keep reading from the request stream until we get an item that allows us to create 
                // the subscription.
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (requestStream.Current.OperationCase != CreateSnapshotPushChannelRequest.OperationOneofCase.Create) {
                        continue;
                    }

                    // We received a create request!

                    var adapter = await Util.ResolveAdapterAndFeature<ISnapshotTagValuePush>(adapterCallContext, _adapterAccessor, requestStream.Current.Create.AdapterId, cancellationToken).ConfigureAwait(false);
                    var adapterRequest = new CreateSnapshotTagValueSubscriptionRequest() {
                        Tags = requestStream.Current.Create.Tags.ToArray(),
                        PublishInterval = requestStream.Current.Create.PublishInterval.ToTimeSpan(),
                        Properties = new Dictionary<string, string>(requestStream.Current.Create.Properties)
                    };
                    Util.ValidateObject(adapterRequest);

                    activity = Telemetry.ActivitySource.StartSnapshotTagValuePushSubscribeActivity(adapter.Adapter.Descriptor.Id, adapterRequest);
                    var subscription = await adapter.Feature.Subscribe(adapterCallContext, adapterRequest, updateChannel, cancellationToken).ConfigureAwait(false);

                    subscription.RunBackgroundOperation(async (ch, ct) => {
                        long outputItems = 0;
                        while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                            while (ch.TryRead(out var val)) {
                                if (val == null) {
                                    continue;
                                }
                                await responseStream.WriteAsync(val.ToGrpcTagValueQueryResult(TagValueQueryType.SnapshotPush)).ConfigureAwait(false);
                                activity.SetResponseItemCountTag(++outputItems);
                            }
                        }
                    }, _backgroundTaskService, cancellationToken);

                    // Break out of the initial loop; we'll handle subscription updates below!
                    break;
                }

                // Now keep reading subscription changes.
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (requestStream.Current.OperationCase != CreateSnapshotPushChannelRequest.OperationOneofCase.Update) {
                        continue;
                    }

                    updateChannel.Writer.TryWrite(new TagValueSubscriptionUpdate() {
                        Action = requestStream.Current.Update.Action == SubscriptionUpdateAction.Subscribe
                            ? Common.SubscriptionUpdateAction.Subscribe
                            : Common.SubscriptionUpdateAction.Unsubscribe,
                        Tags = requestStream.Current.Update.Tags.ToArray()
                    });
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) {
                updateChannel.Writer.TryComplete(e);
            }
            finally {
                updateChannel.Writer.TryComplete();
                activity?.Dispose();
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

            using (var activity = Telemetry.ActivitySource.StartReadSnapshotTagValuesActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                long outputItems = 0;
                try {
                    await foreach (var item in adapter.Feature.ReadSnapshotTagValues(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false)) {
                        if (item == null) {
                            continue;
                        }

                        ++outputItems;
                        await responseStream.WriteAsync(item.ToGrpcTagValueQueryResult(TagValueQueryType.SnapshotPoll)).ConfigureAwait(false);
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(outputItems);
                }
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

            using (var activity = Telemetry.ActivitySource.StartReadRawTagValuesActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                var reader = await adapter.Feature.ReadRawTagValues(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);

                long outputItems = 0;
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!reader.TryRead(out var val) || val == null) {
                        continue;
                    }

                    await responseStream.WriteAsync(val.ToGrpcTagValueQueryResult(TagValueQueryType.Raw)).ConfigureAwait(false);
                    activity.SetResponseItemCountTag(++outputItems);
                }
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

            using (var activity = Telemetry.ActivitySource.StartReadPlotTagValuesActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                long outputItems = 0;
                try {
                    await foreach (var item in adapter.Feature.ReadPlotTagValues(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false)) {
                        if (item == null) {
                            continue;
                        }

                        ++outputItems;
                        await responseStream.WriteAsync(item.ToGrpcTagValueQueryResult(TagValueQueryType.Plot)).ConfigureAwait(false);
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(outputItems);
                }
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

            using (var activity = Telemetry.ActivitySource.StartReadTagValuesAtTimesActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                var reader = await adapter.Feature.ReadTagValuesAtTimes(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);

                var outputItems = 0;
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!reader.TryRead(out var val) || val == null) {
                        continue;
                    }

                    await responseStream.WriteAsync(val.ToGrpcTagValueQueryResult(TagValueQueryType.ValuesAtTimes)).ConfigureAwait(false);
                    activity.SetResponseItemCountTag(++outputItems);
                }
            }
        }


        /// <inheritdoc/>
        public override async Task GetSupportedDataFunctions(GetSupportedDataFunctionsRequest request, IServerStreamWriter<DataFunctionDescriptor> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadProcessedTagValues>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            using (var activity = Telemetry.ActivitySource.StartGetSupportedDataFunctionsActivity(adapter.Adapter.Descriptor.Id)) {
                long outputItems = 0;
                try {
                    await foreach (var item in adapter.Feature.GetSupportedDataFunctions(adapterCallContext, cancellationToken).ConfigureAwait(false)) {
                        if (item == null) {
                            continue;
                        }

                        ++outputItems;
                        await responseStream.WriteAsync(item.ToGrpcDataFunctionDescriptor()).ConfigureAwait(false);
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(outputItems);
                }
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

            using (var activity = Telemetry.ActivitySource.StartReadProcessedTagValuesActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                long outputItems = 0;
                try {
                    await foreach (var item in adapter.Feature.ReadProcessedTagValues(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false)) {
                        if (item == null) {
                            continue;
                        }

                        ++outputItems;
                        await responseStream.WriteAsync(item.ToGrpcProcessedTagValueQueryResult(TagValueQueryType.Processed)).ConfigureAwait(false);
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(outputItems);
                }
            }
        }


        /// <inheritdoc/>
        public override async Task WriteSnapshotTagValues(IAsyncStreamReader<WriteTagValueRequest> requestStream, IServerStreamWriter<WriteTagValueResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var cancellationToken = context.CancellationToken;

            var valueChannel = Channel.CreateUnbounded<RealTimeData.WriteTagValueItem>();
            Activity? activity = null;

            try {
                // Keep reading from the request stream until we get an item that allows us to create 
                // the subscription.
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (requestStream.Current.OperationCase != WriteTagValueRequest.OperationOneofCase.Init) {
                        continue;
                    }

                    // We received a create request!

                    var adapter = await Util.ResolveAdapterAndFeature<IWriteSnapshotTagValues>(adapterCallContext, _adapterAccessor, requestStream.Current.Init.AdapterId, cancellationToken).ConfigureAwait(false);
                    activity = Telemetry.ActivitySource.StartWriteSnapshotTagValuesActivity(adapter.Adapter.Descriptor.Id);

                    var subscription = await adapter.Feature.WriteSnapshotTagValues(adapterCallContext, valueChannel, cancellationToken).ConfigureAwait(false);

                    subscription.RunBackgroundOperation(async (ch, ct) => {
                        long outputItems = 0;
                        while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                            while (ch.TryRead(out var val)) {
                                if (val == null) {
                                    continue;
                                }
                                await responseStream.WriteAsync(val.ToGrpcWriteTagValueResult()).ConfigureAwait(false);
                                activity.SetResponseItemCountTag(++outputItems);
                            }
                        }
                    }, _backgroundTaskService, cancellationToken);

                    // Break out of the initial loop; we'll handle the actual writes below.
                    break;
                }

                // Now handle write requests.
                long inputItems = 0;
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (requestStream.Current.OperationCase != WriteTagValueRequest.OperationOneofCase.Write) {
                        continue;
                    }

                    valueChannel.Writer.TryWrite(requestStream.Current.Write.ToAdapterWriteTagValueItem());
                    activity.SetRequestItemCountTag(++inputItems);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) {
                valueChannel.Writer.TryComplete(e);
            }
            finally {
                valueChannel.Writer.TryComplete();
                activity?.Dispose();
            }
        }


        /// <inheritdoc/>
        public override async Task WriteHistoricalTagValues(IAsyncStreamReader<WriteTagValueRequest> requestStream, IServerStreamWriter<WriteTagValueResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var cancellationToken = context.CancellationToken;

            var valueChannel = Channel.CreateUnbounded<RealTimeData.WriteTagValueItem>();
            Activity? activity = null;

            try {
                // Keep reading from the request stream until we get an item that allows us to create 
                // the subscription.
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (requestStream.Current.OperationCase != WriteTagValueRequest.OperationOneofCase.Init) {
                        continue;
                    }

                    // We received a create request!

                    var adapter = await Util.ResolveAdapterAndFeature<IWriteHistoricalTagValues>(adapterCallContext, _adapterAccessor, requestStream.Current.Init.AdapterId, cancellationToken).ConfigureAwait(false);
                    activity = Telemetry.ActivitySource.StartWriteHistoricalTagValuesActivity(adapter.Adapter.Descriptor.Id);

                    var subscription = await adapter.Feature.WriteHistoricalTagValues(adapterCallContext, valueChannel, cancellationToken).ConfigureAwait(false);

                    subscription.RunBackgroundOperation(async (ch, ct) => {
                        long outputItems = 0;
                        while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                            while (ch.TryRead(out var val)) {
                                if (val == null) {
                                    continue;
                                }
                                await responseStream.WriteAsync(val.ToGrpcWriteTagValueResult()).ConfigureAwait(false);
                                activity.SetResponseItemCountTag(++outputItems);
                            }
                        }
                    }, _backgroundTaskService, cancellationToken);

                    // Break out of the initial loop; we'll handle the actual writes below!
                    break;
                }

                // Now handle write requests.
                long inputItems = 0;
                while (await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (requestStream.Current.OperationCase != WriteTagValueRequest.OperationOneofCase.Write) {
                        continue;
                    }

                    valueChannel.Writer.TryWrite(requestStream.Current.Write.ToAdapterWriteTagValueItem());
                    activity.SetRequestItemCountTag(++inputItems);
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
