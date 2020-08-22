namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// <see cref="IValueEncoder"/> is used to encode and decode <see cref="EncodedValue"/> 
    /// objects passed to <see cref="IAdapterExtensionFeature"/> implementations.
    /// </summary>
    /// <remarks>
    ///   Implementers should inherit from the <see cref="ValueEncoder"/> class.
    /// </remarks>
    /// <seealso cref="ValueEncoder"/>.
    public interface IValueEncoder {

        /// <summary>
        /// The content type for the encoder.
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Encodes a value.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the value to encode.
        /// </typeparam>
        /// <param name="value">
        ///   The value to encode.
        /// </param>
        /// <returns>
        ///   The encoded value.
        /// </returns>
        EncodedValue Encode<T>(T value);

        /// <summary>
        /// Decodes a value.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to decode the encoded value to.
        /// </typeparam>
        /// <param name="value">
        ///   The value to decode.
        /// </param>
        /// <returns>
        ///   The decoded value.
        /// </returns>
        T Decode<T>(EncodedValue value);

    }
}
