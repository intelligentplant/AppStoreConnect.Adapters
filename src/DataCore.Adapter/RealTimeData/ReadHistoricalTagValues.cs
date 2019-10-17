using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Utilities;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// A helper class that can add support for <see cref="IReadInterpolatedTagValues"/>, 
    /// <see cref="IReadPlotTagValues"/> and <see cref="IReadPlotTagValues"/> support to an adapter 
    /// that only natively supports <see cref="IReadRawTagValues"/>.
    /// </summary>
    /// <remarks>
    ///   Interpolated, plot, and processed data queries are handled by querying for raw tag values, 
    ///   and then using utility classes in the <see cref="Utilities"/> namespace to perform additional 
    ///   calculation or aggregation. Native implementations of the data queries will almost always 
    ///   perform better, and should be used if available.
    /// </remarks>
    public class ReadHistoricalTagValues : IReadInterpolatedTagValues, IReadPlotTagValues, IReadProcessedTagValues, IReadTagValuesAtTimes {

        /// <summary>
        /// The tag search provider.
        /// </summary>
        private readonly ITagSearch _tagSearchProvider;

        /// <summary>
        /// The raw data provider.
        /// </summary>
        private readonly IReadRawTagValues _rawValuesProvider;


        /// <summary>
        /// Creates a new <see cref="ReadHistoricalTagValues"/> object.
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
        public ReadHistoricalTagValues(ITagSearch tagSearchProvider, IReadRawTagValues rawValuesProvider) {
            _tagSearchProvider = tagSearchProvider ?? throw new ArgumentNullException(nameof(tagSearchProvider));
            _rawValuesProvider = rawValuesProvider ?? throw new ArgumentNullException(nameof(rawValuesProvider));
        }


        /// <inheritdoc/>
        public ChannelReader<TagValueQueryResult> ReadInterpolatedTagValues(IAdapterCallContext context, ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var tagDefinitionsReader = _tagSearchProvider.GetTags(context, new GetTagsRequest() {
                    Tags = request.Tags
                }, ct);

                while (await tagDefinitionsReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    if (!tagDefinitionsReader.TryRead(out var tag) || tag == null) {
                        continue;
                    }

                    var rawValuesReader = _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                        Tags = new [] { tag.Id },
                        UtcStartTime = request.UtcStartTime.Subtract(request.SampleInterval),
                        UtcEndTime = request.UtcEndTime,
                        SampleCount = 0,
                        BoundaryType = RawDataBoundaryType.Outside
                    }, ct);


                    var resultValuesReader = InterpolationHelper.GetInterpolatedValues(tag, request.UtcStartTime, request.UtcEndTime, request.SampleInterval, rawValuesReader, ct);
                    while (await resultValuesReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!resultValuesReader.TryRead(out var val) || val == null) {
                            continue;
                        }

                        if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            ch.TryWrite(val);
                        }
                    }
                }
            }, true, cancellationToken);

            return result;
        }


        /// <inheritdoc/>
        public ChannelReader<TagValueQueryResult> ReadPlotTagValues(IAdapterCallContext context, ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var tagDefinitionsReader = _tagSearchProvider.GetTags(context, new GetTagsRequest() {
                    Tags = request.Tags
                }, ct);

                var bucketSize = PlotHelper.CalculateBucketSize(request.UtcStartTime, request.UtcEndTime, request.Intervals);

                while (await tagDefinitionsReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    if (!tagDefinitionsReader.TryRead(out var tag) || tag == null) {
                        continue;
                    }

                    var rawValuesReader = _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                        Tags = new[] { tag.Id },
                        UtcStartTime = request.UtcStartTime.Subtract(bucketSize),
                        UtcEndTime = request.UtcEndTime,
                        SampleCount = 0,
                        BoundaryType = RawDataBoundaryType.Outside
                    }, ct);

                    var resultValuesReader = PlotHelper.GetPlotValues(tag, request.UtcStartTime, request.UtcEndTime, bucketSize, rawValuesReader, ct);
                    while (await resultValuesReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!resultValuesReader.TryRead(out var val) || val == null) {
                            continue;
                        }

                        if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            ch.TryWrite(val);
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
            var result = ChannelExtensions.CreateTagValueChannel<ProcessedTagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var tagDefinitionsReader = _tagSearchProvider.GetTags(context, new GetTagsRequest() {
                    Tags = request.Tags
                }, ct);

                while (await tagDefinitionsReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    if (!tagDefinitionsReader.TryRead(out var tag) || tag == null) {
                        continue;
                    }

                    var rawValuesReader = _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                        Tags = new[] { tag.Id },
                        UtcStartTime = request.UtcStartTime.Subtract(request.SampleInterval),
                        UtcEndTime = request.UtcEndTime,
                        SampleCount = 0,
                        BoundaryType = RawDataBoundaryType.Outside
                    }, ct);

                    var resultValuesReader = AggregationHelper.GetAggregatedValues(tag, request.DataFunctions, request.UtcStartTime, request.UtcEndTime, request.SampleInterval, rawValuesReader, ct);
                    while (await resultValuesReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!resultValuesReader.TryRead(out var val) || val == null) {
                            continue;
                        }

                        if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            ch.TryWrite(val);
                        }
                    }
                }
            }, true, cancellationToken);

            return result;
        }


        /// <inheritdoc/>
        public ChannelReader<TagValueQueryResult> ReadTagValuesAtTimes(IAdapterCallContext context, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var tagDefinitionsReader = _tagSearchProvider.GetTags(context, new GetTagsRequest() {
                    Tags = request.Tags
                }, ct);

                while (await tagDefinitionsReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    if (!tagDefinitionsReader.TryRead(out var tag) || tag == null) {
                        continue;
                    }

                    // Values-at-times queries are managed differently to regular interpolated 
                    // queries. For values-at-times, we make a raw data query with an outside 
                    // boundary type for every requested sample time (in case the sample times 
                    // span a huge number of raw samples). We then write the values received 
                    // from the resulting channel into a master raw data channel, which is used 
                    // by the InterpolationHelper to calcukate the required values.

                    var rawValuesChannel = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>();

                    rawValuesChannel.Writer.RunBackgroundOperation(async (ch2, ct2) => {
                        foreach (var sampleTime in request.UtcSampleTimes) {
                            var valueReader = _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                                Tags = new[] { tag.Id },
                                UtcStartTime = sampleTime.AddSeconds(-1),
                                UtcEndTime = sampleTime.AddSeconds(1),
                                SampleCount = 0,
                                BoundaryType = RawDataBoundaryType.Outside
                            }, ct2);

                            while (await valueReader.WaitToReadAsync(ct2).ConfigureAwait(false)) {
                                if (!valueReader.TryRead(out var val) || val == null) {
                                    continue;
                                }
                                ch2.TryWrite(val);
                            }
                        }
                    }, true, ct);

                    var resultValuesReader = InterpolationHelper.GetValuesAtSampleTimes(tag, request.UtcSampleTimes, rawValuesChannel, ct);
                    while (await resultValuesReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!resultValuesReader.TryRead(out var val) || val == null) {
                            continue;
                        }

                        if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            ch.TryWrite(val);
                        }
                    }
                }
            }, true, cancellationToken);

            return result;
        }

    }
}
