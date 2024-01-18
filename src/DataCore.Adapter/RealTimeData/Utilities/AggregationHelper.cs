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
    /// Calculates the aggregated values for the specified bucket.
    /// </summary>
    /// <param name="tag">
    ///   The tag that the calculation is being performed for.
    /// </param>
    /// <param name="bucket">
    ///   The bucket to calculate values for.
    /// </param>
    /// <returns>
    ///   The calculated values for the bucket.
    /// </returns>
    public delegate IEnumerable<TagValueExtended> AggregateCalculator(TagSummary tag, TagValueBucket bucket);


    /// <summary>
    /// Utility class for performing data aggregation (e.g. if a data source does not natively 
    /// support aggregation).
    /// </summary>
    public class AggregationHelper {

        #region [ Fields ]

        /// <summary>
        /// Property value to use with <see cref="CreateXPoweredByProperty"/>.
        /// </summary>
        private static Lazy<string> s_xPoweredByPropertyValue = new Lazy<string>(() => {
            // Value will be in the format "DataCore.Adapter v1.2.3.4"
            var asm = typeof(AggregationHelper).Assembly;
            var fileVersion = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyFileVersionAttribute>(asm);
            return string.IsNullOrEmpty(fileVersion?.Version)
                ? string.Concat(asm.GetName().Name, " v", asm.GetName().Version)
                : string.Concat(asm.GetName().Name, " v", fileVersion!.Version);
        }, LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// Maps from aggregate ID to the <see cref="AggregateCalculator"/> implementation for 
        /// default aggregation types.
        /// </summary>
        private static readonly Dictionary<string, AggregateCalculator> s_defaultAggregatorMap = new Dictionary<string, AggregateCalculator>(StringComparer.OrdinalIgnoreCase) {
            { DefaultDataFunctions.Average.Id, CalculateAverage },
            { DefaultDataFunctions.Count.Id, CalculateCount },
            { DefaultDataFunctions.Delta.Id, CalculateDelta },
            { DefaultDataFunctions.Interpolate.Id, CalculateInterpolated },
            { DefaultDataFunctions.Maximum.Id, CalculateMaximum },
            { DefaultDataFunctions.Minimum.Id, CalculateMinimum },
            { DefaultDataFunctions.PercentBad.Id, CalculatePercentBad },
            { DefaultDataFunctions.PercentGood.Id, CalculatePercentGood },
            { DefaultDataFunctions.Range.Id, CalculateRange },
            { DefaultDataFunctions.StandardDeviation.Id, CalculateStandardDeviation },
            { DefaultDataFunctions.TimeAverage.Id, CalculateTimeAverage },
            { DefaultDataFunctions.Variance.Id, CalculateVariance }
        };

        /// <summary>
        /// The descriptors for the default supported data functions.
        /// </summary>
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        private static readonly Lazy<IEnumerable<DataFunctionDescriptor>> s_defaultDataFunctions = new Lazy<IEnumerable<DataFunctionDescriptor>>(() => s_defaultAggregatorMap.Keys.Select(x => DefaultDataFunctions.FindById(x)).ToArray(), LazyThreadSafetyMode.PublicationOnly);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

        /// <summary>
        /// Maps from aggregate ID to descriptor for custom aggregates that have been registered.
        /// </summary>
        private readonly ConcurrentDictionary<string, DataFunctionDescriptor> _customAggregates = new ConcurrentDictionary<string, DataFunctionDescriptor>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Maps from aggregate ID to the <see cref="AggregateCalculator"/> implementation for 
        /// custom aggregation types.
        /// </summary>
        private readonly ConcurrentDictionary<string, AggregateCalculator> _customAggregatorMap = new ConcurrentDictionary<string, AggregateCalculator>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region [ Interpolate ]

        /// <summary>
        /// Calculates the interpolated value at the start time of the provided bucket.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="bucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        private static IEnumerable<TagValueExtended> CalculateInterpolated(TagSummary tag, TagValueBucket bucket) {
            if (bucket.UtcBucketEnd < bucket.UtcQueryStart) {
                // Entire bucket is before the query start time and can be skipped.
                yield break;
            }

            // Our data set for interpolation consists of all of the raw samples inside the bucket
            // plus the samples before the start boundary and after the end boundary.
            var combinedInputValues = bucket
                .BeforeStartBoundary.GetBoundarySamples()
                .Concat(bucket.RawSamples)
                .Concat(bucket.AfterEndBoundary.GetBoundarySamples())
                .ToArray();

            TagValueExtended? result;

            if (bucket.UtcQueryStart >= bucket.UtcBucketStart && bucket.UtcQueryStart < bucket.UtcBucketEnd) {
                // Query start time lies inside this bucket; interpolate a value at the query
                // start time.
                result = InterpolationHelper.GetInterpolatedValueAtSampleTime(
                    tag,
                    bucket.UtcQueryStart,
                    combinedInputValues
                );
            }
            else {
                // Interpolate a value at the bucket start time.
                result = InterpolationHelper.GetInterpolatedValueAtSampleTime(
                    tag,
                    bucket.UtcBucketStart,
                    combinedInputValues
                );
            }

            if (result == null) {
                result = CreateErrorTagValue(
                    bucket,
                    bucket.UtcBucketStart,
                    Resources.TagValue_ProcessedValue_NoData
                );
            }

            yield return result;

            // If query end time lies inside the bucket time range, we also interpolate a value at
            // the query end time.
            if (bucket.UtcBucketEnd >= bucket.UtcQueryEnd) {
                result = InterpolationHelper.GetInterpolatedValueAtSampleTime(
                    tag,
                    bucket.UtcQueryEnd,
                    combinedInputValues
                );

                if (result == null) {
                    result = CreateErrorTagValue(
                        bucket,
                        bucket.UtcQueryEnd,
                        Resources.TagValue_ProcessedValue_NoData
                    );
                }

                yield return result;
            }
        }

        #endregion

        #region [ Average ]

        /// <summary>
        /// Calculates the average for the specified samples.
        /// </summary>
        /// <param name="values">
        ///   The samples.
        /// </param>
        /// <returns>
        ///   The average values.
        /// </returns>
        private static double CalculateAverage(IEnumerable<TagValue> values) {
            return values.Average(x => x.GetValueOrDefault(double.NaN));
        }


        /// <summary>
        /// Calculates the average value of the specified raw samples.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="bucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        private static IEnumerable<TagValueExtended> CalculateAverage(TagSummary tag, TagValueBucket bucket) {
            var goodQualitySamples = bucket
                .RawSamples
                .Where(x => x.Status == TagValueStatus.Good)
                .ToArray();

            if (goodQualitySamples.Length == 0) {
                return new[] { 
                    CreateErrorTagValue(bucket, bucket.UtcBucketStart, Resources.TagValue_ProcessedValue_NoGoodData)
                };
            }

            var status = bucket.RawSamples.Any(x => x.Status != TagValueStatus.Good)
                ? TagValueStatus.Uncertain
                : TagValueStatus.Good;

            var numericValue = CalculateAverage(goodQualitySamples);

            return new[] {
                new TagValueBuilder()
                    .WithUtcSampleTime(bucket.UtcBucketStart)
                    .WithValue(numericValue)
                    .WithStatus(status)
                    .WithUnits(tag.Units)
                    .WithBucketProperties(bucket)
                    .WithProperties(CreateXPoweredByProperty())
                    .Build()
            };
        }

        #endregion

        #region [ TimeAverage ]

        /// <summary>
        /// Calculates the time-weighted average value of the specified raw samples.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="bucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        private static IEnumerable<TagValueExtended> CalculateTimeAverage(TagSummary tag, TagValueBucket bucket) {
            var goodQualitySamples = bucket
                .RawSamples
                .Where(x => x.Status == TagValueStatus.Good)
                .Where(x => !double.IsNaN(x.GetValueOrDefault(double.NaN)))
                .ToArray();

            if (goodQualitySamples.Length == 0) {
                return new[] {
                    CreateErrorTagValue(bucket, bucket.UtcBucketStart, Resources.TagValue_ProcessedValue_NoGoodData)
                };
            }

            var status = bucket.RawSamples.Any(x => x.Status != TagValueStatus.Good || double.IsNaN(x.GetValueOrDefault(double.NaN)))
                ? TagValueStatus.Uncertain
                : TagValueStatus.Good;

            var isIncompleteInterval = false;

            var firstSample = goodQualitySamples[0];

            var total = 0d;
            var calculationInterval = bucket.UtcBucketEnd - bucket.UtcBucketStart;

            var previousValue = firstSample.UtcSampleTime == bucket.UtcBucketStart
                ? firstSample
                : bucket.BeforeStartBoundary.BestQualityValue != null
                    ? InterpolationHelper.GetInterpolatedValueAtSampleTime(tag, bucket.UtcBucketStart, new[] { bucket.BeforeStartBoundary.BestQualityValue, firstSample })
                    : null;

            if (previousValue == null || double.IsNaN(previousValue.GetValueOrDefault(double.NaN))) {
                isIncompleteInterval = true;
            }

            foreach (var item in goodQualitySamples) {
                try { 
                    if (previousValue == null) {
                        // We don't have a start boundary value, so the average value can only be
                        // calculated over the portion of the time bucket where we have values. We
                        // will already be setting a property on the final value that specifies
                        // that it is a partial result if we get to here.
                        calculationInterval = calculationInterval.Subtract(item.UtcSampleTime - bucket.UtcBucketStart);
                        continue;
                    }

                    var diff = item.UtcSampleTime - previousValue.UtcSampleTime;
                    if (diff <= TimeSpan.Zero) {
                        continue;
                    }

                    total += (previousValue.GetValueOrDefault<double>() + item.GetValueOrDefault<double>()) / 2 * diff.TotalMilliseconds;
                }
                finally {
                    if (previousValue == null || item.UtcSampleTime > previousValue.UtcSampleTime) {
                        previousValue = item;
                    }
                }
            }

            if (previousValue != null && previousValue.UtcSampleTime < bucket.UtcBucketEnd) {
                // Last sample in the bucket was before the bucket end time, so we need to
                // interpolate an end boundary value and include the area under the
                // line from the last raw sample to the boundary in our total.

                var endBoundarySample = bucket.AfterEndBoundary.BestQualityValue != null && bucket.AfterEndBoundary.BestQualityValue.Status == TagValueStatus.Good
                    ? InterpolationHelper.GetInterpolatedValueAtSampleTime(tag, bucket.UtcBucketEnd, new[] { previousValue, bucket.AfterEndBoundary.BestQualityValue })
                    : null;

                if (endBoundarySample == null || endBoundarySample.Status != TagValueStatus.Good || double.IsNaN(endBoundarySample.GetValueOrDefault(double.NaN))) {
                    // We can't calculate an end boundary sample, or the end boundary is NaN or has
                    // non-good status. We will reduce the calculation period for the average so
                    // that it excludes the bucket time after the last raw sample in the bucket
                    // and set a flag that indicates that this is a partial result.
                    calculationInterval = calculationInterval.Subtract(bucket.UtcBucketEnd - previousValue.UtcSampleTime);
                    isIncompleteInterval = true;
                }
                else {
                    var diff = endBoundarySample.UtcSampleTime - previousValue.UtcSampleTime;
                    total += (previousValue.Value.GetValueOrDefault<double>() + endBoundarySample.Value.GetValueOrDefault<double>()) / 2 * diff.TotalMilliseconds;
                }
            }

            var tavg = calculationInterval <= TimeSpan.Zero 
                ? double.NaN 
                : total / calculationInterval.TotalMilliseconds;

            var builder = new TagValueBuilder()
                .WithUtcSampleTime(bucket.UtcBucketStart)
                .WithValue(tavg)
                .WithStatus(isIncompleteInterval || double.IsNaN(tavg) ? TagValueStatus.Uncertain : status)
                .WithUnits(tag.Units)
                .WithBucketProperties(bucket)
                .WithProperties(CreateXPoweredByProperty());

            if (isIncompleteInterval) {
                builder.WithProperties(CreatePartialProperty());
            }

            return new[] {
                builder.Build()
            };
        }

        #endregion

        #region [ Min ]

        /// <summary>
        /// Calculates the minimum value of the specified raw samples.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="bucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        private static IEnumerable<TagValueExtended> CalculateMinimum(TagSummary tag, TagValueBucket bucket) {
            var goodQualitySamples = bucket
                .RawSamples
                .Where(x => x.Status == TagValueStatus.Good)
                .ToArray();

            if (goodQualitySamples.Length == 0) {
                return new[] {
                    CreateErrorTagValue(bucket, bucket.UtcBucketStart, Resources.TagValue_ProcessedValue_NoGoodData)
                };
            }

            var status = bucket.RawSamples.Any(x => x.Status != TagValueStatus.Good)
                ? TagValueStatus.Uncertain
                : TagValueStatus.Good;

            var minValue = goodQualitySamples
                .OrderBy(x => x.GetValueOrDefault(double.NaN))
                .First();

            return new[] {
                new TagValueBuilder(minValue)
                    .WithStatus(status)
                    .WithBucketProperties(bucket)
                    .WithProperties(CreateXPoweredByProperty())
                    .Build()
            };
        }

        #endregion

        #region [ Max ]

        /// <summary>
        /// Calculates the maximum value of the specified raw samples.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="bucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        private static IEnumerable<TagValueExtended> CalculateMaximum(TagSummary tag, TagValueBucket bucket) {
            var goodQualitySamples = bucket
                .RawSamples
                .Where(x => x.Status == TagValueStatus.Good)
                .ToArray();

            if (goodQualitySamples.Length == 0) {
                return new[] {
                    CreateErrorTagValue(bucket, bucket.UtcBucketStart, Resources.TagValue_ProcessedValue_NoGoodData)
                };
            }

            var status = bucket.RawSamples.Any(x => x.Status != TagValueStatus.Good)
                ? TagValueStatus.Uncertain
                : TagValueStatus.Good;

            var maxValue = goodQualitySamples
                .OrderByDescending(x => x.GetValueOrDefault(double.NaN))
                .First();

            return new[] {
                new TagValueBuilder(maxValue)
                    .WithStatus(status)
                    .WithBucketProperties(bucket)
                    .WithProperties(CreateXPoweredByProperty())
                    .Build()
            };
        }

        #endregion

        #region [ Count ]

        /// <summary>
        /// Returns a value describing the number of raw samples in the provided bucket.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="bucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        private static IEnumerable<TagValueExtended> CalculateCount(TagSummary tag, TagValueBucket bucket) {
            var goodQualitySamples = bucket
                .RawSamples
                .Where(x => x.Status == TagValueStatus.Good)
                .ToArray();

            var status = bucket.RawSamples.Any(x => x.Status != TagValueStatus.Good)
                ? TagValueStatus.Uncertain
                : TagValueStatus.Good;

            if (goodQualitySamples.Length == 0) {
                return new[] {
                    new TagValueBuilder()
                        .WithUtcSampleTime(bucket.UtcBucketStart)
                        .WithValue(0d)
                        .WithStatus(status)
                        .WithBucketProperties(bucket)
                        .WithProperties(CreateXPoweredByProperty())
                        .Build()
                };
            }

            return new[] {
                new TagValueBuilder()
                    .WithUtcSampleTime(bucket.UtcBucketStart)
                    .WithValue(goodQualitySamples.Length)
                    .WithStatus(status)
                    .WithBucketProperties(bucket)
                    .WithProperties(CreateXPoweredByProperty())
                    .Build()
            };
        }

        #endregion

        #region [ Range ]

        /// <summary>
        /// Calculates the absolute difference between the minimum and maximum values in the 
        /// specified raw samples.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="bucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        /// <remarks>
        ///   The status used is the worst-case of all of the <paramref name="bucket"/> values used in 
        ///   the calculation.
        /// </remarks>
        private static IEnumerable<TagValueExtended> CalculateRange(TagSummary tag, TagValueBucket bucket) {
            var goodQualitySamples = bucket
                .RawSamples
                .Where(x => x.Status == TagValueStatus.Good)
                .ToArray();

            if (goodQualitySamples.Length == 0) {
                return new[] {
                    CreateErrorTagValue(bucket, bucket.UtcBucketStart, Resources.TagValue_ProcessedValue_NoGoodData)
                };
            }

            var status = bucket.RawSamples.Any(x => x.Status != TagValueStatus.Good)
                    ? TagValueStatus.Uncertain
                    : TagValueStatus.Good;

            var orderedSamples = goodQualitySamples.OrderBy(x => x.GetValueOrDefault(double.NaN));
            var minValue = orderedSamples.First();
            var maxValue = orderedSamples.Last();
            var numericValue = Math.Abs(maxValue.GetValueOrDefault(double.NaN) - minValue.GetValueOrDefault(double.NaN));

            return new[] {
                new TagValueBuilder()
                    .WithUtcSampleTime(bucket.UtcBucketStart)
                    .WithValue(numericValue)
                    .WithStatus(status)
                    .WithUnits(tag.Units)
                    .WithBucketProperties(bucket)
                    .WithProperties(CreateXPoweredByProperty())
                    .Build()
            };
        }

        #endregion

        #region [ Delta ]

        /// <summary>
        /// Calculates the signed difference between the earliest and latest values in the 
        /// specified raw samples.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="bucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        private static IEnumerable<TagValueExtended> CalculateDelta(TagSummary tag, TagValueBucket bucket) {
            var goodQualitySamples = bucket
                .RawSamples
                .Where(x => x.Status == TagValueStatus.Good)
                .ToArray();

            if (goodQualitySamples.Length == 0) {
                return new[] {
                    CreateErrorTagValue(bucket, bucket.UtcBucketStart, Resources.TagValue_ProcessedValue_NoGoodData)
                };
            }

            var status = bucket.RawSamples.Any(x => x.Status != TagValueStatus.Good)
                    ? TagValueStatus.Uncertain
                    : TagValueStatus.Good;

            var firstValue = goodQualitySamples.First();
            var lastValue = goodQualitySamples.Last();
            var numericValue = firstValue.GetValueOrDefault(double.NaN) - lastValue.GetValueOrDefault(double.NaN);

            return new[] {
                new TagValueBuilder()
                    .WithUtcSampleTime(bucket.UtcBucketStart)
                    .WithValue(numericValue)
                    .WithStatus(status)
                    .WithUnits(tag.Units)
                    .WithBucketProperties(bucket)
                    .WithProperties(CreateXPoweredByProperty())
                    .Build()
            };
        }

        #endregion

        #region [ PercentGood ]

        /// <summary>
        /// Returns a value describing the percentage of time in the bucket that the tag value had 
        /// good quality.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="bucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        private static IEnumerable<TagValueExtended> CalculatePercentGood(TagSummary tag, TagValueBucket bucket) {
            if (bucket.RawSampleCount == 0) {
                double val = 0;

                if (bucket.BeforeStartBoundary.ClosestValue != null) {
                    // We have a sample before the bucket start boundary. If the sample has good
                    // quality, we will return a value specifying that the current bucket is 100%
                    // good; otherwise, the value for the current bucket is 0% good.

                    val = bucket.BeforeStartBoundary.ClosestValue.Status == TagValueStatus.Good
                        ? 100
                        : 0;
                }

                return new[] {
                    new TagValueBuilder()
                        .WithUtcSampleTime(bucket.UtcBucketStart)
                        .WithValue(val)
                        .WithUnits("%")
                        .WithStatus(TagValueStatus.Uncertain)
                        .WithBucketProperties(bucket)
                        .WithProperties(CreateXPoweredByProperty())
                        .Build()
                };
            }

            var timeInState = TimeSpan.Zero;
            var previousSampleTime = bucket.UtcBucketStart;
            var previousStatus = bucket.BeforeStartBoundary.ClosestValue?.Status ?? TagValueStatus.Uncertain;

            foreach (var sample in bucket.RawSamples) {
                try {
                    if (previousStatus != TagValueStatus.Good) {
                        continue;
                    }

                    var diff = sample.UtcSampleTime - previousSampleTime;
                    if (diff <= TimeSpan.Zero) {
                        continue;
                    }

                    timeInState = timeInState.Add(diff);
                }
                finally {
                    previousSampleTime = sample.UtcSampleTime;
                    previousStatus = sample.Status;
                }
            }

            if (previousSampleTime < bucket.UtcBucketEnd && previousStatus == TagValueStatus.Good) {
                timeInState = timeInState.Add(bucket.UtcBucketEnd - previousSampleTime);
            }

            var percentTimeInState = timeInState.TotalMilliseconds / (bucket.UtcBucketEnd - bucket.UtcBucketStart).TotalMilliseconds * 100;

            return new[] {
                new TagValueBuilder()
                    .WithUtcSampleTime(bucket.UtcBucketStart)
                    .WithValue(percentTimeInState)
                    .WithUnits("%")
                    .WithStatus(TagValueStatus.Good)
                    .WithBucketProperties(bucket)
                    .WithProperties(CreateXPoweredByProperty())
                    .Build()
            };
        }

        #endregion

        #region [ PercentBad ]

        /// <summary>
        /// Returns a value describing the percentage of time in the bucket that the tag value had 
        /// bad quality.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="bucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        private static IEnumerable<TagValueExtended> CalculatePercentBad(TagSummary tag, TagValueBucket bucket) {
            if (bucket.RawSampleCount == 0) {
                double val = 0;

                if (bucket.BeforeStartBoundary.ClosestValue != null) {
                    // We have a sample before the bucket start boundary. If the sample has bad
                    // quality, we will return a value specifying that the current bucket is 100%
                    // bad; otherwise, the value for the current bucket is 0% bad.

                    val = bucket.BeforeStartBoundary.ClosestValue.Status == TagValueStatus.Bad
                        ? 100
                        : 0;
                }

                return new[] {
                    new TagValueBuilder()
                        .WithUtcSampleTime(bucket.UtcBucketStart)
                        .WithValue(val)
                        .WithUnits("%")
                        .WithStatus(TagValueStatus.Uncertain)
                        .WithBucketProperties(bucket)
                        .WithProperties(CreateXPoweredByProperty())
                        .Build()
                };
            }

            var timeInState = TimeSpan.Zero;
            var previousSampleTime = bucket.UtcBucketStart;
            var previousStatus = bucket.BeforeStartBoundary.ClosestValue?.Status ?? TagValueStatus.Uncertain;

            foreach (var sample in bucket.RawSamples) {
                try {
                    if (previousStatus != TagValueStatus.Bad) {
                        continue;
                    }

                    var diff = sample.UtcSampleTime - previousSampleTime;
                    if (diff <= TimeSpan.Zero) {
                        continue;
                    }

                    timeInState = timeInState.Add(diff);
                }
                finally {
                    previousSampleTime = sample.UtcSampleTime;
                    previousStatus = sample.Status;
                }
            }

            if (previousSampleTime < bucket.UtcBucketEnd && previousStatus == TagValueStatus.Bad) {
                timeInState = timeInState.Add(bucket.UtcBucketEnd - previousSampleTime);
            }

            var percentTimeInState = timeInState.TotalMilliseconds / (bucket.UtcBucketEnd - bucket.UtcBucketStart).TotalMilliseconds * 100;

            return new[] {
                new TagValueBuilder()
                    .WithUtcSampleTime(bucket.UtcBucketStart)
                    .WithValue(percentTimeInState)
                    .WithUnits("%")
                    .WithStatus(TagValueStatus.Good)
                    .WithBucketProperties(bucket)
                    .WithProperties(CreateXPoweredByProperty())
                    .Build()
            };
        }

        #endregion

        #region [ Variance ]

        /// <summary>
        /// Calculates the variance for the specified values.
        /// </summary>
        /// <param name="values">
        ///   The values.
        /// </param>
        /// <param name="average">
        ///   The average value calculated from the <paramref name="values"/>.
        /// </param>
        /// <returns>
        ///   The variance.
        /// </returns>
        private static double CalculateVariance(IEnumerable<TagValue> values, out double average) {
            var avg = CalculateAverage(values);
            var variance =
                values.Sum(x => Math.Pow(x.GetValueOrDefault(double.NaN) - avg, 2))
                /
                (values.Count() - 1);

            average = avg;
            return variance;
        }


        /// <summary>
        /// Calculates the variance for the good-quality samples in the bucket.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="bucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        private static IEnumerable<TagValueExtended> CalculateVariance(TagSummary tag, TagValueBucket bucket) {
            var goodQualitySamples = bucket
                .RawSamples
                .Where(x => x.Status == TagValueStatus.Good)
                .ToArray();

            var status = bucket.RawSamples.Any(x => x.Status != TagValueStatus.Good)
                ? TagValueStatus.Uncertain
                : TagValueStatus.Good;

            if (goodQualitySamples.Length == 0) {
                return new[] {
                    CreateErrorTagValue(bucket, bucket.UtcBucketStart, Resources.TagValue_ProcessedValue_NoGoodData)
                };
            }

            if (goodQualitySamples.Length == 1) {
                return new[] {
                    new TagValueBuilder()
                        .WithUtcSampleTime(bucket.UtcBucketStart)
                        .WithValue(0d)
                        .WithStatus(status)
                        .WithBucketProperties(bucket)
                        .WithProperties(
                            CreateXPoweredByProperty(),
                            AdapterProperty.Create(string.Intern(CommonTagValuePropertyNames.Average), goodQualitySamples.First().GetValueOrDefault(double.NaN))
                         )
                        .Build()
                };
            }

            var variance = CalculateVariance(goodQualitySamples, out var avg);

            return new[] {
                new TagValueBuilder()
                    .WithUtcSampleTime(bucket.UtcBucketStart)
                    .WithValue(variance)
                    .WithStatus(status)
                    .WithBucketProperties(bucket)
                    .WithProperties(
                        CreateXPoweredByProperty(),
                        AdapterProperty.Create(string.Intern(CommonTagValuePropertyNames.Average), avg)
                    )
                    .Build()
            };
        }

        #endregion

        #region [ StdDev ]

        /// <summary>
        /// Calculates the standard deviation for the good-quality samples in the bucket.
        /// </summary>
        /// <param name="tag">
        ///   The tag definition.
        /// </param>
        /// <param name="bucket">
        ///   The values for the current bucket.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        private static IEnumerable<TagValueExtended> CalculateStandardDeviation(TagSummary tag, TagValueBucket bucket) {
            var goodQualitySamples = bucket
                .RawSamples
                .Where(x => x.Status == TagValueStatus.Good)
                .ToArray();

            var status = bucket.RawSamples.Any(x => x.Status != TagValueStatus.Good)
                ? TagValueStatus.Uncertain
                : TagValueStatus.Good;

            if (goodQualitySamples.Length == 0) {
                return new[] {
                    CreateErrorTagValue(bucket, bucket.UtcBucketStart, Resources.TagValue_ProcessedValue_NoGoodData)
                };
            }
            
            if (goodQualitySamples.Length == 1) {
                return new[] {
                    new TagValueBuilder()
                        .WithUtcSampleTime(bucket.UtcBucketStart)
                        .WithValue(0d)
                        .WithStatus(status)
                        .WithBucketProperties(bucket)
                        .WithProperties(
                            CreateXPoweredByProperty(),
                            AdapterProperty.Create(string.Intern(CommonTagValuePropertyNames.Average), goodQualitySamples.First().GetValueOrDefault(double.NaN)),
                            AdapterProperty.Create(string.Intern(CommonTagValuePropertyNames.Variance), 0d)
                        )
                        .Build()
                };
            }

            var variance = CalculateVariance(goodQualitySamples, out var avg);
            var stdDev = Math.Sqrt(variance);

            const double sigma = 3;
            var lowerBound = avg - (sigma * stdDev);
            var upperBound = avg + (sigma * stdDev);

            return new[] {
                new TagValueBuilder()
                    .WithUtcSampleTime(bucket.UtcBucketStart)
                    .WithValue(stdDev)
                    .WithStatus(status)
                    .WithBucketProperties(bucket)
                    .WithProperties(
                        CreateXPoweredByProperty(),
                        AdapterProperty.Create(string.Intern(CommonTagValuePropertyNames.Average), avg),
                        AdapterProperty.Create(string.Intern(CommonTagValuePropertyNames.Variance), variance),
                        AdapterProperty.Create(string.Intern(CommonTagValuePropertyNames.LowerBound), lowerBound),
                        AdapterProperty.Create(string.Intern(CommonTagValuePropertyNames.UpperBound), upperBound),
                        AdapterProperty.Create(string.Intern(CommonTagValuePropertyNames.Sigma), sigma)
                    )
                    .Build()
            };
        }

        #endregion

        #region [ Aggregation using Data Function Names ]

        /// <summary>
        /// Gets the aggregate calculators that map to the specified data function names or IDs.
        /// </summary>
        /// <param name="dataFunctions">
        ///   The data functions for the request. These can specify the function ID, display name, 
        ///   or an alias for a data function.
        /// </param>
        /// <returns>
        ///   A <see cref="IDictionary{TKey, TValue}"/> that maps from the item specified in 
        ///   <paramref name="dataFunctions"/> to the correspoding <see cref="AggregateCalculator"/>. 
        ///   No entries are added for items in <paramref name="dataFunctions"/> that cannot be 
        ///   resolved.
        /// </returns>
        private IDictionary<string, AggregateCalculator> GetAggregateCalculatorsForRequest(IEnumerable<string> dataFunctions) {
            var funcs = new Dictionary<string, AggregateCalculator>();

            if (dataFunctions == null) {
                return funcs;
            }

            foreach (var item in dataFunctions) {
                if (string.IsNullOrWhiteSpace(item)) {
                    continue;
                }

                // Try and get the data function descriptor from the default functions.
                var dataFunc = s_defaultDataFunctions.Value.FirstOrDefault(x => x.IsMatch(item));
                if (dataFunc == null) {
                    // Not a default function; check the custom functions.
                    dataFunc = _customAggregates.Values.FirstOrDefault(x => x.IsMatch(item));
                }
                if (dataFunc == null) {
                    // Unknown data function.
                    continue;
                }

                if (s_defaultAggregatorMap.TryGetValue(dataFunc.Id, out var func)) {
                    funcs[item] = func;
                }
                else if (_customAggregatorMap.TryGetValue(dataFunc.Id, out func)) {
                    funcs[item] = func;
                }
            }

            return funcs;
        }

        /// <summary>
        /// Performs aggregation on raw tag values.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <param name="dataFunctions">
        ///   The data functions to apply to the raw data.
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
        ///   The <see cref="IAsyncEnumerable{T}"/> that will provide the raw data for the aggregation calculations.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will emit the calculated values.
        /// </returns>
        public async IAsyncEnumerable<ProcessedTagValueQueryResult> GetAggregatedValues(
            TagSummary tag, 
            IEnumerable<string> dataFunctions, 
            DateTime utcStartTime, 
            DateTime utcEndTime, 
            TimeSpan sampleInterval, 
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
            if (sampleInterval <= TimeSpan.Zero) {
                throw new ArgumentException(SharedResources.Error_SampleIntervalMustBeGreaterThanZero, nameof(sampleInterval));
            }

            var funcs = GetAggregateCalculatorsForRequest(dataFunctions);

            if (funcs.Count == 0) {
                yield break;
            }

            await foreach (var val in GetAggregatedValues(tag, utcStartTime, utcEndTime, sampleInterval, rawData, funcs, cancellationToken).ConfigureAwait(false)) {
                yield return val;
            }
        }


        /// <summary>
        /// Performs aggregation on raw tag values.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <param name="dataFunctions">
        ///   The data functions to apply to the raw data.
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
        ///   The raw data for the aggregation calculations.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will emit the calculated values.
        /// </returns>
        public IAsyncEnumerable<ProcessedTagValueQueryResult> GetAggregatedValues(
            TagSummary tag,
            IEnumerable<string> dataFunctions,
            DateTime utcStartTime,
            DateTime utcEndTime,
            TimeSpan sampleInterval,
            IEnumerable<TagValueQueryResult> rawData,
            CancellationToken cancellationToken = default
        ) {
            if (rawData == null) {
                throw new ArgumentNullException(nameof(rawData));
            }

            var channel = rawData.PublishToChannel();

            return GetAggregatedValues(
                tag,
                dataFunctions,
                utcStartTime,
                utcEndTime,
                sampleInterval,
                channel.ReadAllAsync(cancellationToken),
                cancellationToken
            );
        }


        /// <summary>
        /// Performs aggregation on raw tag values.
        /// </summary>
        /// <param name="tags">
        ///   The tags in the query.
        /// </param>
        /// <param name="dataFunctions">
        ///   The data functions to apply to the raw data.
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
        ///   The channel that will provide the raw data for the aggregation calculations.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The background task service to use when writing values into the channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will emit the calculated values.
        /// </returns>
        public async IAsyncEnumerable<ProcessedTagValueQueryResult> GetAggregatedValues(
            IEnumerable<TagSummary> tags, 
            IEnumerable<string> dataFunctions, 
            DateTime utcStartTime, 
            DateTime utcEndTime, 
            TimeSpan sampleInterval, 
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
            if (sampleInterval <= TimeSpan.Zero) {
                throw new ArgumentException(SharedResources.Error_SampleIntervalMustBeGreaterThanZero, nameof(sampleInterval));
            }
            if (rawData == null) {
                throw new ArgumentNullException(nameof(rawData));
            }

            if (backgroundTaskService == null) {
                backgroundTaskService = BackgroundTaskService.Default;
            }

            if (!tags.Any()) {
                // No tags.
                yield break;
            }

            if (tags.Count() == 1) {
                // Single tag; use the optimised single-tag overload.
                await foreach (var val in GetAggregatedValues(
                    tags.First(),
                    dataFunctions,
                    utcStartTime,
                    utcEndTime,
                    sampleInterval,
                    rawData,
                    cancellationToken
                )) {
                    yield return val;
                }
                yield break;
            }

            var funcs = GetAggregateCalculatorsForRequest(dataFunctions);

            if (funcs.Count == 0) {
                // No aggregate functions specified.
                yield break;
            }

            // Multiple tags; create a single result channel, and create individual input channels 
            // for each tag in the request and redirect each value emitted from the raw data channel 
            // into the appropriate per-tag input channel.

            var result = Channel.CreateBounded<ProcessedTagValueQueryResult>(new BoundedChannelOptions(500) {
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

            async Task GetValuesForTag(TagSummary tag, ChannelReader<TagValueQueryResult> reader, ChannelWriter<ProcessedTagValueQueryResult> writer, CancellationToken cancellationToken) {
                await foreach (var val in GetAggregatedValues(tag, utcStartTime, utcEndTime, sampleInterval, reader.ReadAllAsync(cancellationToken), funcs, cancellationToken).ConfigureAwait(false)) {
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
        /// Performs aggregation on raw tag values.
        /// </summary>
        /// <param name="tags">
        ///   The tags in the query.
        /// </param>
        /// <param name="dataFunctions">
        ///   The data functions to apply to the raw data.
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
        ///   The raw data for the aggregation calculations.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The background task service to use when writing values into the channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will emit the calculated values.
        /// </returns>
        public IAsyncEnumerable<ProcessedTagValueQueryResult> GetAggregatedValues(
            IEnumerable<TagSummary> tags,
            IEnumerable<string> dataFunctions,
            DateTime utcStartTime,
            DateTime utcEndTime,
            TimeSpan sampleInterval,
            IEnumerable<TagValueQueryResult> rawData,
            IBackgroundTaskService? backgroundTaskService = null,
            CancellationToken cancellationToken = default
        ) {
            if (rawData == null) {
                throw new ArgumentNullException(nameof(rawData));
            }

            var channel = rawData.PublishToChannel();

            return GetAggregatedValues(
                tags,
                dataFunctions,
                utcStartTime,
                utcEndTime,
                sampleInterval,
                channel.ReadAllAsync(cancellationToken),
                backgroundTaskService,
                cancellationToken
            );
        }


        /// <summary>
        /// Performs aggregation for a tag.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <param name="utcStartTime">
        ///   The UTC start time of the data query.
        /// </param>
        /// <param name="utcEndTime">
        ///   The UTC end time of the data query.
        /// </param>
        /// <param name="sampleInterval">
        ///   The sample interval.
        /// </param>
        /// <param name="rawData">
        ///   An <see cref="IAsyncEnumerable{T}"/> that will provide raw tag values to be aggregated.
        /// </param>
        /// <param name="funcs">
        ///   The aggregations to perform.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will read raw data, aggregate it, and write 
        ///   the results to the output channel.
        /// </returns>
        private static async IAsyncEnumerable<ProcessedTagValueQueryResult> GetAggregatedValues(
            TagSummary tag, 
            DateTime utcStartTime, 
            DateTime utcEndTime, 
            TimeSpan sampleInterval, 
            IAsyncEnumerable<TagValueQueryResult> rawData,
            IDictionary<string, AggregateCalculator> funcs, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var bucket = new TagValueBucket(utcStartTime, utcStartTime.Add(sampleInterval), utcStartTime, utcEndTime);

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
                        foreach (var calcVal in CalculateAndEmitBucketSamples(tag, bucket, funcs, utcStartTime, utcEndTime)) {
                            yield return calcVal;
                        }

                        // Create a new bucket.
                        var oldBucket = bucket;
                        bucket = new TagValueBucket(bucket.UtcBucketEnd, bucket.UtcBucketEnd.Add(sampleInterval), utcStartTime, utcEndTime);

                        // Copy pre-/post-end boundary values from the old bucket to the new bucket.
                        bucket.AddBoundarySamples(oldBucket);
                    } while (val.Value.UtcSampleTime >= bucket.UtcBucketEnd && bucket.UtcBucketEnd <= utcEndTime);
                }
            }

            if (bucket.UtcBucketEnd <= utcEndTime) {
                // Only emit the final bucket if it is within our time range.
                foreach (var calcVal in CalculateAndEmitBucketSamples(tag, bucket, funcs, utcStartTime, utcEndTime)) {
                    yield return calcVal;
                }
            }

            if (bucket.UtcBucketEnd < utcEndTime) {
                // The raw data ended before the end time for the query. We will keep moving forward 
                // according to our sample interval, and allow our aggregator the chance to calculate 
                // values for the remaining buckets.

                while (bucket.UtcBucketEnd < utcEndTime) {
                    var oldBucket = bucket;
                    bucket = new TagValueBucket(bucket.UtcBucketEnd, bucket.UtcBucketEnd.Add(sampleInterval), utcStartTime, utcEndTime);
                    if (bucket.UtcBucketEnd > utcEndTime) {
                        // New bucket would end after the query end time, so we don't need to 
                        // calculate for this bucket.
                        break;
                    }

                    // Copy pre-/post-end boundary values from the old bucket to the new bucket.
                    bucket.AddBoundarySamples(oldBucket);

                    foreach (var calcVal in CalculateAndEmitBucketSamples(tag, bucket, funcs, utcStartTime, utcEndTime)) {
                        yield return calcVal;
                    }
                }
            }
        }


        /// <summary>
        /// Computes and emits the aggregated samples for a single bucket.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <param name="bucket">
        ///   The bucket.
        /// </param>
        /// <param name="funcs">
        ///   The aggregations to perform.
        /// </param>
        /// <param name="utcNotBefore">
        ///   Values occurring before this time will not be emitted.
        /// </param>
        /// <param name="utcNotAfter">
        ///   Values occurring after this time will not be emitted.
        /// </param>
        /// <returns>
        ///   A task that will compute and emit the aggregated samples for the bucket.
        /// </returns>
        private static IEnumerable<ProcessedTagValueQueryResult> CalculateAndEmitBucketSamples(
            TagSummary tag, 
            TagValueBucket bucket,
            IDictionary<string, AggregateCalculator> funcs, 
            DateTime utcNotBefore, 
            DateTime utcNotAfter
        ) {
            foreach (var agg in funcs) {
                var vals = agg.Value.Invoke(tag, bucket);
                if (vals == null || !vals.Any()) {
                    continue;
                }

                foreach (var val in vals.Where(v => v != null).Where(v => v.UtcSampleTime >= utcNotBefore && v.UtcSampleTime <= utcNotAfter)) {
                    if (val != null) {
                        yield return ProcessedTagValueQueryResult.Create(tag.Id, tag.Name, val, agg.Key);
                    }
                }
            }
        }


        /// <summary>
        /// Creates a property that can be assigned to calculated values to indicate that they 
        /// were calculated using the aggregation helper.
        /// </summary>
        /// <returns>
        ///   A new <see cref="AdapterProperty"/> object.
        /// </returns>
        internal static AdapterProperty CreateXPoweredByProperty() {
            return AdapterProperty.Create(string.Intern(CommonTagValuePropertyNames.XPoweredBy), s_xPoweredByPropertyValue.Value);
        }


        /// <summary>
        /// Creates a property that indicates that a value was calculated using a partial or 
        /// incomplete data set.
        /// </summary>
        /// <returns>
        ///   A new <see cref="AdapterProperty"/> object.
        /// </returns>
        private static AdapterProperty CreatePartialProperty() {
            return AdapterProperty.Create(CommonTagValuePropertyNames.Partial, true);
        }


        /// <summary>
        /// Creates a tag value for a bucket that contains an error message.
        /// </summary>
        /// <param name="bucket">
        ///   The bucket.
        /// </param>
        /// <param name="sampleTime">
        ///   The UTC sample time.
        /// </param>
        /// <param name="error">
        ///   The error message.
        /// </param>
        /// <returns>
        ///   The tag value.
        /// </returns>
        private static TagValueExtended CreateErrorTagValue(TagValueBucket bucket, DateTime sampleTime, string error) {
            return new TagValueBuilder()
                .WithUtcSampleTime(sampleTime)
                .WithValue(string.Intern(Resources.TagValue_ProcessedValue_Error))
                .WithStatus(TagValueStatus.Bad)
                .WithError(error)
                .WithBucketProperties(bucket)
                .WithProperties(CreateXPoweredByProperty())
                .Build();
        }


        /// <summary>
        /// Gets the descriptors for the default data functions supported by <see cref="AggregationHelper"/>.
        /// </summary>
        /// <returns>
        ///   The default data function descriptors.
        /// </returns>
        public static IEnumerable<DataFunctionDescriptor> GetDefaultDataFunctions() {
            return s_defaultDataFunctions.Value.ToArray();
        }


        /// <summary>
        /// Gets the descriptors for the data functions supported by the <see cref="AggregationHelper"/>.
        /// </summary>
        /// <returns>
        ///   The supported data function descriptors.
        /// </returns>
        public IEnumerable<DataFunctionDescriptor> GetSupportedDataFunctions() {
            return s_defaultDataFunctions.Value.Concat(_customAggregates.Values).ToArray();
        }

        #endregion

        #region [ Custom Function Registration ]

        /// <summary>
        /// Registers a custom data function.
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

            if (s_defaultAggregatorMap.ContainsKey(descriptor.Id)) {
                // Don't allow built-in aggregates to be overridden.
                return false;
            }

            if (!_customAggregates.TryAdd(descriptor.Id, descriptor)) {
                // Function has already been added.
                return false;
            }

            _customAggregatorMap[descriptor.Id] = calculator;

            return true;
        }


        /// <summary>
        /// Unregisters a custom data function.
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

            return _customAggregates.TryRemove(functionId, out var _) && _customAggregatorMap.TryRemove(functionId, out var _);
        }

        #endregion

    }
}
