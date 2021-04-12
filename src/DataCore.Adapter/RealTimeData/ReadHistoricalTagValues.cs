using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData.Utilities;
using DataCore.Adapter.Tags;

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
    public class ReadHistoricalTagValues : IReadPlotTagValues, IReadProcessedTagValues, IReadTagValuesAtTimes, IBackgroundTaskServiceProvider {

        /// <summary>
        /// The ID of the associated adapter.
        /// </summary>
        private readonly string? _adapterId;

        /// <summary>
        /// The tag info provider.
        /// </summary>
        private readonly ITagInfo _tagInfoProvider;

        /// <summary>
        /// The raw data provider.
        /// </summary>
        private readonly IReadRawTagValues _rawValuesProvider;

        /// <inheritdoc/>
        public IBackgroundTaskService BackgroundTaskService { get; }

        /// <summary>
        /// Provides aggregation support.
        /// </summary>
        private readonly AggregationHelper _aggregationHelper = new AggregationHelper();


        /// <summary>
        /// Creates a new <see cref="ReadHistoricalTagValues"/> object.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the associated adapter.
        /// </param>
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
        public ReadHistoricalTagValues(string? adapterId, ITagInfo tagInfoProvider, IReadRawTagValues rawValuesProvider, IBackgroundTaskService? backgroundTaskService) {
            _adapterId = adapterId;
            _tagInfoProvider = tagInfoProvider ?? throw new ArgumentNullException(nameof(tagInfoProvider));
            _rawValuesProvider = rawValuesProvider ?? throw new ArgumentNullException(nameof(rawValuesProvider));
            BackgroundTaskService = backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;
        }


        /// <summary>
        /// Creates a new <see cref="ReadHistoricalTagValues"/> object for the specified adapter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   A new <see cref="ReadHistoricalTagValues"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapter"/> does not meet the requirements specified by 
        ///   <see cref="IsCompatible"/>.
        /// </exception>
        public static ReadHistoricalTagValues ForAdapter(IAdapter adapter) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }

            if (!IsCompatible(adapter)) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterIsNotCompatibleWithHelperClass, adapter.Descriptor.Name, nameof(ReadHistoricalTagValues)), nameof(adapter));
            }

            return new ReadHistoricalTagValues(
                adapter.Descriptor.Id,
                adapter.Features.Get<ITagInfo>(), 
                adapter.Features.Get<IReadRawTagValues>(), 
                adapter.BackgroundTaskService
            );
        }


        /// <summary>
        /// Tests if an adapter is compatible with <see cref="ReadHistoricalTagValues"/>. An 
        /// adapter is compatible if it implements both <see cref="ITagInfo"/> and 
        /// <see cref="IReadRawTagValues"/> features.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <paramref name="adapter"/> is compatible with 
        ///   <see cref="ReadHistoricalTagValues"/>, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsCompatible(IAdapter adapter) {
            if (adapter == null) {
                return false;
            }

            return 
                adapter.Features.Get<ITagInfo>() != null && 
                adapter.Features.Get<IReadRawTagValues>() != null;
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<TagValueQueryResult> ReadPlotTagValues(
            IAdapterCallContext context,
            ReadPlotTagValuesRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            ValidationExtensions.ValidateObject(request);


            var tagDefinitionsReader = _tagInfoProvider.GetTags(context, new GetTagsRequest() {
                Tags = request.Tags
            }, cancellationToken);

            var bucketSize = PlotHelper.CalculateBucketSize(request.UtcStartTime, request.UtcEndTime, request.Intervals);

            await foreach (var tag in tagDefinitionsReader.ConfigureAwait(false)) {
                if (tag == null) {
                    continue;
                }

                var rawValuesReader = await _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                    Tags = new[] { tag.Id },
                    UtcStartTime = request.UtcStartTime.Subtract(bucketSize),
                    UtcEndTime = request.UtcEndTime,
                    SampleCount = 0,
                    BoundaryType = RawDataBoundaryType.Outside
                }, cancellationToken).ConfigureAwait(false);

                var resultValuesReader = PlotHelper.GetPlotValues(tag, request.UtcStartTime, request.UtcEndTime, bucketSize, rawValuesReader.ReadAllAsync(cancellationToken), cancellationToken);
                await foreach (var val in resultValuesReader.ConfigureAwait(false)) {
                    yield return val;
                }
            }
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
        public Task<ChannelReader<DataFunctionDescriptor>> GetSupportedDataFunctions(IAdapterCallContext context, CancellationToken cancellationToken) {
            return Task.FromResult(_aggregationHelper.GetSupportedDataFunctions().PublishToChannel());
        }


        /// <summary>
        /// Gets descriptors for the default data functions supported by <see cref="ReadHistoricalTagValues"/>.
        /// </summary>
        /// <returns>
        ///   The default data function descriptors.
        /// </returns>
        public static IEnumerable<DataFunctionDescriptor> GetDefaultDataFunctions() {
            return AggregationHelper.GetDefaultDataFunctions();
        }


        /// <inheritdoc/>
        public Task<ChannelReader<ProcessedTagValueQueryResult>> ReadProcessedTagValues(IAdapterCallContext context, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            ValidationExtensions.ValidateObject(request);

            var result = ChannelExtensions.CreateProcessedTagValueChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var tagDefinitionsReader = _tagInfoProvider.GetTags(context, new GetTagsRequest() {
                    Tags = request.Tags
                }, ct);

                await foreach (var tag in tagDefinitionsReader.ConfigureAwait(false)) {
                    if (tag == null) {
                        continue;
                    }

                    var rawValuesReader = await _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                        Tags = new[] { tag.Id },
                        UtcStartTime = request.UtcStartTime,
                        UtcEndTime = request.UtcEndTime,
                        SampleCount = 0,
                        BoundaryType = RawDataBoundaryType.Outside
                    }, ct).ConfigureAwait(false);

                    var resultValuesReader = _aggregationHelper.GetAggregatedValues(tag, request.DataFunctions, request.UtcStartTime, request.UtcEndTime, request.SampleInterval, rawValuesReader.ReadAllAsync(ct), ct);
                    await foreach (var value in resultValuesReader.ConfigureAwait(false)) {
                        await ch.WriteAsync(value, ct).ConfigureAwait(false);
                    }
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult<ChannelReader<ProcessedTagValueQueryResult>>(result);
        }


        /// <inheritdoc/>
        public Task<ChannelReader<TagValueQueryResult>> ReadTagValuesAtTimes(IAdapterCallContext context, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            ValidationExtensions.ValidateObject(request);

            var result = ChannelExtensions.CreateTagValueChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var tagDefinitionsReader = _tagInfoProvider.GetTags(context, new GetTagsRequest() {
                    Tags = request.Tags
                }, ct);

                await foreach (var tag in tagDefinitionsReader.ConfigureAwait(false)) {
                    if (tag == null) {
                        continue;
                    }

                    // Values-at-times queries are managed differently to regular interpolated 
                    // queries. For values-at-times, we make a raw data query with an outside 
                    // boundary type for every requested sample time (in case the sample times 
                    // span a huge number of raw samples). We then write the values received 
                    // from the resulting channel into a master raw data channel, which is used 
                    // by the InterpolationHelper to calcukate the required values.

                    var rawValuesChannel = ChannelExtensions.CreateTagValueChannel();

                    rawValuesChannel.Writer.RunBackgroundOperation(async (ch2, ct2) => {
                        foreach (var sampleTime in request.UtcSampleTimes) {
                            var valueReader = await _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                                Tags = new[] { tag.Id },
                                UtcStartTime = sampleTime.AddSeconds(-1),
                                UtcEndTime = sampleTime.AddSeconds(1),
                                SampleCount = 0,
                                BoundaryType = RawDataBoundaryType.Outside
                            }, ct2).ConfigureAwait(false);

                            while (await valueReader.WaitToReadAsync(ct2).ConfigureAwait(false)) {
                                if (!valueReader.TryRead(out var val) || val == null) {
                                    continue;
                                }
                                ch2.TryWrite(val);
                            }
                        }
                    }, true, BackgroundTaskService, ct);

                    var resultValuesReader = InterpolationHelper.GetPreviousValuesAtSampleTimes(tag, request.UtcSampleTimes, rawValuesChannel.Reader.ReadAllAsync(ct), ct);
                    await foreach (var val in resultValuesReader.WithCancellation(ct).ConfigureAwait(false)) {
                        if (val == null) {
                            continue;
                        }

                        if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            ch.TryWrite(val);
                        }
                    }
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult<ChannelReader<TagValueQueryResult>>(result);
        }

    }
}
