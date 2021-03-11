using System;
using System.Linq;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes a non-standard <see cref="Variant"/> value.
    /// </summary>
    /// <remarks>
    ///   Use the <see cref="Create{T}(T, IObjectEncoder)"/> and <see cref="Create{T}(Uri, T, IObjectEncoder)"/> 
    ///   methods to create <see cref="EncodedObject"/> instances from objects, or the <see cref="Create(Uri, string, byte[])"/> 
    ///   method to create an <see cref="EncodedObject"/> directly from a byte array containing 
    ///   the serialized object data.
    /// </remarks>
    public class EncodedObject : IEquatable<EncodedObject> {

        /// <summary>
        /// The ID that defines the extension object type.
        /// </summary>
        public Uri TypeId { get; }

        /// <summary>
        /// The content type for the <see cref="EncodedBody"/>.
        /// </summary>
        public string Encoding { get; }

        /// <summary>
        /// The serialized object body, encoded as a base64 string.
        /// </summary>
        public string EncodedBody { get; }


        /// <summary>
        /// Creates a new <see cref="EncodedObject"/> object.
        /// </summary>
        /// <param name="typeId">
        ///   The URI that defines the extension object type.
        /// </param>
        /// <param name="encoding">
        ///   The content type for the encoded object value.
        /// </param>
        /// <param name="encodedBody">
        ///   The serialized object value, encoded as a base64 string.
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
        /// <remarks>
        ///   This constructor is intended for infrastructure use only. Use the static <see cref="Create{T}(T, IObjectEncoder)"/>, 
        ///   <see cref="Create{T}(Uri, T, IObjectEncoder)"/> or <see cref="Create(Uri, string, byte[])"/> 
        ///   methods instead.
        /// </remarks>
        public EncodedObject(Uri typeId, string encoding, string encodedBody) {
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
            return obj is EncodedObject eo 
                ? Equals(eo) 
                : false;
        }


        /// <inheritdoc/>
        public bool Equals(EncodedObject other) {
            if (other == null) {
                return false;
            }

            return TypeId.Equals(other.TypeId) && 
                Encoding.Equals(other.Encoding, StringComparison.OrdinalIgnoreCase) && 
                EncodedBody.SequenceEqual(other.EncodedBody);
        }


        /// <summary>
        /// Converts the <see cref="EncodedBody"/> from a base64-encoded string into a byte array.
        /// </summary>
        /// <returns>
        ///   A byte array containing the <see cref="EncodedBody"/>.
        /// </returns>
        public byte[] ToByteArray() {
            return Convert.FromBase64String(EncodedBody);
        }


        /// <summary>
        /// Creates a new <see cref="EncodedObject"/> using the specified type ID and value.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the object to encode.
        /// </typeparam>
        /// <param name="typeId">
        ///   The type ID of the object being encoded.
        /// </param>
        /// <param name="value">
        ///   The value to encode.
        /// </param>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/> to encode the <paramref name="value"/> with.
        /// </param>
        /// <returns>
        ///   A new <see cref="EncodedObject"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        public static EncodedObject Create<T>(Uri typeId, T? value, IObjectEncoder encoder) {
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }
            if (!typeId.IsAbsoluteUri) {
                throw new ArgumentOutOfRangeException(nameof(typeId), typeId, SharedResources.Error_AbsoluteUriRequired);
            }
            if (encoder == null) {
                throw new ArgumentNullException(nameof(encoder));
            }

            return encoder.Encode(typeId, value);
        }


        /// <summary>
        /// Creates a new <see cref="EncodedObject"/> using the value.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the value to encode. The type must be registered in <see cref="TypeLibrary"/>.
        /// </typeparam>
        /// <param name="value">
        ///   The value to encode.
        /// </param>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/> to encode the <paramref name="value"/> with.
        /// </param>
        /// <returns>
        ///   A new <see cref="EncodedObject"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <typeparamref name="T"/> is not registered with <see cref="TypeLibrary"/>.
        /// </exception>
        public static EncodedObject Create<T>(T? value, IObjectEncoder encoder) {
            if (encoder == null) {
                throw new ArgumentNullException(nameof(encoder));
            }

            return encoder.Encode(value);
        }


        /// <summary>
        /// Creates a new <see cref="EncodedObject"/> using a <see cref="byte"/> array that 
        /// already contains the encoded object.
        /// </summary>
        /// <param name="typeId">
        ///   The type ID of the encoded object.
        /// </param>
        /// <param name="encoding">
        ///   The encoding that was used.
        /// </param>
        /// <param name="value">
        ///   The encoded object.
        /// </param>
        /// <returns>
        ///   A new <see cref="EncodedObject"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoding"/> is <see langword="null"/>.
        /// </exception>
        public static EncodedObject Create(Uri typeId, string encoding, byte[]? value) {
            return new EncodedObject(typeId, encoding, Convert.ToBase64String(value ?? Array.Empty<byte>()));
        }

    }
}
