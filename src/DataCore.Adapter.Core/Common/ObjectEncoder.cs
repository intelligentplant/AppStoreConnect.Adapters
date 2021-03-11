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
        EncodedObject IObjectEncoder.Encode(Uri typeId, object? value) {
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }
            
            if (value == null) {
                return new EncodedObject(typeId, EncodingType, Convert.ToBase64String(Array.Empty<byte>()));
            }

            var type = value.GetType();
            if (!CanEncode(type)) {
                throw new ArgumentOutOfRangeException(nameof(value), value, SharedResources.Error_CannotEncodeType);
            }

            return new EncodedObject(typeId, EncodingType, Convert.ToBase64String(Encode(value, type)));
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
        protected abstract byte[] Encode(object value, Type type);


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
            return true;
        }


        /// <inheritdoc/>
        object? IObjectEncoder.Decode(Type type, EncodedObject? extensionObject) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            if (extensionObject == null) {
                return null;
            }

            if (!((IObjectEncoder) this).CanDecode(type, extensionObject.Encoding)) {
                throw new ArgumentOutOfRangeException(nameof(type), type, SharedResources.Error_CannotDecodeType);
            }

            return Decode(Convert.FromBase64String(extensionObject!.EncodedBody), type);
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
        protected abstract object? Decode(byte[] encodedData, Type type);

    }
}
