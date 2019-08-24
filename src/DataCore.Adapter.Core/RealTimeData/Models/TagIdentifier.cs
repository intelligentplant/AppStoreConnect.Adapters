using System;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Defines basic information for identifying a real-time data tag.
    /// </summary>
    public sealed class TagIdentifier : ITagIdentifier {

        /// <summary>
        /// The hash code for the tag identifier.
        /// </summary>
        private readonly int _hashCode;

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
            _hashCode = Id.ToUpperInvariant().GetHashCode();
        }


        /// <summary>
        /// Gets the hash code for the tag identifier.
        /// </summary>
        /// <returns>
        ///   The hash code.
        /// </returns>
        public override int GetHashCode() {
            return _hashCode;
        }


        /// <summary>
        /// Tests if this object is equal to another object.
        /// </summary>
        /// <param name="obj">
        ///   The object to test this object against.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the objects are equal, or <see langword="false"/> otherwise.
        /// </returns>
        public override bool Equals(object obj) {
            if (!(obj is TagIdentifier other)) {
                return false;
            }

            return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

    }
}
