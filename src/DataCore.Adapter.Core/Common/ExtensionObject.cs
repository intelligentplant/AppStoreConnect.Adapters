using System;
using System.Linq;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes a non-standard <see cref="Variant"/> value.
    /// </summary>
    public class ExtensionObject : IEquatable<ExtensionObject> {

        /// <summary>
        /// The URI that defines the extension object type.
        /// </summary>
        public Uri TypeId { get; }

        /// <summary>
        /// The content type for the <see cref="EncodedBody"/>.
        /// </summary>
        public string Encoding { get; }

        /// <summary>
        /// The encoded object body.
        /// </summary>
        public byte[] EncodedBody { get; }


        /// <summary>
        /// Creates a new <see cref="ExtensionObject"/> object.
        /// </summary>
        /// <param name="typeId">
        ///   The URI that defines the extension object type.
        /// </param>
        /// <param name="encoding">
        ///   The content type for the encoded object value.
        /// </param>
        /// <param name="encodedBody">
        ///   The encoded object value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoding"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encodedBody"/> is <see langword="null"/>.
        /// </exception>
        public ExtensionObject(Uri typeId, string encoding, byte[] encodedBody) {
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }
            if (!typeId.IsAbsoluteUri) {
                throw new ArgumentOutOfRangeException(nameof(typeId), typeId, SharedResources.Error_AbsoluteUriRequired);
            }
            TypeId = typeId.EnsurePathHasTrailingSlash();
            Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            EncodedBody = encodedBody ?? throw new ArgumentNullException(nameof(encodedBody));
        }


        /// <inheritdoc/>
        public override int GetHashCode() {
#if NETSTANDARD2_0 || NET46
            return HashGenerator.Combine(TypeId, Encoding, EncodedBody);
#else
            return HashCode.Combine(TypeId, Encoding, EncodedBody);
#endif
        }


        /// <inheritdoc/>
        public override bool Equals(object obj) {
            return obj is ExtensionObject eo 
                ? Equals(eo) 
                : false;
        }


        /// <inheritdoc/>
        public bool Equals(ExtensionObject other) {
            if (other == null) {
                return false;
            }

            return TypeId.Equals(other.TypeId) && 
                Encoding.Equals(other.Encoding, StringComparison.OrdinalIgnoreCase) && 
                EncodedBody.SequenceEqual(other.EncodedBody);
        }

    }
}
