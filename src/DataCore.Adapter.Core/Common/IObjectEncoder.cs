using System;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Encodes and decodes object data for use in <see cref="EncodedObject"/> instances.
    /// </summary>
    public interface IObjectEncoder {

        /// <summary>
        /// The encoding type used by the encoder.
        /// </summary>
        string EncodingType { get; }

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
        /// Tests if the <see cref="IObjectEncoder"/> can decode to the specified type.
        /// </summary>
        /// <param name="type">
        ///   The target type.
        /// </param>
        /// <param name="encoding">
        ///   The encoding used in the encoded object data.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the encoder can decode the specified type, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        bool CanDecode(Type type, string encoding);


        /// <summary>
        /// Encodes an object instance
        /// </summary>
        /// <param name="type">
        ///   The type of the opject to be encoded.
        /// </param>
        /// <param name="value">
        ///   The value to be encoded.
        /// </param>
        /// <returns>
        ///   The encoded object data.
        /// </returns>
        byte[]? Encode(Type type, object? value);


        /// <summary>
        /// Decodes encoded data to an instance of the specified type.
        /// </summary>
        /// <param name="type">
        ///   The target type.
        /// </param>
        /// <param name="encoded">
        ///   The encoded object data.
        /// </param>
        /// <returns>
        ///   The decoded object.
        /// </returns>
        object? Decode(Type type, byte[]? encoded);

    }
}
