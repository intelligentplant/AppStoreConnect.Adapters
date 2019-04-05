using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Utility class for creating a visualization-friendly (plot) data set from a set of raw tag 
    /// values.
    /// </summary>
    public static class PlotHelper {

        internal static TimeSpan CalculateBucketSize(DateTime utcStartTime, DateTime utcEndTime, int intervals) {
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException(Resources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, nameof(utcStartTime));
            }
            if (intervals < 1) {
                throw new ArgumentException(Resources.Error_IntervalCountMustBeGreaterThanZero, nameof(intervals));
            }

            return TimeSpan.FromMilliseconds((utcEndTime - utcStartTime).TotalMilliseconds / intervals); ;
        }


        /// <summary>
        /// Creates a plot-friendly data set suitable for trending.
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
        /// <param name="intervals">
        ///   The number of buckets to use when calculating the plot data set.
        /// </param>
        /// <param name="rawData">
        ///   The raw data to use in the calculations.
        /// </param>
        /// <returns>
        ///   A set of trend-friendly samples.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="utcStartTime"/> is greater than or equal to <paramref name="utcEndTime"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="intervals"/> is less than one.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   The plot function works by collecting raw values into buckets. Each bucket covers the 
        ///   same period of time. The method iterates over <paramref name="rawData"/> and adds samples 
        ///   to the bucket, until it encounters a sample that has a time stamp that is after the 
        ///   bucket's end time. The function then takes the earliest, latest, minimum and maximum 
        ///   values in the bucket, as well as the first non-good value in the bucket, and adds them 
        ///   to the result data set.
        /// </para>
        /// 
        /// <para>
        ///   It is important then to note that <see cref="GetPlotValues"/> is not guaranteed to give 
        ///   evenly-spaced time stamps in the resulting data set, but instead returns a data set that 
        ///   gives a reasonable approximation of the tag when visualized.
        /// </para>
        /// 
        /// </remarks>
        public static IEnumerable<TagValue> GetPlotValues(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, int intervals, IEnumerable<TagValue> rawData) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            var bucketSize = CalculateBucketSize(utcStartTime, utcEndTime, intervals);

            var rawSamples = rawData?.Where(x => x != null).ToArray() ?? new TagValue[0];
            if (rawSamples.Length == 0) {
                return new TagValue[0];
            }

            // Set the initial list capacity based on the time range and sample interval.
            var result = new List<TagValue>((int) ((utcEndTime - utcStartTime).TotalMilliseconds / bucketSize.TotalMilliseconds));

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

            // If the initial bucket covers a period of time that starts before the raw data set that 
            // we have been given, move the start time of the bucket forward to match the first raw 
            // sample.

            var firstSample = rawSamples[0];

            if (bucket.UtcStart < firstSample.UtcSampleTime) {
                bucket.UtcStart = firstSample.UtcSampleTime;
                // Make sure that the end time of the bucket is at least equal to the start time of the bucket.
                if (bucket.UtcEnd < bucket.UtcStart) {
                    bucket.UtcEnd = bucket.UtcStart;
                }
            }

            var sampleEnumerator = rawSamples.AsEnumerable().GetEnumerator();
            // The raw sample that was processed before the current sample.  Used when we need to interpolate 
            // a value at utcStartTime or utcEndTime.
            TagValue previousSample = null;
            // The raw sample that was processed before previousSample.  Used when we need to interpolate a 
            // value at utcEndTime.
            TagValue previousPreviousSample = null;
            // An interpolated value calculated at utcStartTime.  Included in the final result when a raw 
            // sample does not exactly fall at utcStartTime.
            TagValue interpolatedStartValue = null;
            // The last value in the previous bucket.
            TagValue lastValuePreviousBucket = null;


            while (sampleEnumerator.MoveNext()) {
                var currentSample = sampleEnumerator.Current;

                // If we've moved past the requested end time, break from the loop.
                if (currentSample.UtcSampleTime > utcEndTime) {
                    break;
                }

                // If the current sample lies before the bucket start time, make a note of the sample 
                // for use in interpolation calculations and move on to the next sample.  This can 
                // occur when utcStartTime is greater than the start time of the raw data set.
                if (currentSample.UtcSampleTime < bucket.UtcStart) {
                    previousPreviousSample = previousSample;
                    previousSample = currentSample;
                    continue;
                }

                // If utcStartTime lies between the previous sample and the current sample, we'll interpolate a value at utcStartTime.
                if (interpolatedStartValue == null &&
                    previousSample != null &&
                    currentSample.UtcSampleTime > utcStartTime &&
                    previousSample.UtcSampleTime < utcStartTime) {
                    interpolatedStartValue = InterpolationHelper.GetValueAtTime(tag, utcStartTime, previousSample, currentSample, InterpolationCalculationType.Interpolate);
                }

                previousPreviousSample = previousSample;
                previousSample = currentSample;

                // If we've moved past the end of the bucket, identify the values to use for the bucket, 
                // move to the next bucket, and repeat this process until the end time for the bucket 
                // is greater than the time stamp for currentSample.
                //
                // This allows us to handle situations where there is a gap in raw data that is bigger 
                // than our bucket size (e.g. if our bucket size is 20 minutes, but there is a gap of 
                // 30 minutes between raw samples).
                while (currentSample.UtcSampleTime > bucket.UtcEnd) {
                    if (bucket.Samples.Count > 0) {
                        var significantValues = new HashSet<TagValue>();

                        if (bucket.Samples.Any(x => !Double.IsNaN(x.NumericValue))) {
                            // If any of the samples are numeric, assume that we can aggregate.
                            significantValues.Add(bucket.Samples.First());
                            significantValues.Add(bucket.Samples.Last());
                            significantValues.Add(bucket.Samples.Aggregate((a, b) => a.NumericValue <= b.NumericValue ? a : b)); // min
                            significantValues.Add(bucket.Samples.Aggregate((a, b) => a.NumericValue >= b.NumericValue ? a : b)); // max

                            var exceptionValue = bucket.Samples.FirstOrDefault(x => x.Status != TagValueStatus.Good);
                            if (exceptionValue != null) {
                                significantValues.Add(exceptionValue);
                            }

                        }
                        else {
                            // We don't have any numeric values, so we have to add each value change 
                            // in the bucket.
                            var currentState = lastValuePreviousBucket;
                            foreach (var item in bucket.Samples) {
                                if (currentState != null && String.Equals(currentState.TextValue, item.TextValue) && currentState.Status == item.Status) {
                                    continue;
                                }
                                currentState = item;
                                significantValues.Add(item);
                            }
                        }

                        foreach (var item in significantValues.OrderBy(x => x.UtcSampleTime)) {
                            result.Add(item);
                        }

                        lastValuePreviousBucket = bucket.Samples.Last();
                        bucket.Samples.Clear();
                    }

                    bucket.UtcStart = bucket.UtcEnd;
                    bucket.UtcEnd = bucket.UtcStart.Add(bucketSize);
                }

                bucket.Samples.Add(currentSample);
            }

            // We've moved past utcEndTime in the raw data.  If we still have any values in the bucket, 
            // identify the significant values and add them to the result.
            if (bucket.Samples.Count > 0) {
                var significantValues = new HashSet<TagValue>();

                if (bucket.Samples.Any(x => !Double.IsNaN(x.NumericValue))) {
                    // If any of the samples are numeric, assume that we can aggregate.

                    significantValues.Add(bucket.Samples.First());
                    significantValues.Add(bucket.Samples.Last());
                    significantValues.Add(bucket.Samples.Aggregate((a, b) => a.NumericValue <= b.NumericValue ? a : b)); // min
                    significantValues.Add(bucket.Samples.Aggregate((a, b) => a.NumericValue >= b.NumericValue ? a : b)); // max

                    var exceptionValue = bucket.Samples.FirstOrDefault(x => x.Status != TagValueStatus.Good);
                    if (exceptionValue != null) {
                        significantValues.Add(exceptionValue);
                    }
                }
                else {
                    // We don't have any numeric values, so we have to add each text value change 
                    // or quality status change in the bucket.
                    var currentState = lastValuePreviousBucket;
                    foreach (var item in bucket.Samples) {
                        if (currentState != null && String.Equals(currentState.TextValue, item.TextValue) && currentState.Status == item.Status) {
                            continue;
                        }
                        currentState = item;
                        significantValues.Add(item);
                    }
                }

                foreach (var item in significantValues.OrderBy(x => x.UtcSampleTime)) {
                    result.Add(item);
                }
            }

            if (result.Count == 0) {
                // Add interpolated values at utcStartTime and utcEndTime, if possible.
                if (interpolatedStartValue != null) {
                    result.Add(interpolatedStartValue);
                    // Only attempt to add a value at utcEndTime if we also have one at utcStartTime.  Otherwise, 
                    // we will be interpolating based on two values that lie before utcStartTime.
                    if (previousSample != null && previousPreviousSample != null) {
                        result.Add(InterpolationHelper.GetValueAtTime(tag, utcEndTime, previousPreviousSample, previousSample, InterpolationCalculationType.Interpolate));
                    }
                }
            }
            else {
                // Add the interpolated value at utcStartTime if the first value in the result set 
                // has a time stamp greater than utcStartTime.
                if (interpolatedStartValue != null && result.First().UtcSampleTime > utcStartTime) {
                    result.Insert(0, interpolatedStartValue);
                }

                // If the last value in the result set has a time stamp less than utcEndTime, re-add 
                // the last value at utcEndTime.
                if (previousSample != null && previousPreviousSample != null && result.Last().UtcSampleTime < utcEndTime) {
                    result.Add(InterpolationHelper.GetValueAtTime(tag, utcEndTime, previousSample, null, InterpolationCalculationType.UsePreviousValue));
                }
            }

            return result;

        }
    }
}
