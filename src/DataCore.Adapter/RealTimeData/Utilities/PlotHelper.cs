using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Utility class for creating a visualization-friendly (plot) data set from a set of raw tag 
    /// values.
    /// </summary>
    public static class PlotHelper {

        /// <summary>
        /// Calculates the bucket size to use for the specified query time range and interval count.
        /// </summary>
        /// <param name="utcStartTime">
        ///   The UTC start time for the query time range.
        /// </param>
        /// <param name="utcEndTime">
        ///   The UTC end time for the query time range.
        /// </param>
        /// <param name="intervals">
        ///   The number of intervals to divide the time range into.
        /// </param>
        /// <returns>
        ///   The time span for each bucket.
        /// </returns>
        public static TimeSpan CalculateBucketSize(DateTime utcStartTime, DateTime utcEndTime, int intervals) {
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException(SharedResources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, nameof(utcStartTime));
            }
            if (intervals < 1) {
                throw new ArgumentException(SharedResources.Error_IntervalCountMustBeGreaterThanZero, nameof(intervals));
            }

            return TimeSpan.FromMilliseconds((utcEndTime - utcStartTime).TotalMilliseconds / intervals); ;
        }


        /// <summary>
        /// Creates a visualization-friendly data set for a single tag that is suitable for trending.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="utcStartTime">
        ///   The UTC start time for the plot data set.
        /// </param>
        /// <param name="utcEndTime">
        ///   The UTC end time for the plot data set.
        /// </param>
        /// <param name="bucketSize">
        ///   The bucket size to use when calculating the plot data set.
        /// </param>
        /// <param name="rawData">
        ///   An <see cref="IAsyncEnumerable{T}"/> that will provide the raw data to use in the calculations.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will emit a set of trend-friendly samples.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="utcStartTime"/> is greater than or equal to <paramref name="utcEndTime"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="bucketSize"/> is less than or equal to <see cref="TimeSpan.Zero"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   The plot function works by collecting raw values into buckets. Each bucket covers the 
        ///   same period of time. The method reads from <paramref name="rawData"/> and adds samples 
        ///   to the bucket, until it encounters a sample that has a time stamp that is after the 
        ///   bucket's end time. The function then takes the earliest, latest, minimum and maximum 
        ///   values in the bucket, as well as the first non-good value in the bucket, and adds them 
        ///   to the result data set.
        /// </para>
        /// 
        /// <para>
        ///   It is important then to note that method is not guaranteed to give evenly-spaced time 
        ///   stamps in the resulting data set, but instead returns a data set that 
        ///   gives a reasonable approximation of the tag when visualized.
        /// </para>
        /// 
        /// </remarks>
        public static IAsyncEnumerable<TagValueQueryResult> GetPlotValues(
            TagSummary tag, 
            DateTime utcStartTime, 
            DateTime utcEndTime, 
            TimeSpan bucketSize, 
            IAsyncEnumerable<TagValueQueryResult> rawData,
            CancellationToken cancellationToken = default
        ) {
            return GetPlotValues(tag, utcStartTime, utcEndTime, bucketSize, rawData, null, cancellationToken);
        }


        /// <summary>
        /// Creates a visualization-friendly data set for a single tag that is suitable for trending.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="utcStartTime">
        ///   The UTC start time for the plot data set.
        /// </param>
        /// <param name="utcEndTime">
        ///   The UTC end time for the plot data set.
        /// </param>
        /// <param name="bucketSize">
        ///   The bucket size to use when calculating the plot data set.
        /// </param>
        /// <param name="rawData">
        ///   An <see cref="IAsyncEnumerable{T}"/> that will provide the raw data to use in the calculations.
        /// </param>
        /// <param name="valueSelector">
        ///   A delegate that will select the samples to return from each time bucket. 
        ///   If <see langword="null"/>, <see cref="DefaultPlotValueSelector"/> will be used.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will emit a set of trend-friendly samples.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="utcStartTime"/> is greater than or equal to <paramref name="utcEndTime"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="bucketSize"/> is less than or equal to <see cref="TimeSpan.Zero"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   The plot function works by collecting raw values into buckets. Each bucket covers the 
        ///   same period of time. The method reads from <paramref name="rawData"/> and adds samples 
        ///   to the bucket, until it encounters a sample that has a time stamp that is after the 
        ///   bucket's end time. The function then takes the earliest, latest, minimum and maximum 
        ///   values in the bucket, as well as the first non-good value in the bucket, and adds them 
        ///   to the result data set.
        /// </para>
        /// 
        /// <para>
        ///   It is important then to note that method is not guaranteed to give evenly-spaced time 
        ///   stamps in the resulting data set, but instead returns a data set that 
        ///   gives a reasonable approximation of the tag when visualized.
        /// </para>
        /// 
        /// </remarks>
        public static async IAsyncEnumerable<TagValueQueryResult> GetPlotValues(
            TagSummary tag,
            DateTime utcStartTime,
            DateTime utcEndTime,
            TimeSpan bucketSize,
            IAsyncEnumerable<TagValueQueryResult> rawData,
            PlotValueSelector? valueSelector,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException(SharedResources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, nameof(utcStartTime));
            }
            if (bucketSize <= TimeSpan.Zero) {
                throw new ArgumentException(Resources.Error_BucketSizeMustBeGreaterThanZero, nameof(bucketSize));
            }

            await foreach (var item in GetPlotValuesInternal(tag, utcStartTime, utcEndTime, bucketSize, rawData, valueSelector, cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Creates a visualization-friendly data set suitable for trending.
        /// </summary>
        /// <param name="tags">
        ///   The tag definitions in the query.
        /// </param>
        /// <param name="utcStartTime">
        ///   The UTC start time for the plot data set.
        /// </param>
        /// <param name="utcEndTime">
        ///   The UTC end time for the plot data set.
        /// </param>
        /// <param name="bucketSize">
        ///   The bucket size to use when calculating the plot data set.
        /// </param>
        /// <param name="rawData">
        ///   An <see cref="IAsyncEnumerable{T}"/> that will provide the raw data to use in the calculations.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running background operations.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will emit a set of trend-friendly samples.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tags"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="utcStartTime"/> is greater than or equal to <paramref name="utcEndTime"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="bucketSize"/> is less than or equal to <see cref="TimeSpan.Zero"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   The plot function works by collecting raw values into buckets. Each bucket covers the 
        ///   same period of time. The method reads from <paramref name="rawData"/> and adds samples 
        ///   to the bucket, until it encounters a sample that has a time stamp that is after the 
        ///   bucket's end time. The function then takes the earliest, latest, minimum and maximum 
        ///   values in the bucket, as well as the first non-good value in the bucket, and adds them 
        ///   to the result data set.
        /// </para>
        /// 
        /// <para>
        ///   It is important then to note that method is not guaranteed to give evenly-spaced time 
        ///   stamps in the resulting data set, but instead returns a data set that 
        ///   gives a reasonable approximation of the tag when visualized.
        /// </para>
        /// 
        /// </remarks>
        public static async IAsyncEnumerable<TagValueQueryResult> GetPlotValues(
            IEnumerable<TagSummary> tags, 
            DateTime utcStartTime, 
            DateTime utcEndTime, 
            TimeSpan bucketSize, 
            IAsyncEnumerable<TagValueQueryResult> rawData, 
            IBackgroundTaskService? backgroundTaskService = null,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException(SharedResources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, nameof(utcStartTime));
            }
            if (bucketSize <= TimeSpan.Zero) {
                throw new ArgumentException(Resources.Error_BucketSizeMustBeGreaterThanZero, nameof(bucketSize));
            }
            if (backgroundTaskService == null) {
                backgroundTaskService = BackgroundTaskService.Default;
            }

            if (!tags.Any()) {
                // No tags; complete the channel and return.
                yield break;
            }

            if (tags.Count() == 1) {
                // Single tag; use the optimised single-tag overload.
                await foreach (var item in GetPlotValues(
                    tags.First(), 
                    utcStartTime, 
                    utcEndTime, 
                    bucketSize, 
                    rawData, 
                    cancellationToken
                ).ConfigureAwait(false)) {
                    yield return item;
                }
                yield break;
            }

            // Multiple tags; create a single result channel, and create individual input channels 
            // for each tag in the request and redirect each value emitted from the raw data channel 
            // into the appropriate per-tag input channel.

            var result = Channel.CreateBounded<TagValueQueryResult>(new BoundedChannelOptions(500) {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = false
            });

            var tagLookupById = tags.ToDictionary(x => x.Id);

            var tagRawDataChannels = tags.ToDictionary(x => x.Id, x => Channel.CreateUnbounded<TagValueQueryResult>(new UnboundedChannelOptions() {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = true
            }));

            // Redirect values from input channel to per-tag channel.
            backgroundTaskService.QueueBackgroundWorkItem(async ct => {
                try {
                    await foreach (var val in rawData.WithCancellation(ct).ConfigureAwait(false)) {
                        if (!tagRawDataChannels.TryGetValue(val.TagId, out var perTagChannel)) {
                            continue;
                        }

                        if (await perTagChannel.Writer.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            perTagChannel.Writer.TryWrite(val);
                        }
                    }
                }
                catch (Exception e) {
                    foreach (var item in tagRawDataChannels.Values) {
                        item.Writer.TryComplete(e);
                    }
                    throw;
                }
                finally {
                    foreach (var item in tagRawDataChannels.Values) {
                        item.Writer.TryComplete();
                    }
                }
            }, cancellationToken);

            // Execute stream for each tag in the query and write all values into the shared 
            // result channel.

            async Task GetValuesForTag(TagSummary tag, ChannelReader<TagValueQueryResult> reader, ChannelWriter<TagValueQueryResult> writer, CancellationToken cancellationToken) {
                await foreach (var val in GetPlotValues(tag, utcStartTime, utcEndTime, bucketSize, reader.ReadAllAsync(cancellationToken), cancellationToken).ConfigureAwait(false)) {
                    await writer.WriteAsync(val, cancellationToken).ConfigureAwait(false);
                }
            }

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await Task.WhenAll(tagRawDataChannels.Select(x => {
                    var tag = tagLookupById[x.Key];
                    var channel = x.Value;
                    return GetValuesForTag(tag, channel.Reader, ch, ct);
                })).ConfigureAwait(false);
            }, true, backgroundTaskService, cancellationToken);

            await foreach (var val in result.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
                yield return val;
            }
        }


        /// <summary>
        /// Creates a visualization-friendly data set suitable for trending.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="utcStartTime">
        ///   The UTC start time for the plot data set.
        /// </param>
        /// <param name="utcEndTime">
        ///   The UTC end time for the plot data set.
        /// </param>
        /// <param name="bucketSize">
        ///   The bucket size to use when calculating the plot data set.
        /// </param>
        /// <param name="rawData">
        ///   A channel that will provide the raw data to use in the calculations.
        /// </param>
        /// <param name="valueSelector">
        ///   A delegate that will select the samples to return from each time bucket. 
        ///   If <see langword="null"/>, <see cref="DefaultPlotValueSelector"/> will be used.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will emit the computed values.
        /// </returns>
        private static async IAsyncEnumerable<TagValueQueryResult> GetPlotValuesInternal(
            TagSummary tag,
            DateTime utcStartTime,
            DateTime utcEndTime,
            TimeSpan bucketSize,
            IAsyncEnumerable<TagValueQueryResult> rawData,
            PlotValueSelector? valueSelector,
            [EnumeratorCancellation] CancellationToken cancellationToken
        ) {
            var bucket = new TagValueBucket(utcStartTime, utcStartTime.Add(bucketSize), utcStartTime, utcEndTime);

            await foreach (var val in rawData.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                if (val == null) {
                    continue;
                }

                // Add the sample to the bucket. If the sample is < bucket start time or >= bucket
                // end time it will update a pre-/post-bucket boundary region instead of being
                // added to the samples in the bucket itself.
                bucket.AddRawSample(val.Value);

                if (val.Value.UtcSampleTime < bucket.UtcBucketStart) {
                    if (val.Value.UtcSampleTime > utcEndTime) {
                        // Sample is before the bucket start time and is also greater than the end
                        // time for the query: break from the foreach loop.
                        break;
                    }

                    // Sample is before the bucket start time: move to the next sample.
                    continue;
                }

                if (val.Value.UtcSampleTime >= bucket.UtcBucketEnd) {
                    // The sample we have just received is later than the end time for the current 
                    // bucket.

                    do {
                        // We have a completed bucket; calculate and emit the values for the
                        // bucket.
                        foreach (var calcVal in CalculateAndEmitBucketSamples(tag, bucket, valueSelector)) {
                            yield return calcVal;
                        }

                        // Create a new current bucket.
                        var oldBucket = bucket;
                        bucket = new TagValueBucket(bucket.UtcBucketEnd, bucket.UtcBucketEnd.Add(bucketSize), utcStartTime, utcEndTime);

                        // Copy pre-/post-end boundary values from the old bucket to the new bucket.
                        bucket.AddBoundarySamples(oldBucket);
                    } while (val.Value.UtcSampleTime >= bucket.UtcBucketEnd && bucket.UtcBucketEnd <= utcEndTime);
                }


            }

            foreach (var calcVal in CalculateAndEmitBucketSamples(tag, bucket, valueSelector)) {
                yield return calcVal;
            }

            if (bucket.UtcBucketEnd >= utcEndTime) {
                // We have emitted data for the full query duration.
                yield break;
            }

            // The raw data ended before the end time for the query. We will keep moving forward 
            // according to our sample interval, and allow our plot selector the chance to calculate 
            // values for the remaining buckets.

            while (bucket.UtcBucketEnd < utcEndTime) {
                var oldBucket = bucket;
                bucket = new TagValueBucket(bucket.UtcBucketEnd, bucket.UtcBucketEnd.Add(bucketSize), utcStartTime, utcEndTime);

                // Copy pre-/post-end boundary values from the old bucket to the new bucket.
                bucket.AddBoundarySamples(oldBucket);

                foreach (var calcVal in CalculateAndEmitBucketSamples(tag, bucket, valueSelector)) {
                    yield return calcVal;
                }
            }
        }


        /// <summary>
        /// Emits samples calculated from the specified bucket.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition for the samples.
        /// </param>
        /// <param name="bucket">
        ///   The bucket.
        /// </param>
        /// <param name="valueSelector">
        ///   A delegate that will select the samples from the <paramref name="bucket"/> to return. 
        ///   If <see langword="null"/>, <see cref="DefaultPlotValueSelector"/> will be used.
        /// </param>
        /// <returns>
        ///   An <see cref="IEnumerable{T}"/> that contains the samples.
        /// </returns>
        /// <remarks>
        ///   Assumes that the <paramref name="bucket"/> contains at least one sample.
        /// </remarks>
        private static IEnumerable<TagValueQueryResult> CalculateAndEmitBucketSamples(
            TagSummary tag, 
            TagValueBucket bucket, 
            PlotValueSelector? valueSelector
        ) {
            TagValueQueryResult CreateSample(PlotValue value) {
                var builder = new TagValueBuilder(value.Sample)
                    .WithBucketProperties(bucket);

                if (value.Criteria != null) {
                    builder.WithProperty(string.Intern(CommonTagValuePropertyNames.Criteria), string.Join(", ", value.Criteria));
                }

                builder.WithProperties(AggregationHelper.CreateXPoweredByProperty());

                return TagValueQueryResult.Create(
                    tag.Id,
                    tag.Name,
                    builder.Build()
                );
            }

            TagValueQueryResult? CalculateStartBoundarySample() {
                TagValueExtended? startVal = null;

                if (bucket.BeforeStartBoundary.BestQualityValue != null && bucket.AfterStartBoundary.BestQualityValue != null) {
                    // We have samples before and after the start time boundary.
                    startVal = InterpolationHelper.GetInterpolatedValueAtSampleTime(tag, bucket.UtcQueryStart, new[] { bucket.BeforeStartBoundary.BestQualityValue, bucket.AfterStartBoundary.BestQualityValue });

                }
                else if (bucket.RawSampleCount >= 2) {
                    // We have at least 2 samples in the bucket; we can extrapolate a sample from
                    // these.
                    startVal = InterpolationHelper.GetInterpolatedValueAtSampleTime(tag, bucket.UtcQueryStart, bucket.RawSamples);
                }

                if (startVal != null) {
                    return CreateSample(new PlotValue(startVal, "start-boundary"));
                }

                return null;
            }

            TagValueQueryResult? CalculateEndBoundarySample() {
                TagValueExtended? endVal = null;

                if (bucket.BeforeEndBoundary.BestQualityValue != null && bucket.AfterEndBoundary.BestQualityValue != null) {
                    // We have samples before and after the end time boundary.
                    endVal = InterpolationHelper.GetInterpolatedValueAtSampleTime(tag, bucket.UtcQueryEnd, new[] { bucket.BeforeEndBoundary.BestQualityValue, bucket.AfterEndBoundary.BestQualityValue });

                }
                else if (bucket.RawSampleCount >= 2) {
                    // We have at least 2 samples in the bucket; we can extrapolate a sample from
                    // these.
                    endVal = InterpolationHelper.GetInterpolatedValueAtSampleTime(tag, bucket.UtcQueryEnd, bucket.RawSamples);
                }

                if (endVal != null) {
                    return CreateSample(new PlotValue(endVal, "end-boundary"));
                }

                return null;
            }

            var significantValues = valueSelector == null
                ? DefaultPlotValueSelector(tag, bucket)
                : valueSelector.Invoke(tag, bucket);

            var startBoundaryValueRequired = bucket.UtcBucketStart == bucket.UtcQueryStart;
            var endBoundaryValueRequired = bucket.UtcBucketEnd >= bucket.UtcQueryEnd;

            foreach (var value in significantValues) {
                if (startBoundaryValueRequired) {
                    startBoundaryValueRequired = false;

                    if (value.Sample.UtcSampleTime > bucket.UtcQueryStart) {
                        // The first sample selected is later than the query start time, so we
                        // will return an interpolated boundary sample if we can.

                        var startVal = CalculateStartBoundarySample();
                        if (startVal != null) {
                            yield return startVal;
                        }
                    }
                }
                if (endBoundaryValueRequired && value.Sample.UtcSampleTime == bucket.UtcQueryEnd) {
                    // We've selected a sample exactly at the query end time, so we don't need to
                    // interpolated a final sample.
                    endBoundaryValueRequired = false;
                }

                yield return CreateSample(value);
            }

            if (startBoundaryValueRequired) {
                var startVal = CalculateStartBoundarySample();
                if (startVal != null) {
                    yield return startVal;
                }
            }

            if (endBoundaryValueRequired) {
                var endVal = CalculateEndBoundarySample();
                if (endVal != null) {
                    yield return endVal;
                }
            }
        }


        /// <summary>
        /// Default delegate for selecting the samples to return from a <see cref="TagValueBucket"/> 
        /// in a plot query.
        /// </summary>
        /// <param name="tag">
        ///   The tag that the plot values are being selected for.
        /// </param>
        /// <param name="bucket">
        ///   The <see cref="TagValueBucket"/>.
        /// </param>
        /// <returns>
        ///   The selected samples.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   For numeric tags (i.e. tags where <see cref="VariantExtensions.IsNumericType(VariantType)"/> 
        ///   is <see langword="true"/> for <see cref="TagSummary.DataType"/> on <paramref name="tag"/>),
        ///   up to 6 values are selected from the bucket:
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>The first sample</item>
        ///   <item>The last sample</item>
        ///   <item>The midpoint sample (i.e. the sample with the timestamp closest to the middle of the bucket's time range)</item>
        ///   <item>The sample with the maximum numeric value and good quality</item>
        ///   <item>The sample with the minimum numeric value and good quality</item>
        ///   <item>The first sample with non-good quality</item>
        /// </list>
        /// 
        /// <para>
        ///   For non-numeric tags, up to 6 values are selected from the bucket using the 
        ///   following conditions:
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>The first sample</item>
        ///   <item>The last sample</item>
        ///   <item>Any sample that represents a change in value or quality from the previous sample</item>
        ///   <item>The sample immediately before any sample that represents a change in value or quality</item>
        /// </list>
        /// 
        /// <para>
        ///   Note that if a non-numeric sample is changing value or quality multiple times in a 
        ///   bucket, the <see cref="DefaultPlotValueSelector"/> method may not return all of these 
        ///   changes due to the limit on the maximum number of samples that can be selected from 
        ///   a single bucket.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="PlotValueSelector"/>
        public static IEnumerable<PlotValue> DefaultPlotValueSelector(TagSummary tag, TagValueBucket bucket) {
            if (bucket == null || bucket.RawSampleCount == 0) {
                return Array.Empty<PlotValue>();
            }

            // The raw samples that can be selected.
            IEnumerable<TagValueExtended> samples;
            // The latest allowed timestamp that can be selected from the bucket.
            DateTime latestAllowedSampleTime;

            if (bucket.UtcBucketStart >= bucket.UtcQueryStart && bucket.UtcBucketEnd <= bucket.UtcQueryEnd) {
                samples = bucket.RawSamples;
                latestAllowedSampleTime = bucket.UtcBucketEnd;
            }
            else {
                var arr = bucket.RawSamples.Where(x => x.UtcSampleTime >= bucket.UtcQueryStart && x.UtcSampleTime <= bucket.UtcQueryEnd).ToArray();
                if (arr.Length == 0) {
                    return Array.Empty<PlotValue>();
                }
                samples = arr;
                latestAllowedSampleTime = bucket.UtcQueryEnd;
            }

            if (tag.DataType.IsNumericType()) {
                // The tag is numeric, so we can select a representative number of samples.

                var selectedValues = new ConcurrentDictionary<TagValueExtended, List<string>>();

                // First value
                selectedValues.GetOrAdd(samples.First(), _ => new List<string>()).Add("first");

                // Last value
                selectedValues.GetOrAdd(samples.Last(), _ => new List<string>()).Add("last");

                // For the midpoint value we need to find the sample with the timestamp that is
                // closest to the midpoint of the bucket.
                var midpointTime = bucket.UtcBucketStart.AddSeconds((latestAllowedSampleTime - bucket.UtcBucketStart).TotalSeconds / 2);
                var midpointDiffs = samples.Where(x => x.Status == TagValueStatus.Good).Select(x => new {
                    Sample = x,
                    MidpointDiff = Math.Abs((midpointTime - x.UtcSampleTime).TotalSeconds)
                }).ToArray();

                if (midpointDiffs.Length > 0) {
                    // Midpoint value
                    selectedValues.GetOrAdd(midpointDiffs.Aggregate((a, b) => a.MidpointDiff <= b.MidpointDiff ? a : b).Sample, _ => new List<string>()).Add("midpoint");
                }

                // For maximum/minimum values we need to aggregate based on the numeric values of
                // the samples. We will do this by converting the numeric value of each sample to
                // double. If the sample doesn't have a numeric value (e.g. it is text when it is
                // expected to be int) we will treat it as if it was double.NaN.
                var numericValues = samples.Where(x => x.Status == TagValueStatus.Good).Select(x => new {
                    Sample = x,
                    NumericValue = x.GetValueOrDefault(double.NaN)
                }).ToArray();

                if (numericValues.Length > 0) {
                    // Maximum value
                    selectedValues.GetOrAdd(numericValues.Aggregate((a, b) => a.NumericValue >= b.NumericValue ? a : b).Sample, _ => new List<string>()).Add("max");

                    // Minimum value
                    selectedValues.GetOrAdd(numericValues.Aggregate((a, b) => a.NumericValue <= b.NumericValue ? a : b).Sample, _ => new List<string>()).Add("min");
                }

                // First non-good value.
                var exceptionValue = samples.FirstOrDefault(x => x.Status != TagValueStatus.Good);
                if (exceptionValue != null) {
                    selectedValues.GetOrAdd(exceptionValue, _ = new List<string>()).Add("non-good");
                }

                return selectedValues.OrderBy(x => x.Key.UtcSampleTime).Select(x => new PlotValue(x.Key, x.Value));
            }
            else {
                // The tag is not numeric, so we have to add text value change or quality status
                // changes in the bucket. We will return a maximum of 6 samples so that we return
                // only a representative number of samples.
                const int MaxNonNumericSamples = 6;

                var currentState = bucket.BeforeStartBoundary.ClosestValue?.GetValueOrDefault<string>();
                var currentQuality = bucket.BeforeStartBoundary.ClosestValue?.Status;
                TagValueExtended? previousValue = null;

                var changesInitial = new HashSet<TagValueExtended>() { 
                    samples.First()
                };

                var changes = samples.Aggregate(changesInitial, (list, item) => {
                    var val = item.GetValueOrDefault<string>();
                    if (currentState == null ||
                        !string.Equals(currentState, val, StringComparison.Ordinal) ||
                        currentQuality != item.Status) {

                        if (list.Count < MaxNonNumericSamples && previousValue != null) {
                            list.Add(previousValue);
                        }
                        if (list.Count < MaxNonNumericSamples) {
                            list.Add(item);
                        }
                        currentState = val;
                        currentQuality = item.Status;
                    }

                    previousValue = item;
                    return list;
                });

                if (changes.Count < MaxNonNumericSamples) {
                    changes.Add(samples.Last());
                }

                return changes.OrderBy(x => x.UtcSampleTime).Select(x => new PlotValue(x));
            }
        }

    }
}
