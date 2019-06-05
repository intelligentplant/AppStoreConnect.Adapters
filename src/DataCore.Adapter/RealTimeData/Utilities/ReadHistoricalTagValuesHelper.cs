using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// A helper class that can add support for <see cref="IReadInterpolatedTagValues"/>, 
    /// <see cref="IReadPlotTagValues"/> and <see cref="IReadPlotTagValues"/> support to a driver that 
    /// only natively supports <see cref="IReadRawTagValues"/>.
    /// </summary>
    /// <remarks>
    ///   Interpolated, plot, and processed data queries are handled by querying for raw tag values, 
    ///   and then using utility classes in the <see cref="Utilities"/> namespace to perform additional 
    ///   calculation or aggregation. Native implementations of the data queries will almost always 
    ///   perform better, and should be used if available.
    /// </remarks>
    public class ReadHistoricalTagValuesHelper : IReadInterpolatedTagValues, IReadPlotTagValues, IReadProcessedTagValues, IReadTagValuesAtTimes {

        /// <summary>
        /// The tag search provider.
        /// </summary>
        private readonly ITagSearch _tagSearchProvider;

        /// <summary>
        /// The raw data provider.
        /// </summary>
        private readonly IReadRawTagValues _rawValuesProvider;


        /// <summary>
        /// Creates a new <see cref="ReadHistoricalTagValuesHelper"/> object.
        /// </summary>
        /// <param name="tagSearchProvider">
        ///   The <see cref="ITagSearch"/> instance that will provide the tag definitions for tags 
        ///   being queried.
        /// </param>
        /// <param name="rawValuesProvider">
        ///   The <see cref="IReadRawTagValues"/> instance that will provide raw tag values to the 
        ///   helper.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagSearchProvider"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="rawValuesProvider"/> is <see langword="null"/>.
        /// </exception>
        public ReadHistoricalTagValuesHelper(ITagSearch tagSearchProvider, IReadRawTagValues rawValuesProvider) {
            _tagSearchProvider = tagSearchProvider ?? throw new ArgumentNullException(nameof(tagSearchProvider));
            _rawValuesProvider = rawValuesProvider ?? throw new ArgumentNullException(nameof(rawValuesProvider));
        }


        /// <summary>
        /// Creates a channel that can be used to write tag values to.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. Specify less than 1 for an unbounded channel.
        /// </param>
        /// <param name="fullMode">
        ///   The action to take if the channel reaches capacity. Ignored if <paramref name="capacity"/> is less than 1.
        /// </param>
        /// <returns>
        ///   A new channel.
        /// </returns>
        protected Channel<T> CreateChannel<T>(int capacity, BoundedChannelFullMode fullMode) where T : TagValueQueryResult {
            return capacity > 0
                ? Channel.CreateBounded<T>(new BoundedChannelOptions(capacity) {
                    FullMode = fullMode,
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = true
                })
                : Channel.CreateUnbounded<T>(new UnboundedChannelOptions() {
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = true
                });
        }


        /// <inheritdoc/>
        public ChannelReader<TagValueQueryResult> ReadInterpolatedTagValues(IAdapterCallContext context, ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken) {
            var result = CreateChannel<TagValueQueryResult>(5000, BoundedChannelFullMode.Wait);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var tagDefinitionsReader = _tagSearchProvider.GetTags(context, new GetTagsRequest() {
                    Tags = request.Tags
                }, ct);

                while (await tagDefinitionsReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    if (!tagDefinitionsReader.TryRead(out var tag) || tag == null) {
                        continue;
                    }

                    var valueReader = _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                        Tags = new [] { tag.Id },
                        UtcStartTime = request.UtcStartTime.Subtract(request.SampleInterval),
                        UtcEndTime = request.UtcEndTime,
                        SampleCount = 0,
                        BoundaryType = RawDataBoundaryType.Outside
                    }, ct);

                    var values = new List<TagValue>();
                    while (await valueReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!valueReader.TryRead(out var val) || val == null) {
                            continue;
                        }
                        values.Add(val.Value);
                    }

                    foreach (var val in InterpolationHelper.GetInterpolatedValues(tag, request.UtcStartTime, request.UtcEndTime, request.SampleInterval, InterpolationCalculationType.Interpolate, values)) {
                        if (ct.IsCancellationRequested) {
                            break;
                        }

                        if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            ch.TryWrite(new TagValueQueryResult(tag.Id, tag.Name, val));
                        }
                    }
                }
            }, true, cancellationToken);

            return result;
        }


        /// <inheritdoc/>
        public ChannelReader<TagValueQueryResult> ReadPlotTagValues(IAdapterCallContext context, ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            var result = CreateChannel<TagValueQueryResult>(5000, BoundedChannelFullMode.Wait);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var tagDefinitionsReader = _tagSearchProvider.GetTags(context, new GetTagsRequest() {
                    Tags = request.Tags
                }, ct);

                var bucketSize = PlotHelper.CalculateBucketSize(request.UtcStartTime, request.UtcEndTime, request.Intervals);

                while (await tagDefinitionsReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    if (!tagDefinitionsReader.TryRead(out var tag) || tag == null) {
                        continue;
                    }

                    var valueReader = _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                        Tags = new[] { tag.Id },
                        UtcStartTime = request.UtcStartTime.Subtract(bucketSize),
                        UtcEndTime = request.UtcEndTime,
                        SampleCount = 0,
                        BoundaryType = RawDataBoundaryType.Outside
                    }, ct);

                    var values = new List<TagValue>();
                    while (await valueReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!valueReader.TryRead(out var val) || val == null) {
                            continue;
                        }
                        values.Add(val.Value);
                    }

                    foreach (var val in PlotHelper.GetPlotValues(tag, request.UtcStartTime, request.UtcEndTime, request.Intervals, values)) {
                        if (ct.IsCancellationRequested) {
                            break;
                        }

                        if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            ch.TryWrite(new TagValueQueryResult(tag.Id, tag.Name, val));
                        }
                    }
                }
            }, true, cancellationToken);

            return result;
        }


        /// <inheritdoc/>
        public Task<IEnumerable<DataFunctionDescriptor>> GetSupportedDataFunctions(IAdapterCallContext context, CancellationToken cancellationToken) {
            return Task.FromResult(AggregationHelper.GetSupportedDataFunctions());
        }


        /// <inheritdoc/>
        public ChannelReader<ProcessedTagValueQueryResult> ReadProcessedTagValues(IAdapterCallContext context, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            var result = CreateChannel<ProcessedTagValueQueryResult>(5000, BoundedChannelFullMode.Wait);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var tagDefinitionsReader = _tagSearchProvider.GetTags(context, new GetTagsRequest() {
                    Tags = request.Tags
                }, ct);

                while (await tagDefinitionsReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    if (!tagDefinitionsReader.TryRead(out var tag) || tag == null) {
                        continue;
                    }

                    var valueReader = _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                        Tags = new[] { tag.Id },
                        UtcStartTime = request.UtcStartTime.Subtract(request.SampleInterval),
                        UtcEndTime = request.UtcEndTime,
                        SampleCount = 0,
                        BoundaryType = RawDataBoundaryType.Outside
                    }, ct);

                    var values = new List<TagValue>();
                    while (await valueReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!valueReader.TryRead(out var val) || val == null) {
                            continue;
                        }
                        values.Add(val.Value);
                    }

                    foreach (var dataFunction in request.DataFunctions) {
                        foreach (var val in AggregationHelper.GetAggregatedValues(tag, dataFunction, request.UtcStartTime, request.UtcEndTime, request.SampleInterval, values)) {
                            if (ct.IsCancellationRequested) {
                                break;
                            }

                            if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                                ch.TryWrite(new ProcessedTagValueQueryResult(tag.Id, tag.Name, val, dataFunction));
                            }
                        }
                    }
                }
            }, true, cancellationToken);

            return result;
        }


        /// <inheritdoc/>
        public ChannelReader<TagValueQueryResult> ReadTagValuesAtTimes(IAdapterCallContext context, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            var result = CreateChannel<TagValueQueryResult>(5000, BoundedChannelFullMode.Wait);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var tagDefinitionsReader = _tagSearchProvider.GetTags(context, new GetTagsRequest() {
                    Tags = request.Tags
                }, ct);

                while (await tagDefinitionsReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    if (!tagDefinitionsReader.TryRead(out var tag) || tag == null) {
                        continue;
                    }

                    foreach (var sampleTime in request.UtcSampleTimes) {
                        var valueReader = _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                            Tags = new[] { tag.Id },
                            UtcStartTime = sampleTime.AddSeconds(-1),
                            UtcEndTime = sampleTime.AddSeconds(1),
                            SampleCount = 0,
                            BoundaryType = RawDataBoundaryType.Outside
                        }, ct);

                        var values = new List<TagValue>();
                        while (await valueReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                            if (!valueReader.TryRead(out var val) || val == null) {
                                continue;
                            }
                            values.Add(val.Value);
                        }

                        var valueAtTime = InterpolationHelper.GetValueAtTime(tag, sampleTime, values, tag.DataType == TagDataType.Numeric ? InterpolationCalculationType.Interpolate : InterpolationCalculationType.UsePreviousValue);
                        if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            ch.TryWrite(new TagValueQueryResult(tag.Id, tag.Name, valueAtTime));
                        }
                    }
                }
            }, true, cancellationToken);

            return result;
        }

    }
}
