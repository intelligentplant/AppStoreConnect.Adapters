using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes a tag value pushed from an adapter to its observers.
    /// </summary>
    public sealed class PushedTagValue : TagDataContainer {

        /// <summary>
        /// The ID of the adapter that emitted the tag value.
        /// </summary>
        public string AdapterId { get; }

        /// <summary>
        /// The tag value.
        /// </summary>
        public TagValue Value { get; }


        /// <summary>
        /// Creates a new <see cref="PushedTagValue"/> object.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter that emitted the tag value.
        /// </param>
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
        ///   <paramref name="adapterId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public PushedTagValue(string adapterId, string tagId, string tagName, TagValue value) : base(tagId, tagName) {
            AdapterId = adapterId ?? throw new ArgumentNullException(nameof(adapterId));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

    }
}
