using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using DataCore.Adapter.Tags;

namespace DataCore.Adapter.RealTimeData {
    /// <summary>
    /// Describes a value returned by a tag value query for processed data.
    /// </summary>
    public class ProcessedTagValueQueryResult : TagValueQueryResult {

        /// <summary>
        /// The data function used to aggregate the tag value.
        /// </summary>
        [Required]
        public string DataFunction { get; set; }


        /// <summary>
        /// Creates a new <see cref="ProcessedTagValueQueryResult"/> object.
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
        /// <param name="dataFunction">
        ///   The data function used to aggregate the tag value.
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
        [JsonConstructor]
        public ProcessedTagValueQueryResult(
            string tagId,
            string tagName,
            TagValueExtended value,
            string dataFunction
        ) : base(tagId, tagName, value) {
            DataFunction = dataFunction ?? throw new ArgumentNullException(nameof(dataFunction));
        }


        /// <summary>
        /// Creates a new <see cref="ProcessedTagValueQueryResult"/> object.
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
        /// <param name="dataFunction">
        ///   The data function used to aggregate the tag value.
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
        public static ProcessedTagValueQueryResult Create(string tagId, string tagName, TagValueExtended value, string dataFunction) {
            return new ProcessedTagValueQueryResult(tagId, tagName, value, dataFunction);
        }


        /// <summary>
        /// Creates a new <see cref="ProcessedTagValueQueryResult"/> object.
        /// </summary>
        /// <param name="tagIdentifier">
        ///   The tag identifier.
        /// </param>
        /// <param name="value">
        ///   The tag value.
        /// </param>
        /// <param name="dataFunction">
        ///   The data function used to aggregate the tag value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagIdentifier"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public static ProcessedTagValueQueryResult Create(TagIdentifier tagIdentifier, TagValueExtended value, string dataFunction) {
            return new ProcessedTagValueQueryResult(tagIdentifier?.Id!, tagIdentifier?.Name!, value, dataFunction);
        }

    }

}
