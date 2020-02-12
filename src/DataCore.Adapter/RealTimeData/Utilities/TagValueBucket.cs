using System;
using System.Collections.Generic;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Holds samples for an aggregation bucket.
    /// </summary>
    public class TagValueBucket {

        /// <summary>
        /// Gets or sets the UTC start time for the bucket.
        /// </summary>
        public DateTime UtcStart { get; }

        /// <summary>
        /// Gets or sets the UTC end time for the bucket.
        /// </summary>
        public DateTime UtcEnd { get; }

        /// <summary>
        /// Gets the raw data samples for the bucket.
        /// </summary>
        public IList<TagValueExtended> RawSamples { get; } = new List<TagValueExtended>();

        /// <summary>
        /// Gets the raw samples that were received prior to start of this bucket. This is to 
        /// allow aggregates that calculate across bucket boundaries (e.g. interpolation) to do so.
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
