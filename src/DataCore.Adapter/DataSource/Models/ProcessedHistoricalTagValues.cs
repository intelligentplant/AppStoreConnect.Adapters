using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DataCore.Adapter.DataSource.Models {

    /// <summary>
    /// Describes the result of a request for aggregated data on a tag.
    /// </summary>
    public sealed class ProcessedHistoricalTagValues: TagDataContainer {

        /// <summary>
        /// The query results, indexed by data function name.
        /// </summary>
        public IDictionary<string, IEnumerable<TagValue>> Values { get; }


        /// <summary>
        /// Creates a new <see cref="ProcessedHistoricalTagValues"/> object.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="tagName">
        ///   The tag name.
        /// </param>
        /// <param name="values">
        ///   The tag values, indexed by data function name.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagName"/> is <see langword="null"/>.
        /// </exception>
        public ProcessedHistoricalTagValues(string tagId, string tagName, IDictionary<string, IEnumerable<TagValue>> values) : base(tagId, tagName) {
            Values = values == null
                ? new ReadOnlyDictionary<string, IEnumerable<TagValue>>(new Dictionary<string, IEnumerable<TagValue>>())
                : new ReadOnlyDictionary<string, IEnumerable<TagValue>>(values);
        }

    }
}
