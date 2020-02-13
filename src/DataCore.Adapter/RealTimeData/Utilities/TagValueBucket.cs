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
        public DateTime UtcStart { get; }

        /// <summary>
        /// The UTC end time for the bucket.
        /// </summary>
        public DateTime UtcEnd { get; }

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
        /// <param name="utcStart">
        ///   The UTC start time for the bucket.
        /// </param>
        /// <param name="utcEnd">
        ///   The UTC end time for the bucket.
        /// </param>
        public TagValueBucket(DateTime utcStart, DateTime utcEnd) {
            UtcStart = utcStart;
            UtcEnd = utcEnd;
        }


        /// <summary>
        /// Gets a string representation of the bucket.
        /// </summary>
        /// <returns>
        /// A string represntation of the bucket.
        /// </returns>
        public override string ToString() {
            return $"{{ UtcStart = {UtcStart:yyyy-MM-ddTHH:mm:ss.fffZ}, UtcEnd = {UtcEnd:yyyy-MM-ddTHH:mm:ss.fffZ}, Raw Sample Count = {RawSamples.Count} }}";
        }

    }
}
