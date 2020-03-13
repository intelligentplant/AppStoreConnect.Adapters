using System;
using System.Collections.Generic;

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
        public IList<TagValueExtended> RawSamples { get; } = new List<TagValueExtended>();

        /// <summary>
        /// The last raw samples that were received prior to start of this bucket. Up to two 
        /// samples are provided. This is to allow aggregates that calculate using values on 
        /// either side of the bucket boundary (such as interpolation).
        /// </summary>
        public IList<TagValueExtended> PreBucketSamples { get; } = new List<TagValueExtended>();


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
