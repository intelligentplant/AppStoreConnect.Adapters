using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Tags;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Holds samples for an aggregation bucket.
    /// </summary>
    public class TagValueBucket {

        /// <summary>
        /// The UTC start time for the bucket.
        /// </summary>
        public DateTime UtcBucketStart { get; }

        /// <summary>
        /// The UTC end time for the bucket.
        /// </summary>
        public DateTime UtcBucketEnd { get; }

        /// <summary>
        /// The overall UTC start time for the query.
        /// </summary>
        public DateTime UtcQueryStart { get; }

        /// <summary>
        /// The overall UTC end time for the query.
        /// </summary>
        public DateTime UtcQueryEnd { get; }

        /// <summary>
        /// The raw data samples for the bucket.
        /// </summary>
        private List<TagValueExtended> _rawSamples = new List<TagValueExtended>();

        /// <summary>
        /// The raw data samples for the bucket.
        /// </summary>
        public IEnumerable<TagValueExtended> RawSamples { get { return _rawSamples; } }

        /// <summary>
        /// The number of raw samples in the bucket.
        /// </summary>
        public int RawSampleCount { get { return _rawSamples.Count; } }

        /// <summary>
        /// Holds information about values immediately before the start boundary of the bucket.
        /// </summary>
        public PreBoundaryInfo BeforeStartBoundary { get; } = new PreBoundaryInfo();

        /// <summary>
        /// Holds information about values immediately after the start boundary of the bucket.
        /// </summary>
        public PostBoundaryInfo AfterStartBoundary { get; } = new PostBoundaryInfo();

        /// <summary>
        /// Holds information about values immediately before the end boundary of the bucket.
        /// </summary>
        public PreBoundaryInfo BeforeEndBoundary { get; } = new PreBoundaryInfo();

        /// <summary>
        /// Holds information about values immediately after the end boundary of the bucket.
        /// </summary>
        public PostBoundaryInfo AfterEndBoundary { get; } = new PostBoundaryInfo();


        /// <summary>
        /// Creates a new <see cref="TagValueBucket"/> object.
        /// </summary>
        /// <param name="utcBucketStart">
        ///   The UTC start time for the bucket.
        /// </param>
        /// <param name="utcBucketEnd">
        ///   The UTC end time for the bucket.
        /// </param>
        /// <param name="utcQueryStart">
        ///   The overall UTC start time for the query.
        /// </param>
        /// <param name="utcQueryEnd">
        ///   The overall UTC end time for the query.
        /// </param>
        public TagValueBucket(DateTime utcBucketStart, DateTime utcBucketEnd, DateTime utcQueryStart, DateTime utcQueryEnd) {
            UtcBucketStart = utcBucketStart;
            UtcBucketEnd = utcBucketEnd;
            UtcQueryStart = utcQueryStart;
            UtcQueryEnd = utcQueryEnd;
        }


        /// <summary>
        /// Adds a raw sample to the bucket.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        internal void AddRawSample(TagValueExtended? value) {
            if (value == null) {
                return;
            }

            if (value.UtcSampleTime < UtcBucketStart) {
                BeforeStartBoundary.UpdateValue(value);
            }
            else if (value.UtcSampleTime >= UtcBucketStart && value.UtcSampleTime < UtcBucketEnd) {
                _rawSamples.Add(value);
                AfterStartBoundary.UpdateValue(value);
                BeforeEndBoundary.UpdateValue(value);
            }
            else {
                AfterEndBoundary.UpdateValue(value);
            }
        }


        /// <summary>
        /// Copies boundary samples from the specified bucket into the current bucket.
        /// </summary>
        /// <param name="previousBucket">
        ///   The bucket to copy the boundary samples from.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="previousBucket"/> is <see langword="null"/>.
        /// </exception>
        internal void AddBoundarySamples(TagValueBucket previousBucket) {
            if (previousBucket == null) {
                throw new ArgumentNullException(nameof(previousBucket));
            }

            if (previousBucket.BeforeEndBoundary.BoundaryStatus == TagValueStatus.Good) {
                // Good boundary status means that we have a best-quality value and a closest
                // value defined, and that these both point to the same sample.
                AddRawSample(previousBucket.BeforeEndBoundary?.BestQualityValue);
            }
            else {
                // Non-good boundary status means that the best-quality value and the closest
                // value are different, or that one or both of these values is null.
                AddRawSample(previousBucket.BeforeEndBoundary?.BestQualityValue);
                AddRawSample(previousBucket.BeforeEndBoundary?.ClosestValue);
            }

            if (previousBucket.AfterEndBoundary.BoundaryStatus == TagValueStatus.Good) {
                // Good boundary status means that we have a best-quality value and a closest
                // value defined, and that these both point to the same sample.
                AddRawSample(previousBucket.AfterEndBoundary?.BestQualityValue);
            }
            else {
                // Non-good boundary status means that the best-quality value and the closest
                // value are different, or that one or both of these values is null.
                AddRawSample(previousBucket.AfterEndBoundary?.ClosestValue);
                AddRawSample(previousBucket.AfterEndBoundary?.BestQualityValue);
            }
        }


        /// <summary>
        /// Gets a string representation of the bucket.
        /// </summary>
        /// <returns>
        /// A string represntation of the bucket.
        /// </returns>
        public override string ToString() {
            return $"{{ Bucket Start: {UtcBucketStart:yyyy-MM-ddTHH:mm:ss.fffZ}, Bucket End: {UtcBucketEnd:yyyy-MM-ddTHH:mm:ss.fffZ} }}";
        }

    }
}
