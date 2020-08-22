using System;
using System.Collections.Generic;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// The <see cref="EncodedValue"/> class represents a value sent to or received by an 
    /// extension adapter feature that has been serialized.
    /// </summary>
    public class EncodedValue {

        /// <summary>
        /// The URI of the encoded extension type.
        /// </summary>
        /// <seealso cref="ExtensionTypeAttribute"/>
        public Uri TypeUri { get; }

        /// <summary>
        /// The content type of the encoded value (e.g. "application/json").
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// The encoded value.
        /// </summary>
        public IEnumerable<byte> Value { get; }


        /// <summary>
        /// Creates a new <see cref="EncodedValue"/> object.
        /// </summary>
        /// <param name="typeUri">
        ///   The URI of the encoded extension type.
        /// </param>
        /// <param name="contentType">
        ///   The content type of the encoded value (e.g. "application/json").
        /// </param>
        /// <param name="value">
        ///   The encoded value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeUri"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="contentType"/> is <see langword="null"/> or white space.
        /// </exception>
        public EncodedValue(Uri typeUri, string contentType, IEnumerable<byte> value) {
            TypeUri = typeUri ?? throw new ArgumentNullException(nameof(typeUri));
            ContentType = string.IsNullOrWhiteSpace(contentType) 
                ? throw new ArgumentException(SharedResources.Error_ContentTypeIsRequired, nameof(contentType))
                : contentType;
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

    }
}
