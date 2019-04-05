using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Defines basic information for identifying a real-time data tag.
    /// </summary>
    public class TagIdentifier {

        /// <summary>
        /// The unique identifier for the tag.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The tag name.
        /// </summary>
        public string Name { get; }


        /// <summary>
        /// Creates a new <see cref="TagIdentifier"/> object.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier for the tag.
        /// </param>
        /// <param name="name">
        ///   The tag name.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public TagIdentifier(string id, string name) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

    }
}
