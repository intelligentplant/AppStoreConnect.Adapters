using System;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Tags;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a value returned by a tag value query.
    /// </summary>
    public class TagValueQueryResult : TagDataContainer {

        /// <summary>
        /// The tag value.
        /// </summary>
        [Required]
        public TagValueExtended Value { get; set; }


        /// <summary>
        /// Creates a new <see cref="TagValueQueryResult"/> object.
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
        public TagValueQueryResult(string tagId, string tagName, TagValueExtended value)
            : base(tagId, tagName) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }


        /// <summary>
        /// Creates a new <see cref="TagValueQueryResult"/> object.
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
        public static TagValueQueryResult Create(string tagId, string tagName, TagValueExtended value) {
            return new TagValueQueryResult(tagId, tagName, value);
        }


        /// <summary>
        /// Creates a new <see cref="TagValueQueryResult"/> object.
        /// </summary>
        /// <param name="tagIdentifier">
        ///   The tag identifier.
        /// </param>
        /// <param name="value">
        ///   The tag value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagIdentifier"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueQueryResult Create(TagIdentifier tagIdentifier, TagValueExtended value) {
            return new TagValueQueryResult(tagIdentifier?.Id!, tagIdentifier?.Name!, value);
        }

    }

}
