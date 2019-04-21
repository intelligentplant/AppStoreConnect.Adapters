using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes the snapshot value for a tag.
    /// </summary>
    public sealed class SnapshotTagValue: TagDataContainer {

        /// <summary>
        /// The tag value.
        /// </summary>
        public TagValue Value { get; }


        /// <summary>
        /// Creates a new <see cref="SnapshotTagValue"/> object.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="tagName">
        ///   The tag name.
        /// </param>
        /// <param name="value">
        ///   The tag value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public SnapshotTagValue(string tagId, string tagName, TagValue value): base(tagId, tagName) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

    }

}
