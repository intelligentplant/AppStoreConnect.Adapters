using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.DataSource.Models {

    /// <summary>
    /// Describes the results of a historical data query on a tag.
    /// </summary>
    public sealed class HistoricalTagValues: TagDataContainer {

        /// <summary>
        /// The tag values.
        /// </summary>
        public IEnumerable<TagValue> Values { get; }


        /// <summary>
        /// Creates a new <see cref="HistoricalTagValues"/> object.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="tagName">
        ///   The tag name.
        /// </param>
        /// <param name="values">
        ///   The tag values.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagName"/> is <see langword="null"/>.
        /// </exception>
        public HistoricalTagValues(string tagId, string tagName, IEnumerable<TagValue> values): base(tagId, tagName) {
            Values = values?.ToArray() ?? new TagValue[0];
        }

    }
}
