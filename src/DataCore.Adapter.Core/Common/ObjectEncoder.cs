using System;
using System.Globalization;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Base implementation for <see cref="IObjectEncoder"/>.
    /// </summary>
    public abstract class ObjectEncoder : IObjectEncoder {

        /// <summary>
        /// The encoding type for the encoder.
        /// </summary>
        public abstract string EncodingType { get; }


        /// <inheritdoc/>
        bool IObjectEncoder.CanEncode(Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            return CanEncode(type);
        }


        /// <summary>
        /// Tests if the encoder can encode the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">
        ///   The type of the object to be encoded.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type can be encoded, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        protected virtual bool CanEncode(Type type) {
            return type != null;
        }


        /// <inheritdoc/>
        byte[]? IObjectEncoder.Encode(Type type, object? value) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            
            if (value == null) {
                return Array.Empty<byte>();
            }

            if (!CanEncode(type)) {
                throw new ArgumentOutOfRangeException(nameof(value), value, SharedResources.Error_CannotEncodeType);
            }

            return Encode(value, type);
        }


        /// <summary>
        /// Encodes an object instance to an <see cref="EncodedObject"/>.
        /// </summary>
        /// <param name="value">
        ///   The value to be encoded.
        /// </param>
        /// <param name="type">
        ///   The type of the value being encoded.
        /// </param>
        /// <returns>
        ///   The encoded object.
        /// </returns>
        protected abstract byte[]? Encode(object? value, Type type);


        /// <inheritdoc/>
        bool IObjectEncoder.CanDecode(Type type, string encoding) {
            if (type == null) {
                return false;
            }
            if (!string.Equals(encoding, EncodingType, StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
            return CanDecode(type);
        }


        /// <summary>
        /// Tests if the encoder can decode an <see cref="EncodedObject"/> to the specified type.
        /// </summary>
        /// <param name="type">
        ///   The target type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the encoder can decode the specified type, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        protected virtual bool CanDecode(Type type) {
            return type != null;
        }


        /// <inheritdoc/>
        object? IObjectEncoder.Decode(Type type, byte[]? encodedData) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            if (encodedData == null) {
                return null;
            }

            return Decode(encodedData, type);
        }


        /// <summary>
        /// Decodes an <see cref="EncodedObject"/> to an instance of the specified type.
        /// </summary>
        /// <param name="encodedData">
        ///   The encoded data.
        /// </param>
        /// <param name="type">
        ///   The target type.
        /// </param>
        /// <returns>
        ///   The decoded object.
        /// </returns>
        protected abstract object? Decode(byte[]? encodedData, Type type);

    }
}
