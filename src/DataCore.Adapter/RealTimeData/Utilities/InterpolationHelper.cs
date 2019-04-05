using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Utility class for creating a data set containing interpolated data from a set of raw tag values.
    /// </summary>
    public static class InterpolationHelper {

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
            if (double.IsNaN(valueBefore.NumericValue) || double.IsNaN(valueAfter.NumericValue) || double.IsInfinity(valueBefore.NumericValue) || double.IsInfinity(valueAfter.NumericValue)) {
                return TagValue.CreateFromExisting(valueBefore)
                    .WithUtcSampleTime(utcSampleTime)
                    .Build();
            }

            var x0 = valueBefore.UtcSampleTime.Ticks;
            var x1 = valueAfter.UtcSampleTime.Ticks;

            var y0 = valueBefore.NumericValue;
            var y1 = valueAfter.NumericValue;

            var nextNumericValue = y0 + (utcSampleTime.Ticks - x0) * ((y1 - y0) / (x1 - x0));
            var nextTextValue = TagValue.GetTextValue(nextNumericValue);
            var nextStatusValue = new[] { valueBefore, valueAfter }.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return TagValue.Create()
                .WithUtcSampleTime(utcSampleTime)
                .WithNumericValue(nextNumericValue)
                .WithTextValue(nextTextValue)
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
        ///   The calculation type to use when calculating the new value.
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
                throw new ArgumentException(Resources.Error_InterpolationRequiresAtLeastOneSample);
            }

            if (valueBefore == null) {
                return utcSampleTime < valueAfter.UtcSampleTime
                    ? throw new ArgumentException(Resources.Error_InterpolationRequiresAtLeastOneSampleEarlierThanRequestedSampleTime, nameof(valueAfter))
                    : TagValue.CreateFromExisting(valueAfter).WithUtcSampleTime(utcSampleTime).Build();
            }

            if (valueAfter == null) {
                return utcSampleTime < valueBefore.UtcSampleTime
                    ? throw new ArgumentException(Resources.Error_InterpolationRequiresAtLeastOneSampleEarlierThanRequestedSampleTime, nameof(valueBefore))
                    : TagValue.CreateFromExisting(valueBefore).WithUtcSampleTime(utcSampleTime).Build();
            }

            if (utcSampleTime < valueBefore.UtcSampleTime && utcSampleTime < valueAfter.UtcSampleTime) {
                throw new ArgumentException(Resources.Error_InterpolationRequiresAtLeastOneSampleEarlierThanRequestedSampleTime);
            }

            if (valueBefore.UtcSampleTime > valueAfter.UtcSampleTime) {
                var tmp = valueBefore;
                valueAfter = valueBefore;
                valueBefore = tmp;
            }

            return interpolationType == InterpolationCalculationType.Interpolate
                ? tag.DataType == TagDataType.Numeric
                    ? InterpolateSample(utcSampleTime, valueBefore, valueAfter)
                    : TagValue.CreateFromExisting(valueBefore).WithUtcSampleTime(utcSampleTime).Build()
                : TagValue.CreateFromExisting(valueBefore).WithUtcSampleTime(utcSampleTime).Build();
        }


        public static TagValue GetValueAtTime(TagDefinition tag, DateTime utcSampleTime, IEnumerable<TagValue> rawValues, InterpolationCalculationType interpolationType) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            if (rawValues == null || !rawValues.Any()) {
                return null;
            }

            var valueBefore = rawValues.LastOrDefault(x => x.UtcSampleTime <= utcSampleTime);
            var valueAfter = rawValues.FirstOrDefault(x => x.UtcSampleTime >= utcSampleTime);

            if (valueBefore == null && valueAfter == null) {
                return null;
            }

            return GetValueAtTime(tag, utcSampleTime, valueBefore, valueAfter, interpolationType);
        }


        /// <summary>
        /// Creates interpolated data from the specified raw values using the provided interpolation 
        /// type.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="utcStartTime">
        ///   The start time for the interpolated data set.
        /// </param>
        /// <param name="utcEndTime">
        ///   The end time for the interpolated data set.
        /// </param>
        /// <param name="sampleInterval">
        ///   The sample interval to use for interpolation.
        /// </param>
        /// <param name="interpolationType">
        ///   The type of interpolation to use.
        /// </param>
        /// <param name="rawData">
        ///   The raw data to use in the interpolation calculations. You should include the raw sample 
        ///   before or at <paramref name="utcStartTime"/>, and the raw sample at or after 
        ///   <paramref name="utcEndTime"/> in this set (if available), to ensure that samples at 
        ///   <paramref name="utcStartTime"/> and <paramref name="utcEndTime"/> can be calculated.
        /// </param>
        /// <returns>
        ///   A set of interpolated samples.
        /// </returns>
        private static IEnumerable<TagValue> GetInterpolatedValuesInternal(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, InterpolationCalculationType interpolationType, IEnumerable<TagValue> rawData) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException(Resources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, nameof(utcStartTime));
            }
            if (sampleInterval <= TimeSpan.Zero) {
                throw new ArgumentException(Resources.Error_SampleIntervalMustBeGreaterThanZero, nameof(sampleInterval));
            }

            var rawSamples = rawData?.Where(x => x != null).ToArray() ?? new TagValue[0];
            if (rawSamples.Length == 0) {
                return new TagValue[0];
            }

            // Set the initial list capacity based on the time range and sample interval.
            var capacity = (int) ((utcEndTime - utcStartTime).TotalMilliseconds / sampleInterval.TotalMilliseconds);
            var result = capacity > 0
                ? new List<TagValue>(capacity)
                : new List<TagValue>();

            var nextSampleTime = utcStartTime;
            var firstSample = rawSamples[0];

            // If the next time stamp that we have to calculate a value for is less than the first raw 
            // sample we were given, keep incrementing the time stamp by sampleInterval until we will 
            // be calculating a time stamp that is inside the boundaries of rawSamples. 
            while (nextSampleTime < firstSample.UtcSampleTime) {
                nextSampleTime = nextSampleTime.Add(sampleInterval);
            }

            // We need to keep track of the previous raw sample at all times, so that we can interpolate 
            // values between the previous raw sample and the current one.
            TagValue previousSample = null;
            // This will hold the raw sample that occurred before previousSample.  It will be used at 
            // the end if we still need to interpolate a value at utcEndTime.
            TagValue previousPreviousSample = null;

            var sampleEnumerator = rawSamples.AsEnumerable().GetEnumerator();
            while (sampleEnumerator.MoveNext()) {
                var currentSample = sampleEnumerator.Current;

                // If we have moved past utcEndTime in our raw data set, interpolate all of our 
                // remaining values until utcEndTime and then break from the loop.
                if (currentSample.UtcSampleTime > utcEndTime) {
                    while (nextSampleTime <= utcEndTime) {
                        previousPreviousSample = previousSample;
                        previousSample = GetValueAtTime(tag, nextSampleTime, previousSample, currentSample, interpolationType);
                        result.Add(previousSample);
                        nextSampleTime = nextSampleTime.Add(sampleInterval);
                    }
                    break;
                }

                // If the current sample time is less than the next sample time that we have to interpolate 
                // at, we'll make a note of the current raw sample and move on to the next one.
                if (currentSample.UtcSampleTime < nextSampleTime) {
                    previousPreviousSample = previousSample;
                    previousSample = currentSample;
                    continue;
                }

                // If the current sample exactly matches the next sample time we have to interpolate at, 
                // or if previousSample has not been previously set, we'll add the current raw sample 
                // to our output unmodified.  previousSample can only be null here if currentSample is 
                // the first raw sample we were given, and it also has a time stamp that is greater than 
                // the utcStartTime that was passed into the method.
                if (currentSample.UtcSampleTime == nextSampleTime || previousSample == null) {
                    previousPreviousSample = previousSample;
                    previousSample = currentSample;
                    result.Add(currentSample);
                    nextSampleTime = nextSampleTime.Add(sampleInterval);
                    continue;
                }

                // If we've moved past the sample time for our next interpolated value, calculate the 
                // interpolated value for the next required time, update the next sample time, and 
                // repeat this process until the time stamp for the next interpolated value is greater 
                // than the time stamp for currentSample.
                //
                // This allows us to handle situations where we need to produce an interpolated value 
                // at a set interval, but there is a gap in raw data that is bigger than the required 
                // interval (e.g. if we are interpolating over a 5 minute interval, but there is a gap 
                // of 30 minutes between raw samples).
                while (currentSample.UtcSampleTime >= nextSampleTime) {
                    // Calculate interpolated point.
                    previousPreviousSample = previousSample;
                    previousSample = GetValueAtTime(tag, nextSampleTime, previousSample, currentSample, interpolationType);
                    result.Add(previousSample);

                    nextSampleTime = nextSampleTime.Add(sampleInterval);
                }
            }

            // If the last interpolated point we calculated has a time stamp earlier than the requested 
            // end time (e.g. if the end time was later than the last raw sample), or if we have not 
            // calculated any values yet, we'll calculate an additional point for the utcEndTime, 
            // based on the two most-recent raw values we processed.  
            if ((previousSample != null && previousPreviousSample != null) && (result.Count == 0 || (result.Count > 0 && result.Last().UtcSampleTime < utcEndTime))) {
                result.Add(GetValueAtTime(tag, utcEndTime, previousPreviousSample, previousSample, InterpolationCalculationType.UsePreviousValue));
            }

            return result;
        }


        /// <summary>
        /// Creates interpolated data from the specified raw values.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="utcStartTime">
        ///   The start time for the interpolated data set.
        /// </param>
        /// <param name="utcEndTime">
        ///   The end time for the interpolated data set.
        /// </param>
        /// <param name="sampleInterval">
        ///   The sample interval to use for interpolation.
        /// </param>
        /// <param name="interpolationType">
        ///   The type of calculation to perform when calculating values. Specify 
        ///   <see cref="InterpolationCalculationType.UsePreviousValue"/> for non-numeric or state-based 
        ///   tag values and <see cref="InterpolationCalculationType.Interpolate"/> for numeric tag 
        ///   values.
        /// </param>
        /// <param name="rawData">
        ///   The raw data to use in the interpolation calculations. You should include the raw sample 
        ///   before or at <paramref name="utcStartTime"/>, and the raw sample at or after 
        ///   <paramref name="utcEndTime"/> in this set (if available), to ensure that samples at 
        ///   <paramref name="utcStartTime"/> and <paramref name="utcEndTime"/> can be calculated.
        /// </param>
        /// <returns>
        ///   A set of interpolated samples.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="utcStartTime"/> is greater than or equal to <paramref name="utcEndTime"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="sampleInterval"/> is less than or equal to <see cref="TimeSpan.Zero"/>.
        /// </exception>
        public static IEnumerable<TagValue> GetInterpolatedValues(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, InterpolationCalculationType interpolationType, IEnumerable<TagValue> rawData) {
            if (rawData == null || !rawData.Any()) {
                return new TagValue[0];
            }
            return GetInterpolatedValuesInternal(tag, utcStartTime, utcEndTime, sampleInterval, interpolationType, rawData);
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
