using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Describes a data reference on an <see cref="AssetModelNode"/>. Note that the reference can 
    /// be to a tag on a different adapter.
    /// </summary>
    public class DataReference {

        /// <summary>
        /// The adapter ID for the reference.
        /// </summary>
        public string AdapterId { get; }

        /// <summary>
        /// The tag name or ID for the reference.
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// The display name for the reference.
        /// </summary>
        [MaxLength(100)]
        public string? Name { get; }


        /// <summary>
        /// Creates a new <see cref="DataReference"/> object.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID for the data reference.
        /// </param>
        /// <param name="tag">
        ///   The tag name or ID for the data reference.
        /// </param>
        /// <param name="name">
        ///   The display name for the data reference.
        /// </param>
        [JsonConstructor]
        public DataReference(string adapterId, string tag, string? name) {
            AdapterId = adapterId ?? throw new ArgumentNullException(nameof(adapterId));
            Tag = tag ?? throw new ArgumentNullException(nameof(tag));
            Name = name;
        }


        /// <summary>
        /// Creates a new <see cref="DataReference"/> object.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID for the data reference.
        /// </param>
        /// <param name="tag">
        ///   The tag name or ID for the data reference.
        /// </param>
        public DataReference(string adapterId, string tag) 
            : this(adapterId, tag, null) { }

    }

}
