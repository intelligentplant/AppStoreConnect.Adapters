using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Utilities;
using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// A helper class that can add support for <see cref="IReadPlotTagValues"/> and <see cref="IReadProcessedTagValues"/> 
    /// support to an adapter that only natively supports <see cref="IReadRawTagValues"/>.
    /// </summary>
    /// <remarks>
    ///   Interpolated, plot, and processed data queries are handled by querying for raw tag values, 
    ///   and then using utility classes in the <see cref="Utilities"/> namespace to perform additional 
    ///   calculation or aggregation. Native implementations of the data queries will almost always 
    ///   perform better, and should be used if available.
    /// </remarks>
    public class ReadHistoricalTagValues : IReadPlotTagValues, IReadProcessedTagValues, IReadTagValuesAtTimes {

        /// <summary>
        /// The tag info provider.
        /// </summary>
        private readonly ITagInfo _tagInfoProvider;

        /// <summary>
        /// The raw data provider.
        /// </summary>
        private readonly IReadRawTagValues _rawValuesProvider;

        /// <summary>
        /// For running background operations.
        /// </summary>
        private readonly IBackgroundTaskService _backgroundTaskService;

        /// <summary>
        /// Provides aggregation support.
        /// </summary>
        private readonly AggregationHelper _aggregationHelper = new AggregationHelper();


        /// <summary>
        /// Creates a new <see cref="ReadHistoricalTagValues"/> object.
        /// </summary>
        /// <param name="tagInfoProvider">
        ///   The <see cref="ITagInfo"/> instance that will provide the tag definitions for tags 
        ///   being queried.
        /// </param>
        /// <param name="rawValuesProvider">
        ///   The <see cref="IReadRawTagValues"/> instance that will provide raw tag values to the 
        ///   helper.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running background operations.
        ///   Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagInfoProvider"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="rawValuesProvider"/> is <see langword="null"/>.
        /// </exception>
        public ReadHistoricalTagValues(ITagInfo tagInfoProvider, IReadRawTagValues rawValuesProvider, IBackgroundTaskService backgroundTaskService) {
            _tagInfoProvider = tagInfoProvider ?? throw new ArgumentNullException(nameof(tagInfoProvider));
            _rawValuesProvider = rawValuesProvider ?? throw new ArgumentNullException(nameof(rawValuesProvider));
            _backgroundTaskService = backgroundTaskService ?? BackgroundTaskService.Default;
        }


        /// <inheritdoc/>
        public ChannelReader<TagValueQueryResult> ReadPlotTagValues(IAdapterCallContext context, ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var tagDefinitionsReader = _tagInfoProvider.GetTags(context, new GetTagsRequest() {
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

                    var resultValuesReader = PlotHelper.GetPlotValues(tag, request.UtcStartTime, request.UtcEndTime, bucketSize, rawValuesReader, _backgroundTaskService, ct);
                    while (await resultValuesReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!resultValuesReader.TryRead(out var val) || val == null) {
                            continue;
                        }

                        if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            ch.TryWrite(val);
                        }

                    }
                }
            }, true, _backgroundTaskService, cancellationToken);

            return result;
        }


        /// <summary>
        /// Registers a custom data function that can be used in calls to 
        /// <see cref="ReadProcessedTagValues"/>.
        /// </summary>
        /// <param name="descriptor">
        ///   The function descriptor.
        /// </param>
        /// <param name="calculator">
        ///   The calculation delegate for the aggregate function.
        /// </param>
        /// <returns>
        ///   A flag that indicates if the registration was successful. See the remarks section 
        ///   for more information.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="descriptor"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="calculator"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   Registration will fail if another function with the same ID is already registered. 
        ///   Built-in functions cannot be overridden.
        /// </remarks>
        public bool RegisterDataFunction(DataFunctionDescriptor descriptor, AggregateCalculator calculator) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }
            if (calculator == null) {
                throw new ArgumentNullException(nameof(calculator));
            }

            return _aggregationHelper.RegisterDataFunction(descriptor, calculator);
        }


        /// <summary>
        /// Unregisters a custom data function previously registered using <see cref="RegisterDataFunction"/>.
        /// </summary>
        /// <param name="functionId">
        ///   The ID of the custom data function.
        /// </param>
        /// <returns>
        ///   A flag that indicates if the registration was removed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="functionId"/> is <see langword="null"/>.
        /// </exception>
        public bool UnregisterDataFunction(string functionId) {
            if (functionId == null) {
                throw new ArgumentNullException(nameof(functionId));
            }

            return _aggregationHelper.UnregisterDataFunction(functionId);
        }


        /// <inheritdoc/>
        public ChannelReader<DataFunctionDescriptor> GetSupportedDataFunctions(IAdapterCallContext context, CancellationToken cancellationToken) {
            return _aggregationHelper.GetSupportedDataFunctions().PublishToChannel();
        }


        /// <inheritdoc/>
        public ChannelReader<ProcessedTagValueQueryResult> ReadProcessedTagValues(IAdapterCallContext context, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<ProcessedTagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var tagDefinitionsReader = _tagInfoProvider.GetTags(context, new GetTagsRequest() {
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

                    var resultValuesReader = _aggregationHelper.GetAggregatedValues(tag, request.DataFunctions, request.UtcStartTime, request.UtcEndTime, request.SampleInterval, rawValuesReader, _backgroundTaskService, ct);
                    while (await resultValuesReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!resultValuesReader.TryRead(out var val) || val == null) {
                            continue;
                        }

                        if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            ch.TryWrite(val);
                        }
                    }
                }
            }, true, _backgroundTaskService, cancellationToken);

            return result;
        }


        /// <inheritdoc/>
        public ChannelReader<TagValueQueryResult> ReadTagValuesAtTimes(IAdapterCallContext context, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var tagDefinitionsReader = _tagInfoProvider.GetTags(context, new GetTagsRequest() {
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
                    }, true, _backgroundTaskService, ct);

                    var resultValuesReader = InterpolationHelper.GetValuesAtSampleTimes(tag, request.UtcSampleTimes, rawValuesChannel, _backgroundTaskService, ct);
                    while (await resultValuesReader.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!resultValuesReader.TryRead(out var val) || val == null) {
                            continue;
                        }

                        if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            ch.TryWrite(val);
                        }
                    }
                }
            }, true, _backgroundTaskService, cancellationToken);

            return result;
        }

    }
}
