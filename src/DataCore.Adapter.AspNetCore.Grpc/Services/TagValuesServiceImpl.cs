using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;
using Grpc.Core;
using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Grpc.Server.Services {
    public class TagValuesServiceImpl : TagValuesService.TagValuesServiceBase {

        private readonly IAdapterCallContext _adapterCallContext;

        private readonly IAdapterAccessor _adapterAccessor;

        private readonly IBackgroundTaskService _backgroundTaskService;


        public TagValuesServiceImpl(IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor, IBackgroundTaskService backgroundTaskService) {
            _adapterCallContext = adapterCallContext;
            _adapterAccessor = adapterAccessor;
            _backgroundTaskService = backgroundTaskService;
        }


        public override async Task CreateSnapshotPushChannel(
            IAsyncStreamReader<CreateSnapshotPushChannelRequest> requestStream, 
            IServerStreamWriter<TagValueQueryResult> responseStream, 
            ServerCallContext context
        ) {
            var cancellationToken = context.CancellationToken;

            // Wait for first subscription change.

            if (!await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                return;
            }

            var adapter = await Util.ResolveAdapterAndFeature<ISnapshotTagValuePush>(
                _adapterCallContext,
                _adapterAccessor,
                requestStream.Current?.AdapterId,
                cancellationToken
            ).ConfigureAwait(false);

            // Create subscription on adapter.

            using (var subscription = await adapter.Feature.Subscribe(_adapterCallContext).ConfigureAwait(false)) {
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                if (requestStream.Current.Action == SubscriptionUpdateAction.Subscribe) {
                    // Push initial tag subscription change to subscription.
                    await subscription.AddTagToSubscription(requestStream.Current.Tag).ConfigureAwait(false);
                }

                // Run background operation to push results back to caller.

                subscription.Reader.RunBackgroundOperation(async (ch, ct) => {
                    while (!ct.IsCancellationRequested) {
                        try {
                            var val = await ch.ReadAsync(ct).ConfigureAwait(false);
                            if (val == null) {
                                continue;
                            }

                            await responseStream.WriteAsync(
                                val.ToGrpcTagValueQueryResult(TagValueQueryType.SnapshotPush)
                            ).ConfigureAwait(false);
                        }
                        catch (ChannelClosedException) {
                            break;
                        }
                        catch (OperationCanceledException) {
                            break;
                        }
                    }
                }, _backgroundTaskService, cancellationToken);

                // Now keep pushing new subscription changes to the subscription.

                while (!cancellationToken.IsCancellationRequested) {
                    try {
                        await requestStream.MoveNext(cancellationToken).ConfigureAwait(false);
                        if (requestStream.Current.Action == SubscriptionUpdateAction.Subscribe) {
                            await subscription.AddTagToSubscription(requestStream.Current.Tag).ConfigureAwait(false);
                        }
                        else {
                            await subscription.RemoveTagFromSubscription(requestStream.Current.Tag).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) {
                        // Do nothing
                    }
                }
            }
        }


        public override async Task ReadSnapshotTagValues(ReadSnapshotTagValuesRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadSnapshotTagValues>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.RealTimeData.ReadSnapshotTagValuesRequest() {
                Tags = request.Tags.ToArray(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.ReadSnapshotTagValues(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcTagValueQueryResult(TagValueQueryType.SnapshotPoll)).ConfigureAwait(false);
            }
        }


        public override async Task ReadRawTagValues(ReadRawTagValuesRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadRawTagValues>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.ReadRawTagValuesRequest() {
                Tags = request.Tags.ToArray(),
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                SampleCount = request.SampleCount,
                BoundaryType = request.BoundaryType.ToAdapterRawDataBoundaryType(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.ReadRawTagValues(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcTagValueQueryResult(TagValueQueryType.Raw)).ConfigureAwait(false);
            }
        }


        public override async Task ReadPlotTagValues(ReadPlotTagValuesRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadPlotTagValues>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.ReadPlotTagValuesRequest() {
                Tags = request.Tags.ToArray(),
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                Intervals = request.Intervals,
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.ReadPlotTagValues(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcTagValueQueryResult(TagValueQueryType.Plot)).ConfigureAwait(false);
            }
        }


        public override async Task ReadTagValuesAtTimes(ReadTagValuesAtTimesRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadTagValuesAtTimes>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.ReadTagValuesAtTimesRequest() {
                Tags = request.Tags.ToArray(),
                UtcSampleTimes = request.UtcSampleTimes.Select(x => x.ToDateTime()).ToArray(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.ReadTagValuesAtTimes(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcTagValueQueryResult(TagValueQueryType.ValuesAtTimes)).ConfigureAwait(false);
            }
        }


        public override async Task GetSupportedDataFunctions(GetSupportedDataFunctionsRequest request, IServerStreamWriter<DataFunctionDescriptor> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadProcessedTagValues>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var reader = adapter.Feature.GetSupportedDataFunctions(_adapterCallContext, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcDataFunctionDescriptor()).ConfigureAwait(false);
            }
        }


        public override async Task ReadProcessedTagValues(ReadProcessedTagValuesRequest request, IServerStreamWriter<ProcessedTagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadProcessedTagValues>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.ReadProcessedTagValuesRequest() {
                Tags = request.Tags.ToArray(),
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                SampleInterval = request.SampleInterval.ToTimeSpan(),
                DataFunctions = request.DataFunctions.ToArray(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.ReadProcessedTagValues(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcProcessedTagValueQueryResult(TagValueQueryType.Processed)).ConfigureAwait(false);
            }
        }


        public override async Task WriteSnapshotTagValues(IAsyncStreamReader<WriteTagValueRequest> requestStream, IServerStreamWriter<WriteTagValueResult> responseStream, ServerCallContext context) {
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
                        var adapter = await Util.ResolveAdapterAndFeature<IWriteSnapshotTagValues>(_adapterCallContext, _adapterAccessor, request.AdapterId, cancellationToken).ConfigureAwait(false);
                        var adapterId = adapter.Adapter.Descriptor.Id;

                        writeChannel = ChannelExtensions.CreateTagValueWriteChannel();
                        writeChannels[adapterId] = writeChannel;

                        var resultsChannel = adapter.Feature.WriteSnapshotTagValues(_adapterCallContext, writeChannel.Reader, cancellationToken);
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


        public override async Task WriteHistoricalTagValues(IAsyncStreamReader<WriteTagValueRequest> requestStream, IServerStreamWriter<WriteTagValueResult> responseStream, ServerCallContext context) {
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
                        var adapter = await Util.ResolveAdapterAndFeature<IWriteHistoricalTagValues>(_adapterCallContext, _adapterAccessor, request.AdapterId, cancellationToken).ConfigureAwait(false);
                        var adapterId = adapter.Adapter.Descriptor.Id;

                        writeChannel = ChannelExtensions.CreateTagValueWriteChannel();
                        writeChannels[adapterId] = writeChannel;

                        var resultsChannel = adapter.Feature.WriteHistoricalTagValues(_adapterCallContext, writeChannel.Reader, cancellationToken);
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
