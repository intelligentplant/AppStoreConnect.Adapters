using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Utilities {
    /// <summary>
    /// Utility class for performing data aggregation (e.g. if a data source does not natively 
    /// support aggregation).
    /// </summary>
    public static class AggregationHelper {

        #region [ Aggregation Helpers ]

        /// <summary>
        /// Aggregates data.
        /// </summary>
        /// <param name="tag">
        ///   The definition for the tag being aggregated.
        /// </param>
        /// <param name="utcStartTime">
        ///   The UTC end time for the aggregated data set.
        /// </param>
        /// <param name="utcEndTime">
        ///   The UTC end time for the aggregated data set.
        /// </param>
        /// <param name="sampleInterval">
        ///   The sample interval to use between aggregation calculations.
        /// </param>
        /// <param name="rawData">
        ///   The raw data to be aggregated.
        /// </param>
        /// <param name="dataFunction">
        ///   The aggregate name (for information purposes only).
        /// </param>
        /// <param name="aggregateFunc">
        ///   The aggregate function to use.
        /// </param>
        /// <returns>
        ///   A collection of aggregated values.
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="aggregateFunc"/> is <see langword="null"/>.
        /// </exception>
        private static IEnumerable<TagValue> GetAggregatedValues(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, IEnumerable<TagValue> rawData, string dataFunction, Func<TagDefinition, DateTime, IEnumerable<TagValue>, TagValue> aggregateFunc) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException(SharedResources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, nameof(utcStartTime));
            }
            if (sampleInterval <= TimeSpan.Zero) {
                throw new ArgumentException(SharedResources.Error_SampleIntervalMustBeGreaterThanZero, nameof(sampleInterval));
            }
            if (aggregateFunc == null) {
                throw new ArgumentNullException(nameof(aggregateFunc));
            }
            if (String.IsNullOrWhiteSpace(dataFunction)) {
                dataFunction = "UNKNOWN";
            }

            // Ensure that we are only working with non-null samples.
            var rawSamples = rawData?.Where(x => x != null).ToArray() ?? new TagValue[0];
            if (rawSamples.Length == 0) {
                return new TagValue[0];
            }

            // Set the initial list capacity based on the time range and sample interval.
            var capacity = (int) ((utcEndTime - utcStartTime).TotalMilliseconds / sampleInterval.TotalMilliseconds);
            var result = capacity > 0
                ? new List<TagValue>(capacity)
                : new List<TagValue>();

            // We'll use an aggregation bucket to keep track of the time period that we are calculating 
            // the next sample over, and the samples that will be used in the aggregation.
            var bucket = new TagValueBucket() {
                UtcStart = utcStartTime.Subtract(sampleInterval),
                UtcEnd = utcStartTime
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

            TagValue previousAggregatedValue = null;

            var sampleEnumerator = rawSamples.AsEnumerable().GetEnumerator();
            while (sampleEnumerator.MoveNext()) {
                var currentSample = sampleEnumerator.Current;

                // If we've moved past the requested end time, break from the loop.
                if (currentSample.UtcSampleTime > utcEndTime) {
                    break;
                }

                // If we've moved past the end of the bucket, calculate the aggregate for the bucket, 
                // move to the next bucket, and repeat this process until the end time for the bucket 
                // is greater than the time stamp for currentSample.
                //
                // This allows us to handle situations where we need to produce an aggregated value at 
                // a set interval, but there is a gap in raw data that is bigger than the required 
                // interval (e.g. if we are averaging over a 5 minute interval, but there is a gap of 
                // 30 minutes between raw samples).
                while (currentSample.UtcSampleTime >= bucket.UtcEnd) {
                    if (bucket.Samples.Count > 0) {
                        // There are samples in the bucket; calculate the aggregate value.
                        var val = aggregateFunc(tag, bucket.UtcEnd, bucket.Samples);
                        result.Add(val);
                        previousAggregatedValue = val;
                        bucket.Samples.Clear();
                    }
                    else if (previousAggregatedValue != null) {
                        // There are no samples in the current bucket, but we have a value from the 
                        // previous bucket that we can re-use.
                        var val = TagValueBuilder.Create()
                                    .WithUtcSampleTime(bucket.UtcEnd)
                                    .WithNumericValue(previousAggregatedValue.NumericValue)
                                    .WithTextValue(previousAggregatedValue.TextValue)
                                    .WithStatus(previousAggregatedValue.Status)
                                    .WithUnits(previousAggregatedValue.Units)
                                    .Build();
                            
                        result.Add(val);
                        previousAggregatedValue = val;
                    }

                    // Set the start/end time for the next bucket.
                    bucket.UtcStart = bucket.UtcEnd;
                    bucket.UtcEnd = bucket.UtcStart.Add(sampleInterval);
                }

                bucket.Samples.Add(currentSample);
            }

            // We have moved past utcEndTime in the raw data by this point.  If we have samples in the 
            // bucket, and we either haven't calculated a value yet, or the most recent value that we 
            // calculated has a time stamp less than utcEndTime, calculate a final sample for 
            // utcEndTime and add it to the result.
            if (bucket.Samples.Count > 0 && (result.Count == 0 || (result.Count > 0 && result.Last().UtcSampleTime < utcEndTime))) {
                var val = aggregateFunc(tag, utcEndTime, bucket.Samples);
                result.Add(val);
            }

            return result;
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
        ///   The values to calculate the average from.
        /// </param>
        /// <returns>
        ///   The new tag value.
        /// </returns>
        /// <remarks>
        ///   The status used is the worst-case of all of the <paramref name="bucket"/> values used in 
        ///   the calculation.
        /// </remarks>
        private static TagValue CalculateAverage(TagDefinition tag, TagValueBucket bucket) {
            var tagInfoSample = bucket.Samples.First();
            var numericValue = bucket.Samples.Min(x => x.NumericValue);
            var textValue = tag.GetTextValue(numericValue);
            var status = bucket.Samples.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return TagValueBuilder.Create()
                .WithUtcSampleTime(bucket.UtcEnd)
                .WithNumericValue(numericValue)
                .WithTextValue(textValue)
                .WithStatus(status)
                .WithUnits(tagInfoSample.Units)
                .Build();
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
        ///   The values to calculate the minimum value from.
        /// </param>
        /// <returns>
        ///   The new tag value.
        /// </returns>
        /// <remarks>
        ///   The status used is the worst-case of all of the <paramref name="bucket"/> values used in 
        ///   the calculation.
        /// </remarks>
        private static TagValue CalculateMinimum(TagDefinition tag, TagValueBucket bucket) {
            var tagInfoSample = bucket.Samples.First();
            var numericValue = bucket.Samples.Min(x => x.NumericValue);
            var textValue = tag.GetTextValue(numericValue);
            var status = bucket.Samples.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return TagValueBuilder.Create()
                .WithUtcSampleTime(bucket.UtcEnd)
                .WithNumericValue(numericValue)
                .WithTextValue(textValue)
                .WithStatus(status)
                .WithUnits(tagInfoSample.Units)
                .Build();
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
        ///   The values to calculate the maximum value from.
        /// </param>
        /// <returns>
        ///   The new tag value.
        /// </returns>
        /// <remarks>
        ///   The status used is the worst-case of all of the <paramref name="bucket"/> values used in 
        ///   the calculation.
        /// </remarks>
        private static TagValue CalculateMaximum(TagDefinition tag, TagValueBucket bucket) {
            var tagInfoSample = bucket.Samples.First();
            var numericValue = bucket.Samples.Max(x => x.NumericValue);
            var textValue = tag.GetTextValue(numericValue);
            var status = bucket.Samples.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return TagValueBuilder.Create()
                .WithUtcSampleTime(bucket.UtcEnd)
                .WithNumericValue(numericValue)
                .WithTextValue(textValue)
                .WithStatus(status)
                .WithUnits(tagInfoSample.Units)
                .Build();
        }

        #endregion

        #region [ Aggregation using Data Function Names ]

        public static ChannelReader<ProcessedTagValueQueryResult> GetAggregatedValues(TagDefinition tag, IEnumerable<string> dataFunctions, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, ChannelReader<TagValueQueryResult> rawData, CancellationToken cancellationToken = default) {
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

            var funcs = new Dictionary<string, Func<TagDefinition, TagValueBucket, TagValue>>();

            if (dataFunctions != null) {
                foreach (var item in dataFunctions) {
                    if (string.IsNullOrWhiteSpace(item)) {
                        continue;
                    }

                    if (string.Equals(item, DefaultDataFunctions.Average.Name, StringComparison.OrdinalIgnoreCase)) {
                        funcs[DefaultDataFunctions.Average.Name] = CalculateAverage;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Maximum.Name, StringComparison.OrdinalIgnoreCase)) {
                        funcs[DefaultDataFunctions.Maximum.Name] = CalculateMaximum;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Minimum.Name, StringComparison.OrdinalIgnoreCase)) {
                        funcs[DefaultDataFunctions.Minimum.Name] = CalculateMinimum;
                    }
                }
            }

            if (funcs.Count == 0) {
                result.Writer.TryComplete();
            }
            else {
                result.Writer.RunBackgroundOperation((ch, ct) => GetAggregatedValues(tag, utcStartTime, utcEndTime, sampleInterval, rawData, ch, funcs, ct), true, cancellationToken);
            }

            return result;
        }


        private static async Task GetAggregatedValues(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, ChannelReader<TagValueQueryResult> rawData, ChannelWriter<ProcessedTagValueQueryResult> resultChannel, IDictionary<string, Func<TagDefinition, TagValueBucket, TagValue>> funcs, CancellationToken cancellationToken) {
            var bucket = new TagValueBucket() {
                UtcStart = utcStartTime.Subtract(sampleInterval),
                UtcEnd = utcStartTime
            };

            while (await rawData.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!rawData.TryRead(out var val)) {
                    break;
                }
                if (val == null) {
                    continue;
                }

                if (val.Value.UtcSampleTime < bucket.UtcStart) {
                    continue;
                }

                if (val.Value.UtcSampleTime > bucket.UtcEnd) {
                    if (bucket.Samples.Count > 0) {
                        await CalculateAndEmitBucketSamples(tag, bucket, resultChannel, funcs, cancellationToken).ConfigureAwait(false);
                    }

                    bucket = new TagValueBucket() {
                        UtcStart = bucket.UtcEnd,
                        UtcEnd = bucket.UtcEnd.Add(sampleInterval)
                    };
                }

                if (val.Value.UtcSampleTime < utcEndTime) {
                    bucket.Samples.Add(val.Value);
                }
            }

            if (bucket.Samples.Count > 0) {
                await CalculateAndEmitBucketSamples(tag, bucket, resultChannel, funcs, cancellationToken).ConfigureAwait(false);
            }
        }


        private static async Task CalculateAndEmitBucketSamples(TagDefinition tag, TagValueBucket bucket, ChannelWriter<ProcessedTagValueQueryResult> resultChannel, IDictionary<string, Func<TagDefinition, TagValueBucket, TagValue>> funcs, CancellationToken cancellationToken) {
            foreach (var agg in funcs) {
                var val = agg.Value.Invoke(tag, bucket);
                if (val != null && await resultChannel.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                    resultChannel.TryWrite(new ProcessedTagValueQueryResult(tag.Id, tag.Name, val, agg.Key));
                }
            }
        }


        /// <summary>
        /// Gets the descriptors for the data functions supported by the <see cref="AggregationHelper"/>.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<DataFunctionDescriptor> GetSupportedDataFunctions() {
            return new[] {
                DefaultDataFunctions.Average,
                DefaultDataFunctions.Maximum,
                DefaultDataFunctions.Minimum
            };
        }

        #endregion

    }
}
