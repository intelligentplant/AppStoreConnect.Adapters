using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;

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
        private static IEnumerable<TagValue> GetAggregatedValues(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, IEnumerable<TagValue> rawData, string dataFunction, Func<TagDefinition, DateTime, IEnumerable<TagValue>, IEnumerable<TagValue>> aggregateFunc) {
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
            var rawSamples = rawData?.Where(x => x != null).ToArray() ?? Array.Empty<TagValue>();
            if (rawSamples.Length == 0) {
                return Array.Empty<TagValue>();
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
                        var vals = aggregateFunc(tag, bucket.UtcEnd, bucket.Samples);
                        foreach (var val in vals) {
                            result.Add(val);
                            previousAggregatedValue = val;
                        }
                        bucket.Samples.Clear();
                    }
                    else if (previousAggregatedValue != null) {
                        // There are no samples in the current bucket, but we have a value from the 
                        // previous bucket that we can re-use.
                        var val = TagValueBuilder.Create()
                                    .WithUtcSampleTime(bucket.UtcEnd)
                                    .WithValue(previousAggregatedValue.Value)
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
                var vals = aggregateFunc(tag, utcEndTime, bucket.Samples);
                foreach (var val in vals) {
                    result.Add(val);
                }
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
        ///   The calculated tag value.
        /// </returns>
        /// <remarks>
        ///   The status used is the worst-case of all of the <paramref name="bucket"/> values used in 
        ///   the calculation.
        /// </remarks>
        private static IEnumerable<TagValue> CalculateAverage(TagDefinition tag, TagValueBucket bucket) {
            if (bucket.Samples.Count == 0) {
                return Array.Empty<TagValue>();
            }

            var tagInfoSample = bucket.Samples.First();
            var numericValue = bucket.Samples.Min(x => x.Value.GetValueOrDefault(double.NaN));
            var status = bucket.Samples.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return new[] {
                TagValueBuilder.Create()
                    .WithUtcSampleTime(bucket.UtcEnd)
                    .WithValue(numericValue)
                    .WithStatus(status)
                    .WithUnits(tagInfoSample.Units)
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
        ///   The values to calculate the minimum value from.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        /// <remarks>
        ///   The status used is the worst-case of all of the <paramref name="bucket"/> values used in 
        ///   the calculation.
        /// </remarks>
        private static IEnumerable<TagValue> CalculateMinimum(TagDefinition tag, TagValueBucket bucket) {
            if (bucket.Samples.Count == 0) {
                return Array.Empty<TagValue>();
            }

            var tagInfoSample = bucket.Samples.First();
            var numericValue = bucket.Samples.Min(x => x.Value.GetValueOrDefault(double.NaN));
            var status = bucket.Samples.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return new[] {
                TagValueBuilder.Create()
                    .WithUtcSampleTime(bucket.UtcEnd)
                    .WithValue(numericValue)
                    .WithStatus(status)
                    .WithUnits(tagInfoSample.Units)
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
        ///   The values to calculate the maximum value from.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        /// <remarks>
        ///   The status used is the worst-case of all of the <paramref name="bucket"/> values used in 
        ///   the calculation.
        /// </remarks>
        private static IEnumerable<TagValue> CalculateMaximum(TagDefinition tag, TagValueBucket bucket) {
            if (bucket.Samples.Count == 0) {
                return Array.Empty<TagValue>();
            }

            var tagInfoSample = bucket.Samples.First();
            var numericValue = bucket.Samples.Max(x => x.Value.GetValueOrDefault(double.NaN));
            var status = bucket.Samples.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return new[] {
                TagValueBuilder.Create()
                    .WithUtcSampleTime(bucket.UtcEnd)
                    .WithValue(numericValue)
                    .WithStatus(status)
                    .WithUnits(tagInfoSample.Units)
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
        ///   The values to calculate the value from.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        private static IEnumerable<TagValue> CalculateCount(TagDefinition tag, TagValueBucket bucket) {
            var numericValue = bucket.Samples.Count();

            return new[] {
                TagValueBuilder.Create()
                    .WithUtcSampleTime(bucket.UtcEnd)
                    .WithValue(numericValue)
                    .WithStatus(TagValueStatus.Good)
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
        ///   The values to calculate the maximum value from.
        /// </param>
        /// <returns>
        ///   The calculated tag value.
        /// </returns>
        /// <remarks>
        ///   The status used is the worst-case of all of the <paramref name="bucket"/> values used in 
        ///   the calculation.
        /// </remarks>
        private static IEnumerable<TagValue> CalculateRange(TagDefinition tag, TagValueBucket bucket) {
            if (bucket.Samples.Count == 0) {
                return null;
            }

            var tagInfoSample = bucket.Samples.First();
            var minValue = bucket.Samples.Min(x => x.Value.GetValueOrDefault(double.NaN));
            var maxValue = bucket.Samples.Max(x => x.Value.GetValueOrDefault(double.NaN));
            var numericValue = Math.Abs(maxValue = minValue);

            var status = bucket.Samples.Aggregate(TagValueStatus.Good, (q, val) => val.Status < q ? val.Status : q); // Worst-case status

            return new[] {
                TagValueBuilder.Create()
                    .WithUtcSampleTime(bucket.UtcEnd)
                    .WithValue(numericValue)
                    .WithStatus(status)
                    .WithUnits(tagInfoSample.Units)
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
        public static ChannelReader<ProcessedTagValueQueryResult> GetAggregatedValues(TagDefinition tag, IEnumerable<string> dataFunctions, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, ChannelReader<TagValueQueryResult> rawData, IBackgroundTaskService scheduler, CancellationToken cancellationToken = default) {
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

            var funcs = new Dictionary<string, Func<TagDefinition, TagValueBucket, IEnumerable<TagValue>>>();

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
                    else if (string.Equals(item, DefaultDataFunctions.Count.Name, StringComparison.OrdinalIgnoreCase)) {
                        funcs[DefaultDataFunctions.Count.Name] = CalculateCount;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Range.Name, StringComparison.OrdinalIgnoreCase)) {
                        funcs[DefaultDataFunctions.Range.Name] = CalculateCount;
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
        public static ChannelReader<ProcessedTagValueQueryResult> GetAggregatedValues(IEnumerable<TagDefinition> tags, IEnumerable<string> dataFunctions, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, ChannelReader<TagValueQueryResult> rawData, IBackgroundTaskService scheduler, CancellationToken cancellationToken = default) {
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

            var funcs = new Dictionary<string, Func<TagDefinition, TagValueBucket, IEnumerable<TagValue>>>();

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
                    else if (string.Equals(item, DefaultDataFunctions.Count.Name, StringComparison.OrdinalIgnoreCase)) {
                        funcs[DefaultDataFunctions.Count.Name] = CalculateCount;
                    }
                    else if (string.Equals(item, DefaultDataFunctions.Range.Name, StringComparison.OrdinalIgnoreCase)) {
                        funcs[DefaultDataFunctions.Range.Name] = CalculateCount;
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


        private static async Task GetAggregatedValues(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, ChannelReader<TagValueQueryResult> rawData, ChannelWriter<ProcessedTagValueQueryResult> resultChannel, IDictionary<string, Func<TagDefinition, TagValueBucket, IEnumerable<TagValue>>> funcs, CancellationToken cancellationToken) {
            var bucket = new TagValueBucket() {
                UtcStart = utcStartTime.Subtract(sampleInterval),
                UtcEnd = utcStartTime
            };

            var iterations = 0;

            while (await rawData.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!rawData.TryRead(out var val)) {
                    break;
                }

                ++iterations;

                if (val == null) {
                    continue;
                }

                if (val.Value.UtcSampleTime < bucket.UtcStart) {
                    continue;
                }

                if (val.Value.UtcSampleTime > bucket.UtcEnd) {
                    await CalculateAndEmitBucketSamples(tag, bucket, resultChannel, funcs, cancellationToken).ConfigureAwait(false);
                    
                    // Calculate the start/end time for the new bucket that the sample we received 
                    // should go into.

                    var ticks = sampleInterval.Ticks * (val.Value.UtcSampleTime.Ticks / sampleInterval.Ticks);
                    var nextBucketStartTime = new DateTime(ticks, DateTimeKind.Utc);

                    bucket = new TagValueBucket() {
                        UtcStart = nextBucketStartTime,
                        UtcEnd = nextBucketStartTime.Add(sampleInterval)
                    };
                }

                if (val.Value.UtcSampleTime < utcEndTime) {
                    bucket.Samples.Add(val.Value);
                }
            }

            await CalculateAndEmitBucketSamples(tag, bucket, resultChannel, funcs, cancellationToken).ConfigureAwait(false);
        }


        private static async Task CalculateAndEmitBucketSamples(TagDefinition tag, TagValueBucket bucket, ChannelWriter<ProcessedTagValueQueryResult> resultChannel, IDictionary<string, Func<TagDefinition, TagValueBucket, IEnumerable<TagValue>>> funcs, CancellationToken cancellationToken) {
            foreach (var agg in funcs) {
                var vals = agg.Value.Invoke(tag, bucket);
                if (vals == null) {
                    continue;
                }
                foreach (var val in vals) {
                    if (val != null && await resultChannel.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                        resultChannel.TryWrite(ProcessedTagValueQueryResult.Create(tag.Id, tag.Name, val, agg.Key));
                    }
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
                DefaultDataFunctions.Minimum,
                DefaultDataFunctions.Count,
                DefaultDataFunctions.Range
            };
        }

        #endregion

    }
}
