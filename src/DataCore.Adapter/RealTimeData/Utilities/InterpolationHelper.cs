using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Utility class for creating a data set containing interpolated data from a set of raw tag values.
    /// </summary>
    public static class InterpolationHelper {

        /// <summary>
        /// Interpolates a value between two numeric points.
        /// </summary>
        /// <param name="x">
        ///   The X-axis position to calculate the value at.
        /// </param>
        /// <param name="x0">
        ///   The first X-axis position to use in the interpolation.
        /// </param>
        /// <param name="x1">
        ///   The second X-axis position to use in the interpolation.
        /// </param>
        /// <param name="y0">
        ///   The first Y-axis value to use in the interpolation.
        /// </param>
        /// <param name="y1">
        ///   The second Y-axis value to use in the interpolation.
        /// </param>
        /// <returns>
        ///   The interpolated value.
        /// </returns>
        public static double InterpolateValue(double x, double x0, double x1, double y0, double y1) {
            if (double.IsNaN(x) ||
                double.IsNaN(x0) ||
                double.IsNaN(x1) ||
                double.IsNaN(y0) ||
                double.IsNaN(y1) ||
                double.IsInfinity(x) ||
                double.IsInfinity(x0) ||
                double.IsInfinity(x1) ||
                double.IsInfinity(y0) ||
                double.IsInfinity(y1)
            ) {
                return double.NaN;
            }

            return y0 + (x - x0) * ((y1 - y0) / (x1 - x0));
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
        /// <param name="forceUncertainStatus">
        ///   When <see langword="true"/>, the resulting value will have <see cref="StatusCodes.Uncertain"/> 
        ///   status, even if <paramref name="valueBefore"/> and <paramref name="valueAfter"/> 
        ///   have <see cref="StatusCodes.Good"/> status.
        /// </param>
        /// <returns>
        ///   The interpolated sample.
        /// </returns>
        private static TagValueExtended? InterpolateSample(
            DateTime utcSampleTime, 
            TagValueExtended? valueBefore, 
            TagValueExtended? valueAfter,
            bool forceUncertainStatus
        ) {
            // If either value is not numeric, we'll just return the earlier value with the requested 
            // sample time. This is to allow "interpolation" of state-based values.

            var y0 = valueBefore?.GetValueOrDefault(double.NaN) ?? double.NaN;
            var y1 = valueAfter?.GetValueOrDefault(double.NaN) ?? double.NaN;

            if ( 
                double.IsNaN(y0) || 
                double.IsNaN(y1) || 
                double.IsInfinity(y0) || 
                double.IsInfinity(y1)
            ) {
                return valueBefore == null 
                    ? null 
                    : new TagValueBuilder(valueBefore)
                        .WithUtcSampleTime(utcSampleTime)
                        .WithStatus(
                            valueBefore.Status.IsGood() && !forceUncertainStatus 
                                ? StatusCodes.Good 
                                : StatusCodes.Uncertain,
                            TagValueInfoBits.Interpolated
                        )
                        .WithProperties(AggregationHelper.CreateXPoweredByProperty())
                        .Build();
            }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var x0 = valueBefore.UtcSampleTime;
            var x1 = valueAfter.UtcSampleTime;

            var nextNumericValue = InterpolateValue(utcSampleTime.Ticks, x0.Ticks, x1.Ticks, y0, y1);
            var nextStatusValue = valueBefore.Status.IsGood() && valueAfter.Status.IsGood() && !forceUncertainStatus
                ? StatusCodes.Good
                : StatusCodes.Uncertain;

            return new TagValueBuilder()
                .WithUtcSampleTime(utcSampleTime)
                .WithValue(nextNumericValue)
                .WithStatus(nextStatusValue, TagValueInfoBits.Interpolated)
                .WithUnits(valueBefore.Units)
                .WithNotes($"Interpolated using samples @ {valueBefore.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z and {valueAfter.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z.")
                .WithProperties(AggregationHelper.CreateXPoweredByProperty())
                .Build();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

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
        /// <param name="forceUncertainStatus">
        ///   When <see langword="true"/>, the resulting value will have <see cref="StatusCodes.Uncertain"/> 
        ///   status, even if <paramref name="valueBefore"/> and <paramref name="valueAfter"/> 
        ///   have <see cref="StatusCodes.Good"/> status.
        /// </param>
        /// <returns>
        ///   The calculated <see cref="TagValueExtended"/>, or <see langword="null"/> if a value cannot be 
        ///   calculated.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        private static TagValueExtended? GetValueAtTime(
            TagSummary tag, 
            DateTime utcSampleTime, 
            TagValueExtended? valueBefore, 
            TagValueExtended? valueAfter, 
            InterpolationCalculationType interpolationType,
            bool forceUncertainStatus = false
        ) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (valueBefore == null && valueAfter == null) {
                return null;
            }

            if (interpolationType == InterpolationCalculationType.UsePreviousValue || !tag.DataType.IsNumericType()) {
                // We've been asked to repeat the previous value, or this is not a numeric tag, so 
                // we can't interpolate between two values.

                if (valueBefore != null && valueBefore.UtcSampleTime <= utcSampleTime) {
                    var status = forceUncertainStatus 
                        ? (StatusCode) StatusCodes.Uncertain 
                        : valueBefore.Status;

                    return new TagValueBuilder(valueBefore)
                        .WithUtcSampleTime(utcSampleTime)
                        .WithStatus(status, TagValueInfoBits.Interpolated)
                        .WithProperties(AggregationHelper.CreateXPoweredByProperty())
                        .Build();
                }
                if (valueAfter != null && valueAfter.UtcSampleTime <= utcSampleTime) {
                    var status = forceUncertainStatus
                        ? (StatusCode) StatusCodes.Uncertain
                        : valueAfter.Status;

                    return new TagValueBuilder(valueAfter)
                        .WithUtcSampleTime(utcSampleTime)
                        .WithStatus(status, TagValueInfoBits.Interpolated)
                        .WithProperties(AggregationHelper.CreateXPoweredByProperty())
                        .Build();
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
                valueBefore = valueAfter;
                valueAfter = tmp;
            }

            return InterpolateSample(utcSampleTime, valueBefore, valueAfter, forceUncertainStatus);
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
        /// <param name="forceUncertainStatus">
        ///   When <see langword="true"/>, the resulting value will have <see cref="StatusCodes.Uncertain"/> 
        ///   status, even if <paramref name="valueBefore"/> has <see cref="StatusCodes.Good"/> 
        ///   status.
        /// </param>
        /// <returns>
        ///   The calculated <see cref="TagValueExtended"/>, or <see langword="null"/> if a value cannot be 
        ///   calculated.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueExtended? GetPreviousValueAtSampleTime(
            TagSummary tag,
            DateTime utcSampleTime,
            TagValueExtended valueBefore,
            bool forceUncertainStatus = false
        ) {
            return GetValueAtTime(
                tag,
                utcSampleTime,
                valueBefore,
                null,
                InterpolationCalculationType.UsePreviousValue,
                forceUncertainStatus
            );
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
        /// <param name="forceUncertainStatus">
        ///   When <see langword="true"/>, the resulting value will have <see cref="StatusCodes.Uncertain"/> 
        ///   status, even if <paramref name="valueBefore"/> and <paramref name="valueAfter"/> 
        ///   have <see cref="StatusCodes.Good"/> status.
        /// </param>
        /// <returns>
        ///   The calculated <see cref="TagValueExtended"/>, or <see langword="null"/> if a value cannot be 
        ///   calculated.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueExtended? GetInterpolatedValueAtSampleTime(
            TagSummary tag,
            DateTime utcSampleTime,
            TagValueExtended valueBefore,
            TagValueExtended valueAfter,
            bool forceUncertainStatus = false
        ) {
            return GetValueAtTime(
                tag, 
                utcSampleTime, 
                valueBefore, 
                valueAfter, 
                InterpolationCalculationType.Interpolate, 
                forceUncertainStatus
            );
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
        /// <param name="values">
        ///   The raw samples that the samples for interpolation will be selected from.
        /// </param>
        /// <returns>
        ///   The calculated <see cref="TagValueExtended"/>, or <see langword="null"/> if a value cannot be 
        ///   calculated.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueExtended? GetInterpolatedValueAtSampleTime(
            TagSummary tag,
            DateTime utcSampleTime,
            IEnumerable<TagValueExtended> values
        ) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (values == null || !values.Any()) {
                return null;
            }

            // Option 1: if we have a value exactly at the sample time, use that value.

            var exactValue = values.FirstOrDefault(x => x != null && x.UtcSampleTime == utcSampleTime);
            if (exactValue != null) {
                return new TagValueBuilder(exactValue)
                    .WithProperties(AggregationHelper.CreateXPoweredByProperty())
                    .Build();
            }

            // Option 2: if we have boundary values around the sample time, use those values.

            var boundaryStartClosest = values.LastOrDefault(x => x != null && x.UtcSampleTime < utcSampleTime);
            var boundaryStartBest = boundaryStartClosest == null || boundaryStartClosest.Status.IsGood()
                ? boundaryStartClosest
                : values.LastOrDefault(x => x != null && x.UtcSampleTime < utcSampleTime && x.Status.IsGood()) ?? boundaryStartClosest;

            var boundaryEndClosest = values.FirstOrDefault(x => x != null && x.UtcSampleTime > utcSampleTime);
            var boundaryEndBest = boundaryEndClosest == null || boundaryEndClosest.Status.IsGood()
                ? boundaryEndClosest
                : values.FirstOrDefault(x => x != null && x.UtcSampleTime > utcSampleTime && x.Status.IsGood()) ?? boundaryEndClosest;

            if (boundaryStartBest != null && boundaryEndBest != null) {
                // We have a boundary value before and after the sample time.
                return GetValueAtTime(
                    tag,
                    utcSampleTime,
                    boundaryStartBest,
                    boundaryEndBest,
                    InterpolationCalculationType.Interpolate,
                    boundaryStartBest != boundaryStartClosest || boundaryEndBest != boundaryEndClosest
                        ? true
                        : false
                );
            }

            // Option 3: if we have two good-quality values before the sample time, extrapolate 
            // using those values.

            var boundaryValues = values
                .Where(x => x != null)
                .Where(x => x.Status.IsGood())
                .Where(x => x.UtcSampleTime < utcSampleTime)
                .Reverse() // Take values closest to the sample time
                .Take(2)
                .Reverse() // Switch values back into ascending time order
                .ToArray();

            if (boundaryValues.Length == 2) {
                return GetValueAtTime(
                    tag,
                    utcSampleTime,
                    boundaryValues[0],
                    boundaryValues[1],
                    InterpolationCalculationType.Interpolate,
                    // Use uncertain status because we are extrapolating instead of interpolating.
                    true
                );
            }

            // Option 4: if we have two good-quality values after the sample time, extrapolated 
            // using those values.

            boundaryValues = values
                .Where(x => x != null)
                .Where(x => x.Status.IsGood())
                .Where(x => x.UtcSampleTime > utcSampleTime)
                .Take(2)
                .ToArray();

            if (boundaryValues.Length == 2) {
                return GetValueAtTime(
                    tag,
                    utcSampleTime,
                    boundaryValues[0],
                    boundaryValues[1],
                    InterpolationCalculationType.Interpolate,
                    // Use uncertain status because we are extrapolating instead of interpolating.
                    true
                );
            }

            // Option 5: if we have any values on either side of the sample time, interpolate 
            // using those values.

            boundaryValues = new[] {
                values.LastOrDefault(x => x != null && x.UtcSampleTime < utcSampleTime),
                values.FirstOrDefault(x => x != null && x.UtcSampleTime > utcSampleTime)
            }.Where(x => x != null).ToArray();

            if (boundaryValues.Length == 2) {
                return GetValueAtTime(
                    tag,
                    utcSampleTime,
                    boundaryValues[0],
                    boundaryValues[1],
                    InterpolationCalculationType.Interpolate,
                    // Use uncertain status because we are extrapolating instead of interpolating. 
                    // This isn't technically required because the calculated status is guaranteed 
                    // to be uncertain since we are not using two good quality values, 
                    // but we will do so anyway for correctness.
                    true
                );
            }

            // Option 6: if we have two values before the sample time, extrapolate using those 
            // values.

            boundaryValues = values
                .Where(x => x != null)
                .Where(x => x.UtcSampleTime < utcSampleTime)
                .Reverse() // Take values closest to the sample time
                .Take(2)
                .Reverse() // Switch values back into ascending time order
                .ToArray();

            if (boundaryValues.Length == 2) {
                return GetValueAtTime(
                    tag,
                    utcSampleTime,
                    boundaryValues[0],
                    boundaryValues[1],
                    InterpolationCalculationType.Interpolate,
                    // Use uncertain status because we are extrapolating instead of interpolating. 
                    // This isn't technically required because the calculated status is guaranteed 
                    // to be uncertain since we are not using two good quality values, 
                    // but we will do so anyway for correctness.
                    true
                );
            }

            // Option 7: if we have two values after the sample time, extrapolate using those 
            // values.

            boundaryValues = values
                .Where(x => x != null)
                .Where(x => x.UtcSampleTime > utcSampleTime)
                .Take(2)
                .ToArray();

            if (boundaryValues.Length == 2) {
                return GetValueAtTime(
                    tag,
                    utcSampleTime,
                    boundaryValues[0],
                    boundaryValues[1],
                    InterpolationCalculationType.Interpolate,
                    // Use uncertain status because we are extrapolating instead of interpolating. 
                    // This isn't technically required because the calculated status is guaranteed 
                    // to be uncertain since we are not using two good quality values, 
                    // but we will do so anyway for correctness.
                    true
                );
            }

            return null;
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
        ///   An <see cref="IAsyncEnumerable{T}"/> that will provide the raw data for the calculations.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will emit the calculated tag values.
        /// </returns>
        public static IAsyncEnumerable<TagValueQueryResult> GetPreviousValuesAtSampleTimes(
            TagSummary tag, 
            IEnumerable<DateTime> utcSampleTimes, 
            IAsyncEnumerable<TagValueQueryResult> rawData, 
            CancellationToken cancellationToken = default
        ) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (utcSampleTimes == null) {
                throw new ArgumentNullException(nameof(utcSampleTimes));
            }
            if (rawData == null) {
                throw new ArgumentNullException(nameof(rawData));
            }

            return GetPreviousValuesAtTimes(tag, utcSampleTimes.OrderBy(x => x), rawData, cancellationToken);
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
        ///   An <see cref="IAsyncEnumerable{T}"/> that will provide the raw data for the calculations.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the results.
        /// </returns>
        private static async IAsyncEnumerable<TagValueQueryResult> GetPreviousValuesAtTimes(
            TagSummary tag, 
            IEnumerable<DateTime> utcSampleTimes, 
            IAsyncEnumerable<TagValueQueryResult> rawData,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var sampleTimesEnumerator = utcSampleTimes.GetEnumerator();
            try {
                if (!sampleTimesEnumerator.MoveNext()) {
                    yield break;
                }

                var nextSampleTime = sampleTimesEnumerator.Current;
                TagValueExtended value1 = null!;
                TagValueExtended value0 = null!;
                var sampleTimesRemaining = true;

                await foreach (var val in rawData.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                    if (val == null || !sampleTimesRemaining) {
                        continue;
                    }

                    value0 = value1;
                    value1 = val.Value;

                    if (value0 != null) {
                        while (value1.UtcSampleTime > nextSampleTime && sampleTimesRemaining) {
                            var interpolatedValue = GetValueAtTime(tag, nextSampleTime, value0, value1, InterpolationCalculationType.UsePreviousValue);
                            if (sampleTimesEnumerator.MoveNext()) {
                                nextSampleTime = sampleTimesEnumerator.Current;
                            }
                            else {
                                sampleTimesRemaining = false;
                            }

                            if (interpolatedValue == null) {
                                continue;
                            }

                            yield return TagValueQueryResult.Create(tag.Id, tag.Name, interpolatedValue);
                        }
                    }
                }

                // If the last interpolated point we calculated has a timestamp earlier than the requested 
                // end time (e.g. if the end time was later than the last raw sample), we'll calculate an 
                // additional point for the utcEndTime, based on the two most-recent raw values we processed.  
                if (!cancellationToken.IsCancellationRequested &&
                    sampleTimesRemaining &&
                    value0 != null &&
                    value1 != null) {

                    var interpolatedValue = GetValueAtTime(tag, nextSampleTime, value0, value1, InterpolationCalculationType.UsePreviousValue);
                    
                    if (interpolatedValue != null) {
                        yield return TagValueQueryResult.Create(tag.Id, tag.Name, interpolatedValue);
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
