using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.RealTimeData.Features;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {
    public class TagValuesServiceImpl : TagValuesService.TagValuesServiceBase {

        private readonly IAdapterCallContext _adapterCallContext;

        private readonly IAdapterAccessor _adapterAccessor;

        private static readonly ConcurrentDictionary<string, RealTimeData.ISnapshotTagValueSubscription> s_subscriptions = new ConcurrentDictionary<string, RealTimeData.ISnapshotTagValueSubscription>();


        public TagValuesServiceImpl(IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _adapterCallContext = adapterCallContext;
            _adapterAccessor = adapterAccessor;
        }


        public override async Task CreateSnapshotPushChannel(CreateSnapshotPushChannelRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ISnapshotTagValuePush>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var key = $"{_adapterCallContext.ConnectionId}:{nameof(TagValuesServiceImpl)}:{adapterId}".ToUpperInvariant();
            if (s_subscriptions.TryGetValue(key, out var _)) {
                throw new RpcException(new Status(StatusCode.AlreadyExists, string.Format(Resources.Error_DuplicateSnapshotSubscriptionAlreadyExists, adapterId)));
            }

            using (var subscription = await adapter.Feature.Subscribe(_adapterCallContext, cancellationToken).ConfigureAwait(false)) {
                try {
                    s_subscriptions[key] = subscription;
                    if (request.Tags.Count > 0) {
                        await subscription.AddTagsToSubscription(_adapterCallContext, request.Tags, cancellationToken).ConfigureAwait(false);
                    }

                    while (!cancellationToken.IsCancellationRequested) {
                        try {
                            var tag = await subscription.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                            await responseStream.WriteAsync(tag.Value.ToGrpcTagValue(tag.TagId, tag.TagName, TagValueQueryType.SnapshotPush)).ConfigureAwait(false);
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


        public override async Task GetSnapshotPushChannelTags(GetSnapshotPushChannelTagsRequest request, IServerStreamWriter<TagIdentifier> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var key = $"{_adapterCallContext.ConnectionId}:{nameof(TagValuesServiceImpl)}:{adapterId}".ToUpperInvariant();

            RealTimeData.ISnapshotTagValueSubscription subscription;
            if (!s_subscriptions.TryGetValue(key, out var o) || (subscription = o as RealTimeData.ISnapshotTagValueSubscription) == null) {
                throw new RpcException(new Status(StatusCode.NotFound, string.Format(Resources.Error_SnapshotSubscriptionDoesNotExist, adapterId)));
            }
            
            var cancellationToken = context.CancellationToken;
            var channel = subscription.GetTags(_adapterCallContext, cancellationToken);

            while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!channel.TryRead(out var item) || item == null) {
                    continue;
                }

                await responseStream.WriteAsync(new TagIdentifier() {
                    Id = item.Id,
                    Name = item.Name
                }).ConfigureAwait(false);
            }
        }


        public override async Task<AddTagsToSnapshotPushChannelResponse> AddTagsToSnapshotPushChannel(AddTagsToSnapshotPushChannelRequest request, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var key = $"{_adapterCallContext.ConnectionId}:{nameof(TagValuesServiceImpl)}:{adapterId}".ToUpperInvariant();
            
            RealTimeData.ISnapshotTagValueSubscription subscription;
            if (!s_subscriptions.TryGetValue(key, out var o) || (subscription = o as RealTimeData.ISnapshotTagValueSubscription) == null) {
                throw new RpcException(new Status(StatusCode.NotFound, string.Format(Resources.Error_SnapshotSubscriptionDoesNotExist, adapterId)));
            }

            var cancellationToken = context.CancellationToken;
            return new AddTagsToSnapshotPushChannelResponse() {
                Count = await subscription.AddTagsToSubscription(_adapterCallContext, request.Tags, cancellationToken).ConfigureAwait(false)
            };
        }


        public override async Task<RemoveTagsFromSnapshotPushChannelResponse> RemoveTagsFromSnapshotPushChannel(RemoveTagsFromSnapshotPushChannelRequest request, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var key = $"{_adapterCallContext.ConnectionId}:{nameof(TagValuesServiceImpl)}:{adapterId}".ToUpperInvariant();

            RealTimeData.ISnapshotTagValueSubscription subscription;
            if (!s_subscriptions.TryGetValue(key, out var o) || (subscription = o as RealTimeData.ISnapshotTagValueSubscription) == null) {
                throw new RpcException(new Status(StatusCode.NotFound, string.Format(Resources.Error_SnapshotSubscriptionDoesNotExist, adapterId)));
            }

            var cancellationToken = context.CancellationToken;
            return new RemoveTagsFromSnapshotPushChannelResponse() {
                Count = await subscription.RemoveTagsFromSubscription(_adapterCallContext, request.Tags, cancellationToken).ConfigureAwait(false)
            };
        }


        public override async Task ReadSnapshotTagValues(ReadSnapshotTagValuesRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadSnapshotTagValues>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.RealTimeData.Models.ReadSnapshotTagValuesRequest() {
                Tags = request.Tags.ToArray(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.ReadSnapshotTagValues(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.Value.ToGrpcTagValue(val.TagId, val.TagName, TagValueQueryType.SnapshotPoll)).ConfigureAwait(false);
            }
        }


        public override async Task ReadRawTagValues(ReadRawTagValuesRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadRawTagValues>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.Models.ReadRawTagValuesRequest() {
                Tags = request.Tags.ToArray(),
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                SampleCount = request.SampleCount,
                BoundaryType = request.BoundaryType.FromGrpcRawDataBoundaryType(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.ReadRawTagValues(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.Value.ToGrpcTagValue(val.TagId, val.TagName, TagValueQueryType.Raw)).ConfigureAwait(false);
            }
        }


        public override async Task ReadPlotTagValues(ReadPlotTagValuesRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadPlotTagValues>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.Models.ReadPlotTagValuesRequest() {
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

                await responseStream.WriteAsync(val.Value.ToGrpcTagValue(val.TagId, val.TagName, TagValueQueryType.Plot)).ConfigureAwait(false);
            }
        }


        public override async Task ReadInterpolatedTagValues(ReadInterpolatedTagValuesRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadInterpolatedTagValues>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.Models.ReadInterpolatedTagValuesRequest() {
                Tags = request.Tags.ToArray(),
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                SampleInterval = request.SampleInterval.ToTimeSpan(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.ReadInterpolatedTagValues(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.Value.ToGrpcTagValue(val.TagId, val.TagName, TagValueQueryType.Interpolated)).ConfigureAwait(false);
            }
        }


        public override async Task ReadTagValuesAtTimes(ReadTagValuesAtTimesRequest request, IServerStreamWriter<TagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadTagValuesAtTimes>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.Models.ReadTagValuesAtTimesRequest() {
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

                await responseStream.WriteAsync(val.Value.ToGrpcTagValue(val.TagId, val.TagName, TagValueQueryType.ValuesAtTimes)).ConfigureAwait(false);
            }
        }


        public override async Task<GetSupportedDataFunctionsResponse> GetSupportedDataFunctions(GetSupportedDataFunctionsRequest request, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadProcessedTagValues>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var values = await adapter.Feature.GetSupportedDataFunctions(_adapterCallContext, cancellationToken).ConfigureAwait(false);

            var result = new GetSupportedDataFunctionsResponse();
            result.DataFunctions.AddRange(values.Select(x => x.ToGrpcDataFunctionDescriptor()));

            return result;
        }


        public override async Task ReadProcessedTagValues(ReadProcessedTagValuesRequest request, IServerStreamWriter<ProcessedTagValueQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadProcessedTagValues>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.Models.ReadProcessedTagValuesRequest() {
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

                await responseStream.WriteAsync(val.Value.ToGrpcProcessedTagValue(val.TagId, val.TagName, val.DataFunction, TagValueQueryType.Processed)).ConfigureAwait(false);
            }
        }


        public override async Task WriteSnapshotTagValues(IAsyncStreamReader<WriteTagValueRequest> requestStream, IServerStreamWriter<WriteTagValueResult> responseStream, ServerCallContext context) {
            var cancellationToken = context.CancellationToken;

            // For writing values to the target adapters.
            var writeChannels = new Dictionary<string, System.Threading.Channels.Channel<RealTimeData.Models.WriteTagValueItem>>(StringComparer.OrdinalIgnoreCase);

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
                        }, cancellationToken);
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
            var writeChannels = new Dictionary<string, System.Threading.Channels.Channel<RealTimeData.Models.WriteTagValueItem>>(StringComparer.OrdinalIgnoreCase);

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
                        }, cancellationToken);
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
