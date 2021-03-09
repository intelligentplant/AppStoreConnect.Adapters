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
        public ExtensionObject(Uri typeId, string encoding, byte[] encodedBody) {
            TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
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
