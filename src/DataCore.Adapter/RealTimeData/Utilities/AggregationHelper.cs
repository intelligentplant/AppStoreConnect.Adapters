﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
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
        /// Values calculated using <see cref="AggregationHelper"/> will contain a property with 
        /// this name.
        /// </summary>
        public const string XPoweredByPropertyName = "X-Powered-By";

        /// <summary>
        /// Property value to use with <see cref="CreateXPoweredByProperty"/>.
        /// </summary>
        private static Lazy<string> s_xPoweredByPropertyValue = new Lazy<string>(() => {
            // Value will be in the format "DataCore.Adapter v1.2.3.4"
            var asm = typeof(AggregationHelper).Assembly;
            var fileVersion = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyFileVersionAttribute>(asm);
            return string.IsNullOrEmpty(fileVersion?.Version)
                ? string.Concat(asm.GetName().Name, " v", asm.GetName().Version)
                : string.Concat(asm.GetName().Name, " v", fileVersion.Version);
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
            { DefaultDataFunctions.Delta.Id, CalculateDelta }
        };

        /// <summary>
        /// The descriptors for the default supported data functions.
        /// </summary>
        private static readonly Lazy<IEnumerable<DataFunctionDescriptor>> s_defaultDataFunctions = new Lazy<IEnumerable<DataFunctionDescriptor>>(() => s_defaultAggregatorMap.Keys.Select(x => DefaultDataFunctions.FindById(x)).ToArray(), LazyThreadSafetyMode.PublicationOnly);

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
            var result = new List<TagValueExtended>();

            TagValueExtended sample0 = null;
            TagValueExtended sample1 = null;

            // First, check if our bucket contains samples, and the first sample exactly matches 
            // the requested start time.

            var interpRequired = true;

            if (bucket.RawSamples.Count > 0) {
                var val = bucket.RawSamples.First();
                if (val.UtcSampleTime == bucket.UtcBucketStart) {
                    result.Add(TagValueBuilder
                        .CreateFromExisting(val)
                        .WithProperties(CreateXPoweredByProperty())
                        .Build()
                    );
                    interpRequired = false;
                }
            }

            if (interpRequired) {
                if (bucket.RawSamples.Count == 0) {
                    // No samples in the current bucket. We can still extrapolate a value if we have 
                    // at least two samples in the PreBucketSamples collection.

                    if (bucket.PreBucketSamples.Count == 2) {
                        sample0 = bucket.PreBucketSamples[0];
                        sample1 = bucket.PreBucketSamples[1];
                    }
                    else if (bucket.PreBucketSamples.Count > 2) {
                        var preBucketSamples = bucket.PreBucketSamples.Reverse().Take(2).ToArray();
                        // Samples were reversed; more-recent sample will be at index 0.
                        sample0 = preBucketSamples[1];
                        sample1 = preBucketSamples[0];
                    }
                }
                else {
                    // We have samples in the current bucket.

                    sample1 = bucket.RawSamples[0];

                    if (bucket.PreBucketSamples.Count > 0) {
                        // We have at least one usable sample from the pre-bucket samples collection 
                        // that we can use as the earlier sample in our interpolation.

                        sample0 = bucket.PreBucketSamples.Last();
                    }
                    else if (bucket.RawSamples.Count > 1) {
                        // If we have more than one sample in the current bucket, we will extrapolate 
                        // backwards from the first two samples to the bucket start time.

                        sample0 = sample1;
                        sample1 = bucket.RawSamples[1];
                    }

                }

                var val = InterpolationHelper.GetValueAtTime(
                    tag,
                    bucket.UtcBucketStart,
                    sample0,
                    sample1,
                    InterpolationCalculationType.Interpolate
                );

                if (val != null) {
                    result.Add(val);
                }
            }

            if (bucket.UtcBucketStart < bucket.UtcQueryEnd && bucket.UtcBucketEnd >= bucket.UtcQueryEnd) {
                // This is the final bucket in the query and the bucket started before the query 
                // end time; we need to emit a second value at the query end time.

                // First, check if the last sample in the bucket is exactly at the query end time.

                interpRequired = true;
                if (bucket.RawSamples.Count > 0) {
                    if (bucket.RawSamples.Count > 0) {
                        var val = bucket.RawSamples.Last();
                        if (val.UtcSampleTime == bucket.UtcQueryEnd) {
                            result.Add(TagValueBuilder
                                .CreateFromExisting(val)
                                .WithProperties(CreateXPoweredByProperty())
                                .Build()
                            );
                            interpRequired = false;
                        }
                    }
                }

                if (interpRequired) {
                    // Due to the way LINQ lazy-evaluates this chain, we'll never actually call the Concat 
                    // method if we get two values from the raw samples.
                    var lastSamples = bucket
                        .RawSamples
                        .Reverse()
                        .Concat(bucket.PreBucketSamples.Reverse())
                        .Take(2)
                        .ToArray();

                    if (lastSamples.Length == 2) {

                        // Indices reversed because we reversed the samples above.
                        sample0 = lastSamples[1];
                        sample1 = lastSamples[0];

                        var val = InterpolationHelper.GetValueAtTime(
                            tag,
                            bucket.UtcQueryEnd,
                            sample0,
                            sample1,
                            InterpolationCalculationType.Interpolate
                        );

                        if (val != null) {
                            result.Add(val);
                        }
                    }
                }
            }

            return result.ToArray();
        }

        #endregion

        #region [ Average ]

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
                    CreateErrorTagValue(bucket, bucket.UtcBucketStart, Resources.TagValue_ProcessedValue_NoData)
                };
            }

            var status = bucket.RawSamples.Any(x => x.Status != TagValueStatus.Good)
                ? TagValueStatus.Uncertain
                : TagValueStatus.Good;

            var numericValue = goodQualitySamples.Average(x => x.Value.GetValueOrDefault(double.NaN));

            return new[] {
                TagValueBuilder.Create()
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
                    CreateErrorTagValue(bucket, bucket.UtcBucketStart, Resources.TagValue_ProcessedValue_NoData)
                };
            }

            var status = bucket.RawSamples.Any(x => x.Status != TagValueStatus.Good)
                ? TagValueStatus.Uncertain
                : TagValueStatus.Good;

            var minValue = goodQualitySamples
                .OrderBy(x => x.Value.GetValueOrDefault(double.NaN))
                .First();

            return new[] {
                TagValueBuilder.CreateFromExisting(minValue)
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
                    CreateErrorTagValue(bucket, bucket.UtcBucketStart, Resources.TagValue_ProcessedValue_NoData)
                };
            }

            var status = bucket.RawSamples.Any(x => x.Status != TagValueStatus.Good)
                ? TagValueStatus.Uncertain
                : TagValueStatus.Good;

            var maxValue = goodQualitySamples
                .OrderByDescending(x => x.Value.GetValueOrDefault(double.NaN))
                .First();

            return new[] {
                TagValueBuilder.CreateFromExisting(maxValue)
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
                    TagValueBuilder.Create()
                        .WithUtcSampleTime(bucket.UtcBucketStart)
                        .WithValue(0d)
                        .WithStatus(status)
                        .WithBucketProperties(bucket)
                        .WithProperties(CreateXPoweredByProperty())
                        .Build()
                };
            }

            return new[] {
                TagValueBuilder.Create()
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
                    CreateErrorTagValue(bucket, bucket.UtcBucketStart, Resources.TagValue_ProcessedValue_NoData)
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
                TagValueBuilder.Create()
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
        /// Calculates the absolute difference between the earliest and latest values in the 
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
                    CreateErrorTagValue(bucket, bucket.UtcBucketStart, Resources.TagValue_ProcessedValue_NoData)
                };
            }

            var status = bucket.RawSamples.Any(x => x.Status != TagValueStatus.Good)
                    ? TagValueStatus.Uncertain
                    : TagValueStatus.Good;

            var firstValue = goodQualitySamples.First();
            var lastValue = goodQualitySamples.Last();
            var numericValue = Math.Abs(firstValue.Value.GetValueOrDefault(double.NaN) - lastValue.Value.GetValueOrDefault(double.NaN));

            return new[] {
                TagValueBuilder.Create()
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
            if (bucket.RawSamples.Count == 0) {
                return new[] {
                    TagValueBuilder.Create()
                        .WithUtcSampleTime(bucket.UtcBucketStart)
                        .WithValue(0d)
                        .WithUnits("%")
                        .WithStatus(TagValueStatus.Uncertain)
                        .WithBucketProperties(bucket)
                        .WithProperties(CreateXPoweredByProperty())
                        .Build()
                };
            }

            var sampleCount = bucket.RawSamples.Count;
            var percentGoodCount = bucket.RawSamples.Count(x => x.Status == TagValueStatus.Good);

            return new[] {
                TagValueBuilder.Create()
                    .WithUtcSampleTime(bucket.UtcBucketStart)
                    .WithValue((double) percentGoodCount / sampleCount * 100)
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
            if (bucket.RawSamples.Count == 0) {
                return new[] {
                    TagValueBuilder.Create()
                        .WithUtcSampleTime(bucket.UtcBucketStart)
                        .WithValue(0d)
                        .WithUnits("%")
                        .WithStatus(TagValueStatus.Uncertain)
                        .WithBucketProperties(bucket)
                        .WithProperties(CreateXPoweredByProperty())
                        .Build()
                };
            }

            var sampleCount = bucket.RawSamples.Count;
            var percentBadCount = bucket.RawSamples.Count(x => x.Status == TagValueStatus.Bad);

            return new[] {
                TagValueBuilder.Create()
                    .WithUtcSampleTime(bucket.UtcBucketStart)
                    .WithValue((double) percentBadCount / sampleCount * 100)
                    .WithUnits("%")
                    .WithStatus(TagValueStatus.Good)
                    .WithBucketProperties(bucket)
                    .WithProperties(CreateXPoweredByProperty())
                    .Build()
            };
        }

        #endregion

        #region [ Aggregation using Data Function Names ]

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
        /// <param name="scheduler">
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
            IBackgroundTaskService scheduler = null, 
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
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true
            });

            var funcs = new Dictionary<string, AggregateCalculator>();

            if (dataFunctions != null) {
                foreach (var item in dataFunctions) {
                    if (string.IsNullOrWhiteSpace(item)) {
                        continue;
                    }

                    if (s_defaultAggregatorMap.TryGetValue(item, out var func)) {
                        funcs[item] = func;
                    }
                    else if (_customAggregatorMap.TryGetValue(item, out func)) {
                        funcs[item] = func;
                    }
                }
            }

            if (funcs.Count == 0) {
                result.Writer.TryComplete();
                return result;
            }
            
            result.Writer.RunBackgroundOperation((ch, ct) => GetAggregatedValues(tag, utcStartTime, utcEndTime, sampleInterval, rawData, ch, funcs, ct), true, scheduler, cancellationToken);
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
        /// <param name="scheduler">
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
            IBackgroundTaskService scheduler = null,
            CancellationToken cancellationToken = default
        ) {
            if (rawData == null) {
                throw new ArgumentNullException(nameof(rawData));
            }

            var channel = Channel.CreateUnbounded<TagValueQueryResult>(new UnboundedChannelOptions() {
                AllowSynchronousContinuations = true,
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
                scheduler,
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
        /// <param name="scheduler">
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
            IBackgroundTaskService scheduler = null, 
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
                    scheduler,
                    cancellationToken
                );
            }

            var funcs = new Dictionary<string, AggregateCalculator>();

            if (dataFunctions != null) {
                foreach (var item in dataFunctions) {
                    if (string.IsNullOrWhiteSpace(item)) {
                        continue;
                    }

                    if (s_defaultAggregatorMap.TryGetValue(item, out var func)) {
                        funcs[item] = func;
                    }
                    else if (_customAggregatorMap.TryGetValue(item, out func)) {
                        funcs[item] = func;
                    }
                }
            }

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
            }, true, scheduler, cancellationToken);

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
        /// <param name="scheduler">
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
            IBackgroundTaskService scheduler = null,
            CancellationToken cancellationToken = default
        ) {
            if (rawData == null) {
                throw new ArgumentNullException(nameof(rawData));
            }

            var channel = Channel.CreateUnbounded<TagValueQueryResult>(new UnboundedChannelOptions() {
                AllowSynchronousContinuations = true,
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
                scheduler,
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
                    AddPreBucketSample(bucket, val.Value);
                    continue;
                }

                if (val.Value.UtcSampleTime >= bucket.UtcBucketEnd) {
                    // The sample we have just received is later than the end time for the current 
                    // bucket.

                    // Determine the pre-bucket samples that we need to copy to the new bucket e.g. 
                    // to help with interpolation.
                    var preBucketSamples = GetPreBucketSamples(bucket);

                    do {
                        // Emit values from the current bucket and create a new bucket.
                        await CalculateAndEmitBucketSamples(tag, bucket, resultChannel, funcs, utcStartTime, utcEndTime, cancellationToken).ConfigureAwait(false);
                        bucket = new TagValueBucket(bucket.UtcBucketEnd, bucket.UtcBucketEnd.Add(sampleInterval), utcStartTime, utcEndTime);

                        // Now, copy over the pre-bucket samples to the new bucket.
                        foreach (var item in preBucketSamples) {
                            AddPreBucketSample(bucket, item);
                        }
                    } while (val.Value.UtcSampleTime >= bucket.UtcBucketEnd);
                }

                // Add the sample to the bucket.
                if (val.Value.UtcSampleTime <= utcEndTime) {
                    bucket.RawSamples.Add(val.Value);
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

                // Determine the pre-bucket samples that we need to copy e.g. to help with interpolation.
                var preBucketSamples = GetPreBucketSamples(bucket);

                while (bucket.UtcBucketEnd < utcEndTime) {
                    bucket = new TagValueBucket(bucket.UtcBucketEnd, bucket.UtcBucketEnd.Add(sampleInterval), utcStartTime, utcEndTime);
                    if (bucket.UtcBucketEnd > utcEndTime) {
                        // New bucket would end after the query end time, so we don't need to 
                        // calculate for this bucket.
                        break;
                    }

                    foreach (var item in preBucketSamples) {
                        bucket.PreBucketSamples.Add(item);
                    }

                    await CalculateAndEmitBucketSamples(tag, bucket, resultChannel, funcs, utcStartTime, utcEndTime, cancellationToken).ConfigureAwait(false);
                }
            }
        }


        /// <summary>
        /// Adds a pre-bucket sample to a bucket. If two or more pre-bucket samples have already 
        /// been added, the earliest sample will be removed.
        /// </summary>
        /// <param name="bucket">
        ///   The bucket.
        /// </param>
        /// <param name="value">
        ///   The sample.
        /// </param>
        private static void AddPreBucketSample(TagValueBucket bucket, TagValueExtended value) {
            if (bucket.PreBucketSamples.Count >= 2) {
                bucket.PreBucketSamples.RemoveAt(0);
            }
            bucket.PreBucketSamples.Add(value);
        }


        /// <summary>
        /// Gets a set of up to two pre-bucket samples to include in the bucket after the specified 
        /// one. These can be used when e.g. interpolating a value before the first sample in the 
        /// next bucket.
        /// </summary>
        /// <param name="bucket">
        ///   The current bucket. The returned values should be assigned to <see cref="TagValueBucket.PreBucketSamples"/> 
        ///   collection in the next bucket.
        /// </param>
        /// <returns>
        ///   A collection of <see cref="TagValueExtended"/> objects.
        /// </returns>
        private static IEnumerable<TagValueExtended> GetPreBucketSamples(TagValueBucket bucket) {
            if (bucket.RawSamples.Count == 2) {
                return bucket.RawSamples.ToArray();
            }
            if (bucket.RawSamples.Count > 2) {
                return bucket.RawSamples.Reverse().Take(2).Reverse().ToArray();
            }

            return bucket.PreBucketSamples.Concat(bucket.RawSamples).Reverse().Take(2).Reverse().ToArray();
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

                foreach (var val in vals.Where(v => v.UtcSampleTime >= utcNotBefore && v.UtcSampleTime <= utcNotAfter)) {
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
            return AdapterProperty.Create(XPoweredByPropertyName, s_xPoweredByPropertyValue.Value);
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
            return TagValueBuilder.Create()
                .WithUtcSampleTime(sampleTime)
                .WithValue(Resources.TagValue_ProcessedValue_Error)
                .WithStatus(TagValueStatus.Bad)
                .WithError(error)
                .WithBucketProperties(bucket)
                .WithProperties(CreateXPoweredByProperty())
                .Build();
        }


        /// <summary>
        /// Gets the descriptors for the data functions supported by the <see cref="AggregationHelper"/>.
        /// </summary>
        /// <returns></returns>
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
