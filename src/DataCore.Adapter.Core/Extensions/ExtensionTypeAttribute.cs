using System;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Marks a type as an extension type used in a non-standard adapter feature.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class ExtensionTypeAttribute : Attribute {

        /// <summary>
        /// The type URI.
        /// </summary>
        public Uri Uri { get; }


        /// <summary>
        /// Creates a new <see cref="ExtensionTypeAttribute"/> object.
        /// </summary>
        /// <param name="uri">
        ///   The URI for the extension type.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uri"/> is not a valid URI.
        /// </exception>
        public ExtensionTypeAttribute(string uri) {
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var u)) {
                throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(uri));
            }
            Uri = u;
        }

    }
}
