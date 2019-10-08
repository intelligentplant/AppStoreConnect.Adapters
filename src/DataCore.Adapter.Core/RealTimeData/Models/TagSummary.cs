using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes summary information about a tag.
    /// </summary>
    public class TagSummary : TagIdentifier, ITagSummary {

        /// <inheritdoc/>
        public string Category { get; }

        /// <inheritdoc/>
        public string Description { get; }

        /// <inheritdoc/>
        public string Units { get; }


        /// <summary>
        /// Creates a new <see cref="TagSummary"/> object.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier for the tag.
        /// </param>
        /// <param name="name">
        ///   The tag name.
        /// </param>
        /// <param name="category">
        ///   The tag measurement category (temperature, pressure, mass, etc).
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
        public TagSummary(string id, string name, string category, string description, string units)
            : base(id, name) {
            Category = category ?? string.Empty;
            Description = description ?? string.Empty;
            Units = units ?? string.Empty;
        }

    }
}
