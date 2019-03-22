using System;
using System.Collections.Generic;
using System.Text;
using DataCore.Adapter.DataSource.Models;

namespace DataCore.Adapter.DataSource.Utilities {

    /// <summary>
    /// Holds samples for an aggregation bucket.
    /// </summary>
    internal class TagValueBucket {

        /// <summary>
        /// Gets or sets the UTC start time for the bucket.
        /// </summary>
        public DateTime UtcStart { get; set; }

        /// <summary>
        /// Gets or sets the UTC end time for the bucket.
        /// </summary>
        public DateTime UtcEnd { get; set; }

        /// <summary>
        /// The data samples in the bucket.
        /// </summary>
        private readonly List<TagValue> _samples = new List<TagValue>();

        /// <summary>
        /// Gets the data samples for the bucket.
        /// </summary>
        public ICollection<TagValue> Samples { get { return _samples; } }


        /// <summary>
        /// Gets a string representation of the bucket.
        /// </summary>
        /// <returns>
        /// A string represntation of the bucket.
        /// </returns>
        public override string ToString() {
            return $"{{ UtcStart = {UtcStart:yyyy-MM-ddTHH:mm:ss.fffZ}, UtcEnd = {UtcEnd:yyyy-MM-ddTHH:mm:ss.fffZ}, Sample Count = {Samples.Count} }}";
        }

    }
}
