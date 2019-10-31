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
        private static TagValueExtended InterpolateSample(DateTime utcSampleTime, TagValueExtended valueBefore, TagValueExtended valueAfter) {
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
        ///   The calculated <see cref="TagValueExtended"/>, or <see langword="null"/> if a value cannot be 
        ///   calculated.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueExtended GetValueAtTime(TagDefinition tag, DateTime utcSampleTime, TagValueExtended valueBefore, TagValueExtended valueAfter, InterpolationCalculationType interpolationType) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (valueBefore == null && valueAfter == null) {
                return null;
            }

            if (interpolationType == InterpolationCalculationType.UsePreviousValue || tag.DataType != TagDataType.Numeric) {
                // We've been asked to repeat the previous value, or this is not a numeric tag, so 
                // we can't interpolate between two values.

                if (valueBefore != null && valueBefore.UtcSampleTime <= utcSampleTime) {
                    TagValueBuilder.CreateFromExisting(valueBefore).WithUtcSampleTime(utcSampleTime).Build();
                }
                if (valueAfter != null && valueAfter.UtcSampleTime <= utcSampleTime) {
                    TagValueBuilder.CreateFromExisting(valueAfter).WithUtcSampleTime(utcSampleTime).Build();
                }
                return null;
            }

            // If either of the provided samples matches the sample time we are interpolating at, 
            // re-use that sample.
            if (valueBefore != null && valueBefore.UtcSampleTime == utcSampleTime) {
                return valueBefore;
            }
            if (valueAfter != null && valueAfter.UtcSampleTime == utcSampleTime) {
                return valueAfter;
            }

            // We need to interpolate. We can only do this if both samples were provided.
            if (valueBefore == null || valueAfter == null) {
                return null;
            }

            if (valueBefore.UtcSampleTime > valueAfter.UtcSampleTime) {
                var tmp = valueBefore;
                valueAfter = valueBefore;
                valueBefore = tmp;
            }

            return InterpolateSample(utcSampleTime, valueBefore, valueAfter);
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


        private static async Task GetInterpolatedValues(TagDefinition tag, IEnumerable<DateTime> utcSampleTimes, InterpolationCalculationType interpolationCalculationType, ChannelReader<TagValueQueryResult> rawData, ChannelWriter<TagValueQueryResult> resultChannel, CancellationToken cancellationToken) {
            var sampleTimesEnumerator = utcSampleTimes.GetEnumerator();
            try {
                if (!sampleTimesEnumerator.MoveNext()) {
                    return;
                }

                var nextSampleTime = sampleTimesEnumerator.Current;
                TagValueExtended value1 = null;
                TagValueExtended value0 = null;
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
