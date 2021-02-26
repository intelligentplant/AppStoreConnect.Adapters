using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            { DefaultDataFunctions.Interpolate.Id, CalculateInterpolated },
            { DefaultDataFunctions.Maximum.Id, CalculateMaximum },
            { DefaultDataFunctions.Minimum.Id, CalculateMinimum },
            { DefaultDataFunctions.PercentBad.Id, CalculatePercentBad },
            { DefaultDataFunctions.PercentGood.Id, CalculatePercentGood },
            { DefaultDataFunctions.Range.Id, CalculateRange },
            { DefaultDataFunctions.Delta.Id, CalculateDelta },
            { DefaultDataFunctions.Variance.Id, CalculateVariance },
            { DefaultDataFunctions.StandardDeviation.Id, CalculateStandardDeviation }
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
            // We calculate at the bucket start time. Our complete input data set consists of the 
            // start boundary values followed by the raw samples in the bucket.
            var combinedInputValues = bucket
                .StartBoundary
                .GetBoundarySamples()
                .Concat(bucket.RawSamples)
                .ToArray();

            var result = InterpolationHelper.GetInterpolatedValueAtSampleTime(
                tag,
                bucket.UtcBucketStart,
                combinedInputValues
            );

            if (result == null) {
                result = CreateErrorTagValue(
                    bucket,
                    bucket.UtcBucketStart,
                    Resources.TagValue_ProcessedValue_NoData
                );
            }

            yield return result;

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
            return values.Average(x => x.Value.GetValueOrDefault(double.NaN));
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
                .OrderBy(x => x.Value.GetValueOrDefault(double.NaN))
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
                .OrderByDescending(x => x.Value.GetValueOrDefault(double.NaN))
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

            var orderedSamples = goodQualitySamples.OrderBy(x => x.Value.GetValueOrDefault(double.NaN));
            var minValue = orderedSamples.First();
            var maxValue = orderedSamples.Last();
            var numericValue = Math.Abs(maxValue.Value.GetValueOrDefault(double.NaN) - minValue.Value.GetValueOrDefault(double.NaN));

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
            var numericValue = firstValue.Value.GetValueOrDefault(double.NaN) - lastValue.Value.GetValueOrDefault(double.NaN);

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
        /// Returns a value describing the percentage of raw samples in the provided bucket that 
        /// have good quality.
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
                return new[] {
                    new TagValueBuilder()
                        .WithUtcSampleTime(bucket.UtcBucketStart)
                        .WithValue(0d)
                        .WithUnits("%")
                        .WithStatus(TagValueStatus.Uncertain)
                        .WithBucketProperties(bucket)
                        .WithProperties(CreateXPoweredByProperty())
                        .Build()
                };
            }

            var percentGoodCount = bucket.RawSamples.Count(x => x.Status == TagValueStatus.Good);

            return new[] {
                new TagValueBuilder()
                    .WithUtcSampleTime(bucket.UtcBucketStart)
                    .WithValue((double) percentGoodCount / bucket.RawSampleCount * 100)
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
        /// Returns a value describing the percentage of raw samples in the provided bucket that 
        /// have bad quality.
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
                return new[] {
                    new TagValueBuilder()
                        .WithUtcSampleTime(bucket.UtcBucketStart)
                        .WithValue(0d)
                        .WithUnits("%")
                        .WithStatus(TagValueStatus.Uncertain)
                        .WithBucketProperties(bucket)
                        .WithProperties(CreateXPoweredByProperty())
                        .Build()
                };
            }

            var percentBadCount = bucket.RawSamples.Count(x => x.Status == TagValueStatus.Bad);

            return new[] {
                new TagValueBuilder()
                    .WithUtcSampleTime(bucket.UtcBucketStart)
                    .WithValue((double) percentBadCount / bucket.RawSampleCount * 100)
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
                            AdapterProperty.Create(CommonTagPropertyNames.Average, goodQualitySamples.First().GetValueOrDefault(double.NaN))
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
                        AdapterProperty.Create(CommonTagPropertyNames.Average, avg)
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
                            AdapterProperty.Create(CommonTagPropertyNames.Average, goodQualitySamples.First().GetValueOrDefault(double.NaN)),
                            AdapterProperty.Create(CommonTagPropertyNames.Variance, 0d)
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
                        AdapterProperty.Create(CommonTagPropertyNames.Average, avg),
                        AdapterProperty.Create(CommonTagPropertyNames.Variance, variance),
                        AdapterProperty.Create(CommonTagPropertyNames.LowerBound, lowerBound),
                        AdapterProperty.Create(CommonTagPropertyNames.UpperBound, upperBound),
                        AdapterProperty.Create(CommonTagPropertyNames.Sigma, sigma)
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
        public ChannelReader<ProcessedTagValueQueryResult> GetAggregatedValues(
            TagSummary tag, 
            IEnumerable<string> dataFunctions, 
            DateTime utcStartTime, 
            DateTime utcEndTime, 
            TimeSpan sampleInterval, 
            ChannelReader<TagValueQueryResult> rawData, 
            IBackgroundTaskService? backgroundTaskService = null, 
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

            var result = Channel.CreateBounded<ProcessedTagValueQueryResult>(new BoundedChannelOptions(500) {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = true
            });

            var funcs = GetAggregateCalculatorsForRequest(dataFunctions);

            if (funcs.Count == 0) {
                result.Writer.TryComplete();
                return result;
            }
            
            result.Writer.RunBackgroundOperation((ch, ct) => GetAggregatedValues(tag, utcStartTime, utcEndTime, sampleInterval, rawData, ch, funcs, ct), true, backgroundTaskService, cancellationToken);
            return result;
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
        /// <param name="backgroundTaskService">
        ///   The background task service to use when writing values into the channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will emit the calculated values.
        /// </returns>
        public ChannelReader<ProcessedTagValueQueryResult> GetAggregatedValues(
            TagSummary tag,
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

            var channel = Channel.CreateUnbounded<TagValueQueryResult>(new UnboundedChannelOptions() {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = true
            });

            foreach (var item in rawData) {
                channel.Writer.TryWrite(item);
            }
            channel.Writer.TryComplete();

            return GetAggregatedValues(
                tag,
                dataFunctions,
                utcStartTime,
                utcEndTime,
                sampleInterval,
                channel,
                backgroundTaskService,
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
        public ChannelReader<ProcessedTagValueQueryResult> GetAggregatedValues(
            IEnumerable<TagSummary> tags, 
            IEnumerable<string> dataFunctions, 
            DateTime utcStartTime, 
            DateTime utcEndTime, 
            TimeSpan sampleInterval, 
            ChannelReader<TagValueQueryResult> rawData, 
            IBackgroundTaskService? backgroundTaskService = null, 
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

            Channel<ProcessedTagValueQueryResult> result;

            if (!tags.Any()) {
                // No tags; create the channel and return.
                result = Channel.CreateUnbounded<ProcessedTagValueQueryResult>();
                result.Writer.TryComplete();
                return result;
            }

            if (tags.Count() == 1) {
                // Single tag; use the optimised single-tag overload.
                return GetAggregatedValues(
                    tags.First(),
                    dataFunctions,
                    utcStartTime,
                    utcEndTime,
                    sampleInterval,
                    rawData,
                    backgroundTaskService,
                    cancellationToken
                );
            }

            var funcs = GetAggregateCalculatorsForRequest(dataFunctions);

            if (funcs.Count == 0) {
                // No aggregate functions specified; complete the channel and return.
                result = Channel.CreateUnbounded<ProcessedTagValueQueryResult>();
                result.Writer.TryComplete();
                return result;
            }

            // Multiple tags; create a single result channel, and create individual input channels 
            // for each tag in the request and redirect each value emitted from the raw data channel 
            // into the appropriate per-tag input channel.

            result = Channel.CreateBounded<ProcessedTagValueQueryResult>(new BoundedChannelOptions(500) {
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
            }, backgroundTaskService, cancellationToken);

            // Execute stream for each tag in the query and write all values into the shared 
            // result channel.
            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await Task.WhenAll(
                    tagRawDataChannels.Select(x => GetAggregatedValues(
                        tagLookupById[x.Key],
                        utcStartTime,
                        utcEndTime,
                        sampleInterval,
                        x.Value,
                        ch,
                        funcs,
                        ct
                    ))
                ).WithCancellation(ct).ConfigureAwait(false);
            }, true, backgroundTaskService, cancellationToken);

            return result;
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
        public ChannelReader<ProcessedTagValueQueryResult> GetAggregatedValues(
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

            var channel = Channel.CreateUnbounded<TagValueQueryResult>(new UnboundedChannelOptions() {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = true
            });

            foreach (var item in rawData) {
                channel.Writer.TryWrite(item);
            }
            channel.Writer.TryComplete();

            return GetAggregatedValues(
                tags,
                dataFunctions,
                utcStartTime,
                utcEndTime,
                sampleInterval,
                channel,
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
        ///   A channel that will provide raw tag values to be aggregated.
        /// </param>
        /// <param name="resultChannel">
        ///   A channel that calculated results will be written to.
        /// </param>
        /// <param name="funcs">
        ///   The aggregations to perform.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will read raw data, aggregate it, and write the results to the output 
        ///   channel.
        /// </returns>
        private static async Task GetAggregatedValues(
            TagSummary tag, 
            DateTime utcStartTime, 
            DateTime utcEndTime, 
            TimeSpan sampleInterval, 
            ChannelReader<TagValueQueryResult> rawData, 
            ChannelWriter<ProcessedTagValueQueryResult> resultChannel, 
            IDictionary<string, AggregateCalculator> funcs, 
            CancellationToken cancellationToken
        ) {
            var bucket = new TagValueBucket(utcStartTime, utcStartTime.Add(sampleInterval), utcStartTime, utcEndTime);
            
            while (await rawData.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!rawData.TryRead(out var val)) {
                    break;
                }

                if (val == null) {
                    continue;
                }

                if (val.Value.UtcSampleTime < bucket.UtcBucketStart) {
                    bucket.UpdateStartBoundaryValue(val.Value);
                    continue;
                }

                if (val.Value.UtcSampleTime >= bucket.UtcBucketEnd) {
                    // The sample we have just received is later than the end time for the current 
                    // bucket.

                    do {
                        // Emit values from the current bucket and create a new bucket.
                        await CalculateAndEmitBucketSamples(tag, bucket, resultChannel, funcs, utcStartTime, utcEndTime, cancellationToken).ConfigureAwait(false);
                        var previousBucket = bucket;
                        bucket = new TagValueBucket(bucket.UtcBucketEnd, bucket.UtcBucketEnd.Add(sampleInterval), utcStartTime, utcEndTime);

                        // Add the end boundary value(s) from the previous bucket as the start 
                        // boundary value(s) on the new one.
                        if (previousBucket.EndBoundary.BoundaryStatus == TagValueStatus.Good) {
                            if (previousBucket.EndBoundary.BestQualityValue != null) {
                                bucket.UpdateStartBoundaryValue(previousBucket.EndBoundary.BestQualityValue);
                            }
                        }
                        else {
                            if (previousBucket.EndBoundary.BestQualityValue != null) {
                                bucket.UpdateStartBoundaryValue(previousBucket.EndBoundary.BestQualityValue);
                            }
                            if (previousBucket.EndBoundary.ClosestValue != null) {
                                bucket.UpdateStartBoundaryValue(previousBucket.EndBoundary.ClosestValue);
                            }
                        }
                    } while (val.Value.UtcSampleTime >= bucket.UtcBucketEnd);
                }

                // Add the sample to the bucket.
                if (val.Value.UtcSampleTime <= utcEndTime) {
                    bucket.AddRawSample(val.Value);
                }
            }

            if (bucket.UtcBucketEnd <= utcEndTime) {
                // Only emit the final bucket if it is within our time range.
                await CalculateAndEmitBucketSamples(tag, bucket, resultChannel, funcs, utcStartTime, utcEndTime, cancellationToken).ConfigureAwait(false);
            }

            if (bucket.UtcBucketEnd < utcEndTime) {
                // The raw data ended before the end time for the query. We will keep moving forward 
                // according to our sample interval, and allow our aggregator the chance to calculate 
                // values for the remaining buckets.

                while (bucket.UtcBucketEnd < utcEndTime) {
                    var previousBucket = bucket;
                    bucket = new TagValueBucket(bucket.UtcBucketEnd, bucket.UtcBucketEnd.Add(sampleInterval), utcStartTime, utcEndTime);
                    if (bucket.UtcBucketEnd > utcEndTime) {
                        // New bucket would end after the query end time, so we don't need to 
                        // calculate for this bucket.
                        break;
                    }

                    // Add the end boundary value(s) from the previous bucket as the start 
                    // boundary value(s) on the new one.
                    if (previousBucket.EndBoundary.BoundaryStatus == TagValueStatus.Good) {
                        if (previousBucket.EndBoundary.BestQualityValue != null) {
                            bucket.UpdateStartBoundaryValue(previousBucket.EndBoundary.BestQualityValue);
                        }
                    }
                    else {
                        if (previousBucket.EndBoundary.BestQualityValue != null) {
                            bucket.UpdateStartBoundaryValue(previousBucket.EndBoundary.BestQualityValue);
                        }
                        if (previousBucket.EndBoundary.ClosestValue != null) {
                            bucket.UpdateStartBoundaryValue(previousBucket.EndBoundary.ClosestValue);
                        }
                    }

                    await CalculateAndEmitBucketSamples(tag, bucket, resultChannel, funcs, utcStartTime, utcEndTime, cancellationToken).ConfigureAwait(false);
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
        /// <param name="resultChannel">
        ///   The channel to write the results to.
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will compute and emit the aggregated samples for the bucket.
        /// </returns>
        private static async Task CalculateAndEmitBucketSamples(
            TagSummary tag, 
            TagValueBucket bucket, 
            ChannelWriter<ProcessedTagValueQueryResult> resultChannel, 
            IDictionary<string, AggregateCalculator> funcs, 
            DateTime utcNotBefore, 
            DateTime utcNotAfter, 
            CancellationToken cancellationToken
        ) {
            foreach (var agg in funcs) {
                var vals = agg.Value.Invoke(tag, bucket);
                if (vals == null || !vals.Any()) {
                    continue;
                }

                foreach (var val in vals.Where(v => v != null).Where(v => v.UtcSampleTime >= utcNotBefore && v.UtcSampleTime <= utcNotAfter)) {
                    if (val != null && await resultChannel.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                        resultChannel.TryWrite(ProcessedTagValueQueryResult.Create(tag.Id, tag.Name, val, agg.Key));
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
            return AdapterProperty.Create(CommonTagPropertyNames.XPoweredBy, s_xPoweredByPropertyValue.Value);
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
                .WithValue(Resources.TagValue_ProcessedValue_Error)
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
