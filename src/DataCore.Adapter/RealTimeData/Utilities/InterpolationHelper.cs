using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Utility class for creating a data set containing interpolated data from a set of raw tag values.
    /// </summary>
    public static class InterpolationHelper {

        /// <summary>
        /// Interpolates a value between two numeric points.
        /// </summary>
        /// <param name="x">
        ///   The sample time to calculate the value at.
        /// </param>
        /// <param name="x0">
        ///   The first sample time to use in the interpolation.
        /// </param>
        /// <param name="x1">
        ///   The second sample time to use in the interpolation.
        /// </param>
        /// <param name="y0">
        ///   The first numeric value to use in the interpolation.
        /// </param>
        /// <param name="y1">
        ///   The second numeric value to use in the interpolation.
        /// </param>
        /// <returns>
        ///   The interpolated value.
        /// </returns>
        public static double InterpolateValue(DateTime x, DateTime x0, DateTime x1, double y0, double y1) {
            if (double.IsNaN(y0) ||
                double.IsNaN(y1) ||
                double.IsInfinity(y0) ||
                double.IsInfinity(y1)) {

                return double.NaN;
            }
            
            var x0Ticks = x0.Ticks;
            var x1Ticks = x1.Ticks;

            return y0 + (x.Ticks - x0Ticks) * ((y1 - y0) / (x1Ticks - x0Ticks));
        }


        /// <summary>
        /// Interpolates a value between two numeric samples.
        /// </summary>
        /// <param name="utcSampleTime">
        ///   The time stamp for the interpolated sample.
        /// </param>
        /// <param name="valueBefore">
        ///   The closest raw sample before <paramref name="utcSampleTime"/>.
        /// </param>
        /// <param name="valueAfter">
        ///   The closest raw sample after <paramref name="utcSampleTime"/>.
        /// </param>
        /// <returns>
        ///   The interpolated sample.
        /// </returns>
        private static TagValue InterpolateSample(DateTime utcSampleTime, TagValue valueBefore, TagValue valueAfter) {
            // If either value is not numeric, we'll just return the earlier value with the requested 
            // sample time. This is to allow "interpolation" of state-based values.

            var y0 = valueBefore?.Value.GetValueOrDefault(double.NaN) ?? double.NaN;
            var y1 = valueAfter?.Value.GetValueOrDefault(double.NaN) ?? double.NaN;

            if ( 
                double.IsNaN(y0) || 
                double.IsNaN(y1) || 
                double.IsInfinity(y0) || 
                double.IsInfinity(y1)) {
                
                return valueBefore == null 
                    ? null 
                    : TagValueBuilder.CreateFromExisting(valueBefore)
                        .WithUtcSampleTime(utcSampleTime)
                        .Build();
            }

            var x0 = valueBefore.UtcSampleTime;
            var x1 = valueAfter.UtcSampleTime;

            var nextNumericValue = InterpolateValue(utcSampleTime, x0, x1, y0, y1);
            var nextStatusValue = new[] { valueBefore, valueAfter }.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return TagValueBuilder.Create()
                .WithUtcSampleTime(utcSampleTime)
                .WithValue(nextNumericValue)
                .WithStatus(nextStatusValue)
                .WithUnits(valueBefore.Units)
                .WithNotes($"Interpolated using samples @ {valueBefore.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z and {valueAfter.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z.")
                .Build();
        }


        /// <summary>
        /// Calculates a tag value at the specified time stamp.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="utcSampleTime">
        ///   The UTC sample time for the calculated sample.
        /// </param>
        /// <param name="valueBefore">
        ///   The raw sample immediately before <paramref name="utcSampleTime"/>.
        /// </param>
        /// <param name="valueAfter">
        ///   The raw sample immediately after <paramref name="utcSampleTime"/>.
        /// </param>
        /// <param name="interpolationType">
        ///   The type of calculation type to perform when calculating the value. Specify 
        ///   <see cref="InterpolationCalculationType.UsePreviousValue"/> for non-numeric or state-based 
        ///   tags and <see cref="InterpolationCalculationType.Interpolate"/> for numeric tags.
        /// </param>
        /// <returns>
        ///   The calculated <see cref="TagValue"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="valueBefore"/> and <paramref name="valueAfter"/> are both 
        ///   <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="utcSampleTime"/> is earlier than both <paramref name="valueBefore"/> and 
        ///   <paramref name="valueAfter"/>.
        /// </exception>
        public static TagValue GetValueAtTime(TagDefinition tag, DateTime utcSampleTime, TagValue valueBefore, TagValue valueAfter, InterpolationCalculationType interpolationType) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (valueBefore == null && valueAfter == null) {
                throw new ArgumentException(SharedResources.Error_InterpolationRequiresAtLeastOneSample);
            }

            if (valueBefore == null) {
                return utcSampleTime < valueAfter.UtcSampleTime
                    ? throw new ArgumentException(SharedResources.Error_InterpolationRequiresAtLeastOneSampleEarlierThanRequestedSampleTime, nameof(valueAfter))
                    : TagValueBuilder.CreateFromExisting(valueAfter).WithUtcSampleTime(utcSampleTime).Build();
            }

            if (valueAfter == null) {
                return utcSampleTime < valueBefore.UtcSampleTime
                    ? throw new ArgumentException(SharedResources.Error_InterpolationRequiresAtLeastOneSampleEarlierThanRequestedSampleTime, nameof(valueBefore))
                    : TagValueBuilder.CreateFromExisting(valueBefore).WithUtcSampleTime(utcSampleTime).Build();
            }

            //if (utcSampleTime < valueBefore.UtcSampleTime && utcSampleTime < valueAfter.UtcSampleTime) {
            //    throw new ArgumentException(SharedResources.Error_InterpolationRequiresAtLeastOneSampleEarlierThanRequestedSampleTime);
            //}

            if (valueBefore.UtcSampleTime > valueAfter.UtcSampleTime) {
                var tmp = valueBefore;
                valueAfter = valueBefore;
                valueBefore = tmp;
            }

            return interpolationType == InterpolationCalculationType.Interpolate
                ? tag.DataType == TagDataType.Numeric
                    ? InterpolateSample(utcSampleTime, valueBefore, valueAfter)
                    : TagValueBuilder.CreateFromExisting(valueBefore).WithUtcSampleTime(utcSampleTime).Build()
                : TagValueBuilder.CreateFromExisting(valueBefore).WithUtcSampleTime(utcSampleTime).Build();
        }


        /// <summary>
        /// Performs interpolation on raw tag values.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <param name="utcStartTime">
        ///   The start time for the data query.
        /// </param>
        /// <param name="utcEndTime">
        ///   The end time for the data query.
        /// </param>
        /// <param name="sampleInterval">
        ///   The sample interval for the data query.
        /// </param>
        /// <param name="rawData">
        ///   The channel that will provide the raw data for the interpolation calculations.
        /// </param>
        /// <param name="scheduler">
        ///   The background task service to use when writing values into the channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will emit the calculated tag values.
        /// </returns>
        public static ChannelReader<TagValueQueryResult> GetInterpolatedValues(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, ChannelReader<TagValueQueryResult> rawData, IBackgroundTaskService scheduler = null, CancellationToken cancellationToken = default) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException(SharedResources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, nameof(utcStartTime));
            }
            if (sampleInterval <= TimeSpan.Zero) {
                throw new ArgumentException(SharedResources.Error_SampleIntervalMustBeGreaterThanZero, nameof(sampleInterval));
            }

            var result = Channel.CreateBounded<TagValueQueryResult>(new BoundedChannelOptions(500) {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true
            });

            result.Writer.RunBackgroundOperation(
                (ch, ct) => GetInterpolatedValues(
                    tag, 
                    GetSampleTimes(utcStartTime, utcEndTime, sampleInterval), 
                    InterpolationCalculationType.Interpolate, 
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
        /// Performs interpolation on raw tag values.
        /// </summary>
        /// <param name="tags">
        ///   The tags in the query.
        /// </param>
        /// <param name="utcStartTime">
        ///   The start time for the data query.
        /// </param>
        /// <param name="utcEndTime">
        ///   The end time for the data query.
        /// </param>
        /// <param name="sampleInterval">
        ///   The sample interval for the data query.
        /// </param>
        /// <param name="rawData">
        ///   The channel that will provide the raw data for the interpolation calculations.
        /// </param>
        /// <param name="scheduler">
        ///   The background task service to use when writing values into the channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will emit the calculated tag values.
        /// </returns>
        public static ChannelReader<TagValueQueryResult> GetInterpolatedValues(IEnumerable<TagDefinition> tags, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, ChannelReader<TagValueQueryResult> rawData, IBackgroundTaskService scheduler = null, CancellationToken cancellationToken = default) {
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException(SharedResources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, nameof(utcStartTime));
            }
            if (sampleInterval <= TimeSpan.Zero) {
                throw new ArgumentException(SharedResources.Error_SampleIntervalMustBeGreaterThanZero, nameof(sampleInterval));
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
                return GetInterpolatedValues(
                    tags.First(),
                    utcStartTime,
                    utcEndTime,
                    sampleInterval,
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
                    tagRawDataChannels.Select(x => GetInterpolatedValues(
                        tagLookupById[x.Key],
                        GetSampleTimes(utcStartTime, utcEndTime, sampleInterval),
                        InterpolationCalculationType.Interpolate,
                        x.Value,
                        ch,
                        ct
                    ))
                ).WithCancellation(ct).ConfigureAwait(false);
            }, true, scheduler, cancellationToken);

            return result;
        }


        /// <summary>
        /// Gets tag values at the specified sample times.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <param name="utcSampleTimes">
        ///   The sample times.
        /// </param>
        /// <param name="rawData">
        ///   A channel that will provide the raw data for the calculations.
        /// </param>
        /// <param name="scheduler">
        ///   The background task service to use when writing values into the channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will emit the calculated tag values.
        /// </returns>
        public static ChannelReader<TagValueQueryResult> GetValuesAtSampleTimes(TagDefinition tag, IEnumerable<DateTime> utcSampleTimes, ChannelReader<TagValueQueryResult> rawData, IBackgroundTaskService scheduler, CancellationToken cancellationToken = default) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (utcSampleTimes == null) {
                throw new ArgumentNullException(nameof(utcSampleTimes));
            }

            var result = Channel.CreateBounded<TagValueQueryResult>(new BoundedChannelOptions(500) {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true
            });

            result.Writer.RunBackgroundOperation((ch, ct) => GetInterpolatedValues(tag, utcSampleTimes.OrderBy(x => x), InterpolationCalculationType.UsePreviousValue, rawData, ch, ct), true, scheduler, cancellationToken);

            return result;
        }


        internal static IEnumerable<DateTime> GetSampleTimes(DateTime utcStart, DateTime utcEnd, TimeSpan sampleInterval) {
            var currentSampleTime = utcStart;

            while (currentSampleTime < utcEnd) {
                yield return currentSampleTime;
                currentSampleTime = currentSampleTime.Add(sampleInterval);
            }

            yield return utcEnd;
        }


        private static async Task GetInterpolatedValues(TagDefinition tag, IEnumerable<DateTime> utcSampleTimes, InterpolationCalculationType interpolationCalculationType, ChannelReader<TagValueQueryResult> rawData, ChannelWriter<TagValueQueryResult> resultChannel, CancellationToken cancellationToken) {
            var sampleTimesEnumerator = utcSampleTimes.GetEnumerator();
            try {
                if (!sampleTimesEnumerator.MoveNext()) {
                    return;
                }

                var nextSampleTime = sampleTimesEnumerator.Current;
                TagValue value1 = null;
                TagValue value0 = null;
                var sampleTimesRemaining = true;

                while (await rawData.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!rawData.TryRead(out var val)) {
                        break;
                    }
                    if (val == null || !sampleTimesRemaining) {
                        continue;
                    }

                    value0 = value1;
                    value1 = val.Value;

                    if (value0 != null) {
                        while (value1.UtcSampleTime > nextSampleTime && sampleTimesRemaining) {
                            var interpolatedValue = GetValueAtTime(tag, nextSampleTime, value0, value1, interpolationCalculationType);
                            if (sampleTimesEnumerator.MoveNext()) {
                                nextSampleTime = sampleTimesEnumerator.Current;
                            }
                            else {
                                sampleTimesRemaining = false;
                            }

                            if (await resultChannel.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                                resultChannel.TryWrite(TagValueQueryResult.Create(tag.Id, tag.Name, interpolatedValue));
                            }
                        }
                    }
                }

                // If the last interpolated point we calculated has a time stamp earlier than the requested 
                // end time (e.g. if the end time was later than the last raw sample), we'll calculate an 
                // additional point for the utcEndTime, based on the two most-recent raw values we processed.  
                if (!cancellationToken.IsCancellationRequested &&
                    sampleTimesRemaining &&
                    value0 != null &&
                    value1 != null) {

                    var interpolatedValue = GetValueAtTime(tag, nextSampleTime, value0, value1, interpolationCalculationType);
                    if (await resultChannel.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                        resultChannel.TryWrite(TagValueQueryResult.Create(tag.Id, tag.Name, interpolatedValue));
                    }
                }
            }
            finally {
                sampleTimesEnumerator.Dispose();
            }
        }

    }


    /// <summary>
    /// Defines the type of calculation to perform when interpolating tag values.
    /// </summary>
    public enum InterpolationCalculationType {

        /// <summary>
        /// The new value should be interpolated using the samples immediately before and immediately 
        /// after the timestamp for the new value. Recommended for numeric tag values.
        /// </summary>
        Interpolate,

        /// <summary>
        /// The new value should repeat the value from the raw sample immediately before the timestamp 
        /// for the new value. Recommended for non-numeric and state-based tag values.
        /// </summary>
        UsePreviousValue

    }
}
