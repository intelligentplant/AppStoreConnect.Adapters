using System;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes summary information about a tag.
    /// </summary>
    public class TagSummary : TagIdentifier {

        /// <summary>
        /// The tag description.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// The tag units.
        /// </summary>
        public string? Units { get; }

        /// <summary>
        /// The tag data type.
        /// </summary>
        public VariantType DataType { get; }


        /// <summary>
        /// Creates a new <see cref="TagSummary"/> object.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier for the tag.
        /// </param>
        /// <param name="name">
        ///   The tag name.
        /// </param>
        /// <param name="description">
        ///   The tag description.
        /// </param>
        /// <param name="units">
        ///   The tag units.
        /// </param>
        /// <param name="dataType">
        ///   The tag data type.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public TagSummary(string id, string name, string? description, string? units, VariantType dataType) 
            : base(id, name) {
            Description = description ?? string.Empty;
            Units = units ?? string.Empty;
            DataType = dataType;
        }


        /// <summary>
        /// Creates a new <see cref="TagSummary"/> object.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier for the tag.
        /// </param>
        /// <param name="name">
        ///   The tag name.
        /// </param>
        /// <param name="description">
        ///   The tag description.
        /// </param>
        /// <param name="units">
        ///   The tag units.
        /// </param>
        /// <param name="dataType">
        ///   The tag data type.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public static TagSummary Create(string id, string name, string? description, string? units, VariantType dataType) {
            return new TagSummary(id, name, description, units, dataType);
        }


        /// <summary>
        /// Creates a new <see cref="TagSummary"/> object that is a copy of an existing instance.
        /// </summary>
        /// <param name="other">
        ///   The <see cref="TagSummary"/> to copy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="other"/> is <see langword="null"/>.
        /// </exception>
        /// <returns>
        ///   A new <see cref="TagSummary"/> instance.
        /// </returns>
        public static TagSummary FromExisting(TagSummary other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            return Create(other.Id, other.Name, other.Description, other.Units, other.DataType);
        }

    }
}
