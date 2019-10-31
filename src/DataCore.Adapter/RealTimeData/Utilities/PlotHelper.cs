using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;

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
        ///   A channel that will provide the raw data to use in the calculations.
        /// </param>
        /// <param name="scheduler">
        ///   The background task service to use when writing values into the channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will emit a set of trend-friendly samples.
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
        public static ChannelReader<TagValueQueryResult> GetPlotValues(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan bucketSize, ChannelReader<TagValueQueryResult> rawData, IBackgroundTaskService scheduler = null, CancellationToken cancellationToken = default) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException(SharedResources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, nameof(utcStartTime));
            }
            if (bucketSize <= TimeSpan.Zero) {
                throw new ArgumentException(Resources.Error_BucketSizeMustBeGreaterThanZero, nameof(bucketSize));
            }

            var result = Channel.CreateBounded<TagValueQueryResult>(new BoundedChannelOptions(500) {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true
            });

            result.Writer.RunBackgroundOperation(
                (ch, ct) => GetPlotValues(
                    tag,
                    utcStartTime,
                    utcEndTime,
                    bucketSize,
                    rawData,
                    ch,
                    ct
                ),
                true,
                scheduler,
                cancellationToken
            );

            return result;
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
        ///   A channel that will provide the raw data to use in the calculations.
        /// </param>
        /// <param name="scheduler">
        ///   The background task service to use when writing values into the channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will emit a set of trend-friendly samples.
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
        public static ChannelReader<TagValueQueryResult> GetPlotValues(IEnumerable<TagDefinition> tags, DateTime utcStartTime, DateTime utcEndTime, TimeSpan bucketSize, ChannelReader<TagValueQueryResult> rawData, IBackgroundTaskService scheduler = null, CancellationToken cancellationToken = default) {
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException(SharedResources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, nameof(utcStartTime));
            }
            if (bucketSize <= TimeSpan.Zero) {
                throw new ArgumentException(Resources.Error_BucketSizeMustBeGreaterThanZero, nameof(bucketSize));
            }

            Channel<TagValueQueryResult> result;

            if (!tags.Any()) {
                // No tags; complete the channel and return.
                result = Channel.CreateUnbounded<TagValueQueryResult>();
                result.Writer.TryComplete();
                return result;
            }

            if (tags.Count() == 1) {
                // Single tag; use the optimised single-tag overload.
                return GetPlotValues(
                    tags.First(), 
                    utcStartTime, 
                    utcEndTime, 
                    bucketSize, 
                    rawData, 
                    scheduler, 
                    cancellationToken
                );
            }

            // Multiple tags; create a single result channel, and create individual input channels 
            // for each tag in the request and redirect each value emitted from the raw data channel 
            // into the appropriate per-tag input channel.

            result = Channel.CreateBounded<TagValueQueryResult>(new BoundedChannelOptions(500) {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = false
            });

            var tagLookupById = tags.ToDictionary(x => x.Id);

            var tagRawDataChannels = tags.ToDictionary(x => x.Id, x => Channel.CreateUnbounded<TagValueQueryResult>(new UnboundedChannelOptions() {
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true
            }));

            // Redirect values from input channel to per-tag channel.
            rawData.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!ch.TryRead(out var val) || val == null) {
                            continue;
                        }

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
            }, scheduler, cancellationToken);

            // Execute stream for each tag in the query and write all values into the shared 
            // result channel.
            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await Task.WhenAll(
                    tagRawDataChannels.Select(x => GetPlotValues(
                        tagLookupById[x.Key],
                        utcStartTime,
                        utcEndTime,
                        bucketSize,
                        x.Value,
                        ch,
                        ct
                    ))    
                ).WithCancellation(ct).ConfigureAwait(false);
            }, true, scheduler, cancellationToken);

            return result;
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
        /// <param name="resultChannel">
        ///   A channel that the computed values will be written to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will compute the values.
        /// </returns>
        private static async Task GetPlotValues(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan bucketSize, ChannelReader<TagValueQueryResult> rawData, ChannelWriter<TagValueQueryResult> resultChannel, CancellationToken cancellationToken) {
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

            var bucket = new TagValueBucket() {
                UtcStart = utcStartTime,
                UtcEnd = utcStartTime.Add(bucketSize)
            };

            TagValueExtended lastValuePreviousBucket = null;

            while (await rawData.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!rawData.TryRead(out var val)) {
                    break;
                }
                if (val == null) {
                    continue;
                }

                if (val.Value.UtcSampleTime < utcStartTime) {
                    continue;
                }

                if (val.Value.UtcSampleTime > utcEndTime) {
                    continue;
                }

                if (val.Value.UtcSampleTime >= bucket.UtcEnd) {
                    if (bucket.RawSamples.Count > 0) {
                        await CalculateAndEmitBucketSamples(tag, bucket, lastValuePreviousBucket, resultChannel, cancellationToken).ConfigureAwait(false);
                        lastValuePreviousBucket = bucket.RawSamples.Last();
                    }

                    bucket = new TagValueBucket() {
                        UtcStart = bucket.UtcEnd,
                        UtcEnd = bucket.UtcStart.Add(bucketSize)
                    };
                }

                bucket.RawSamples.Add(val.Value);
            }

            if (bucket.RawSamples.Count > 0) {
                await CalculateAndEmitBucketSamples(tag, bucket, lastValuePreviousBucket, resultChannel, cancellationToken).ConfigureAwait(false);
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
        /// <param name="resultsChannel">
        ///   The channel to write the calculated values to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will calculate and emit the samples.
        /// </returns>
        /// <remarks>
        ///   Assumes that the <paramref name="bucket"/> contains at least one sample.
        /// </remarks>
        private static async Task CalculateAndEmitBucketSamples(TagDefinition tag, TagValueBucket bucket, TagValueExtended lastValuePreviousBucket, ChannelWriter<TagValueQueryResult> resultsChannel, CancellationToken cancellationToken) {
            var significantValues = new HashSet<TagValueExtended>();

            if (tag.DataType == TagDataType.Numeric) {
                var numericValues = bucket.RawSamples.ToDictionary(x => x, x => x.Value.GetValueOrDefault(double.NaN));

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
                var currentState = lastValuePreviousBucket?.Value.GetValueOrDefault<string>();
                var currentQuality = lastValuePreviousBucket?.Status;

                foreach (var item in bucket.RawSamples) {
                    var tVal = item.Value.GetValueOrDefault<string>();
                    if (currentState != null && 
                        string.Equals(currentState, tVal, StringComparison.Ordinal) && 
                        currentQuality == item.Status) {
                        continue;
                    }
                    currentState = tVal;
                    currentQuality = item.Status;
                    significantValues.Add(item);
                }
            }

            var exceptionValue = bucket.RawSamples.FirstOrDefault(x => x.Status != TagValueStatus.Good);
            if (exceptionValue != null) {
                significantValues.Add(exceptionValue);
            }

            foreach (var value in significantValues.OrderBy(x => x.UtcSampleTime)) {
                if (!await resultsChannel.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                    break;
                }
                resultsChannel.TryWrite(TagValueQueryResult.Create(tag.Id, tag.Name, value));
            }
        }

    }
}
