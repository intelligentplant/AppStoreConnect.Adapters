using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes a value returned by a tag value query.
    /// </summary>
    public class TagValueQueryResult: TagDataContainer {

        /// <summary>
        /// The tag value.
        /// </summary>
        public TagValue Value { get; }


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
        public TagValueQueryResult(string tagId, string tagName, TagValue value): base(tagId, tagName) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

    }


    /// <summary>
    /// Describes a value returned by a tag value query for processed data.
    /// </summary>
    public class ProcessedTagValueQueryResult : TagValueQueryResult {

        /// <summary>
        /// The data function used to aggregate the tag value.
        /// </summary>
        public string DataFunction { get; }


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
        public ProcessedTagValueQueryResult(string tagId, string tagName, TagValue value, string dataFunction) : base(tagId, tagName, value) {
            DataFunction = dataFunction ?? throw new ArgumentNullException(nameof(dataFunction));
        }

    }

}
