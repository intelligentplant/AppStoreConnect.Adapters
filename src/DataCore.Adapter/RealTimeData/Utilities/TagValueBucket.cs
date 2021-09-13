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
        /// The raw data samples for the bucket
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
        public BoundaryInfo StartBoundary { get; } = new BoundaryInfo();

        /// <summary>
        /// Holds information about values immediately before the end boundary of the bucket.
        /// </summary>
        public BoundaryInfo EndBoundary { get; } = new BoundaryInfo();

        /// <summary>
        /// The <see cref="TagValueStatusCodeFlags"/> to set on values calculated or selected from 
        /// the bucket.
        /// </summary>
        public TagValueStatusCodeFlags InfoBits => UtcBucketEnd > UtcQueryEnd
            ? TagValueStatusCodeFlags.Partial
            : TagValueStatusCodeFlags.None;


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
        /// Adds a raw sample to the bucket and updates the end boundary value for the bucket if 
        /// required.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        internal void AddRawSample(TagValueExtended value) {
            if (value == null) {
                return;
            }

            _rawSamples.Add(value);
            EndBoundary.UpdateValue(value);
        }


        /// <summary>
        /// Updates the start boundary value for the bucket. Note that updating the start boundary 
        /// will also update the end boundary, if an end boundary value has not yet been set. 
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        internal void UpdateStartBoundaryValue(TagValueExtended value) {
            if (value == null) {
                return;
            }

            StartBoundary.UpdateValue(value);
            // We may also have to update the end boundary value.
            EndBoundary.UpdateValue(value);
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
