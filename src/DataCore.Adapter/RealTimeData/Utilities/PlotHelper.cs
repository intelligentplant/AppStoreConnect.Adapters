using System;
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
        public static async IAsyncEnumerable<TagValueQueryResult> GetPlotValues(
            TagSummary tag, 
            DateTime utcStartTime, 
            DateTime utcEndTime, 
            TimeSpan bucketSize, 
            IAsyncEnumerable<TagValueQueryResult> rawData,
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

            await foreach (var item in GetPlotValuesInternal(tag, utcStartTime, utcEndTime, bucketSize, rawData, cancellationToken).ConfigureAwait(false)) {
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will emit the computed values.
        /// </returns>
        private static async IAsyncEnumerable<TagValueQueryResult> GetPlotValuesInternal(TagSummary tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan bucketSize, IAsyncEnumerable<TagValueQueryResult> rawData, [EnumeratorCancellation] CancellationToken cancellationToken) {
            // We will determine the values to return for the plot request by creating aggregation 
            // buckets that cover a time range that is equal to the bucketSize. For each bucket, we 
            // will we add up to 5 raw samples into the resulting data set:
            //
            // * The earliest value in the bucket.
            // * The latest value in the bucket.
            // * The maximum value in the bucket.
            // * The minimum value in the bucket.
            // * The first non-good-status value in the bucket.
            //
            // If a sample meets more than one of the above conditions, it will only be added to the 
            // result once.

            var bucket = new TagValueBucket(utcStartTime, utcStartTime.Add(bucketSize), utcStartTime, utcEndTime);

            TagValueExtended lastValuePreviousBucket = null!;

            await foreach (var val in rawData.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                if (val == null) {
                    continue;
                }

                if (val.Value.UtcSampleTime < utcStartTime) {
                    continue;
                }

                if (val.Value.UtcSampleTime > utcEndTime) {
                    continue;
                }

                if (val.Value.UtcSampleTime >= bucket.UtcBucketEnd) {
                    if (bucket.RawSampleCount > 0) {
                        foreach (var calculatedValue in CalculateAndEmitBucketSamples(tag, bucket, lastValuePreviousBucket)) {
                            yield return calculatedValue;
                        }
                        lastValuePreviousBucket = bucket.RawSamples.Last();
                    }

                    do {
                        bucket = new TagValueBucket(bucket.UtcBucketEnd, bucket.UtcBucketEnd.Add(bucketSize), utcStartTime, utcEndTime);
                    } while (bucket.UtcBucketEnd < val.Value.UtcSampleTime);
                }

                bucket.AddRawSample(val.Value);
            }

            if (bucket.RawSampleCount > 0) { 
                foreach (var calculatedValue in CalculateAndEmitBucketSamples(tag, bucket, lastValuePreviousBucket)) {
                    yield return calculatedValue;
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
        /// <param name="lastValuePreviousBucket">
        ///   The last value that was added to the previous bucket for the same tag.
        /// </param>
        /// <returns>
        ///   An <see cref="IEnumerable{T}"/> that contains the samples.
        /// </returns>
        /// <remarks>
        ///   Assumes that the <paramref name="bucket"/> contains at least one sample.
        /// </remarks>
        private static IEnumerable<TagValueQueryResult> CalculateAndEmitBucketSamples(TagSummary tag, TagValueBucket bucket, TagValueExtended lastValuePreviousBucket) {
            var significantValues = new HashSet<TagValueExtended>();

            if (tag.DataType.IsNumericType()) {
                var numericValues = bucket.RawSamples.ToDictionary(x => x, x => x.GetValueOrDefault(double.NaN));

                significantValues.Add(bucket.RawSamples.First());
                significantValues.Add(bucket.RawSamples.Last());
                significantValues.Add(bucket.RawSamples.Aggregate((a, b) => {
                    var nValA = numericValues[a];
                    var nValB = numericValues[b];
                    return nValA <= nValB
                        ? a
                        : b;
                })); // min
                significantValues.Add(bucket.RawSamples.Aggregate((a, b) => {
                    var nValA = numericValues[a];
                    var nValB = numericValues[b];
                    return nValA >= nValB
                        ? a
                        : b;
                })); // max
            }
            else {
                // The tag is not numeric, so we have to add each text value change or quality status 
                // change in the bucket.
                var currentState = lastValuePreviousBucket?.GetValueOrDefault<string>();
                var currentQuality = lastValuePreviousBucket?.StatusCode;

                foreach (var item in bucket.RawSamples) {
                    var tVal = item.GetValueOrDefault<string>();
                    if (currentState != null && 
                        string.Equals(currentState, tVal, StringComparison.Ordinal) && 
                        currentQuality == item.StatusCode
                    ) {
                        continue;
                    }
                    currentState = tVal;
                    currentQuality = item.StatusCode;
                    significantValues.Add(item);
                }
            }

            var exceptionValue = bucket.RawSamples.FirstOrDefault(x => !x.StatusCode.IsGood());
            if (exceptionValue != null) {
                significantValues.Add(exceptionValue);
            }

            foreach (var value in significantValues.OrderBy(x => x.UtcSampleTime)) {
                yield return TagValueQueryResult.Create(
                    tag.Id, 
                    tag.Name, 
                    new TagValueBuilder(value)
                        .WithStatus(value.StatusCode, bucket.InfoBits)
                        .WithBucketProperties(bucket)
                        .WithProperties(AggregationHelper.CreateXPoweredByProperty())
                        .Build()
                );
            }
        }

    }
}
