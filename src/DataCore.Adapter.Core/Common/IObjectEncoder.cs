using System;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Encodes and decodes <see cref="EncodedObject"/> instances.
    /// </summary>
    public interface IObjectEncoder {

        /// <summary>
        /// Tests if the <see cref="IObjectEncoder"/> can encode the specified 
        /// <see cref="Type"/>.
        /// </summary>
        /// <param name="type">
        ///   The type of the object to be encoded.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type can be encoded, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        bool CanEncode(Type type);


        /// <summary>
        /// Tests if the <see cref="IObjectEncoder"/> can decode an <see cref="EncodedObject"/> 
        /// to the specified type.
        /// </summary>
        /// <param name="type">
        ///   The target type.
        /// </param>
        /// <param name="encoding">
        ///   The encoding of the <see cref="EncodedObject"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the encoder can decode the specified type, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        bool CanDecode(Type type, string encoding);


        /// <summary>
        /// Encodes an object instance to an <see cref="EncodedObject"/>.
        /// </summary>
        /// <param name="typeId">
        ///   The type ID for the encoded object.
        /// </param>
        /// <param name="value">
        ///   The value to be encoded.
        /// </param>
        /// <returns>
        ///   The encoded object.
        /// </returns>
        EncodedObject Encode(Uri typeId, object? value);


        /// <summary>
        /// Decodes an <see cref="EncodedObject"/> to an instance of the specified type.
        /// </summary>
        /// <param name="type">
        ///   The target type.
        /// </param>
        /// <param name="extensionObject">
        ///   The extension object.
        /// </param>
        /// <returns>
        ///   The decoded object.
        /// </returns>
        object? Decode(Type type, EncodedObject? extensionObject);

    }
}
