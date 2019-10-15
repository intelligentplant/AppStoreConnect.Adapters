using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes summary information about a tag.
    /// </summary>
    public class TagSummary : TagIdentifier {

        /// <inheritdoc/>
        public string Description { get; set; }

        /// <inheritdoc/>
        public string Units { get; set; }


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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public static TagSummary Create(string id, string name, string description, string units) {
            return new TagSummary() {
                Id = id ?? throw new ArgumentNullException(nameof(id)),
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                Description = description ?? string.Empty,
                Units = units ?? string.Empty
            };
        }

    }
}
