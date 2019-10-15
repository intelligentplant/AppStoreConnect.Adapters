using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a value returned by a tag value query.
    /// </summary>
    public class TagValueQueryResult: TagDataContainer {

        /// <summary>
        /// The tag value.
        /// </summary>
        [Required]
        public TagValue Value { get; set; }


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
        public static TagValueQueryResult Create(string tagId, string tagName, TagValue value) {
            return new TagValueQueryResult() {
                TagId = tagId ?? throw new ArgumentNullException(nameof(tagId)),
                TagName = tagName ?? throw new ArgumentNullException(nameof(tagName)),
                Value = value ?? throw new ArgumentNullException(nameof(value))
            };
        }

    }


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
        public static ProcessedTagValueQueryResult Create(string tagId, string tagName, TagValue value, string dataFunction) {
            return new ProcessedTagValueQueryResult() {
                TagId = tagId ?? throw new ArgumentNullException(nameof(tagId)),
                TagName = tagName ?? throw new ArgumentNullException(nameof(tagName)),
                Value = value ?? throw new ArgumentNullException(nameof(value)),
                DataFunction = dataFunction ?? throw new ArgumentNullException(nameof(dataFunction))
            };
            
        }

    }

}
