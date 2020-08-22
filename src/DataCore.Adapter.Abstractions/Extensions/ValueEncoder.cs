using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Base implementaton of <see cref="IValueEncoder"/>.
    /// </summary>
    public abstract class ValueEncoder : IValueEncoder {
        
        /// <inheritdoc/>
        public string ContentType { get; }


        /// <summary>
        /// Creates a new <see cref="ValueEncoder"/> object.
        /// </summary>
        /// <param name="contentType">
        ///   The content type for the encoder e.g. "application/json".
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="contentType"/> is <see langword="null"/> or white space.
        /// </exception>
        protected ValueEncoder(string contentType) {
            ContentType = string.IsNullOrWhiteSpace(contentType)
                ? throw new ArgumentException(SharedResources.Error_ContentTypeIsRequired, nameof(contentType))
                : contentType;
        }


        /// <summary>
        /// Gets the type URI for the specified type.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   The type URI, or <see langword="null"/> if the <paramref name="type"/> is not 
        ///   annotated with <see cref="ExtensionTypeAttribute"/>
        /// </returns>
        private static Uri GetTypeUri(Type type) {
            return type.GetCustomAttribute<ExtensionTypeAttribute>()?.Uri;
        }


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
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="T"/> is not annotated with <see cref="ExtensionTypeAttribute"/>.
        /// </exception>
        EncodedValue IValueEncoder.Encode<T>(T value) {
            var typeUri = GetTypeUri(typeof(T));
            if (typeUri == null) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_TypeIsNotAnExtensionType, typeof(T).FullName, typeof(ExtensionTypeAttribute).FullName), nameof(value));
            }
            return new EncodedValue(typeUri, ContentType, Encode(value) ?? Array.Empty<byte>());
        }


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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The content type of the <paramref name="value"/> does not match the encoder's 
        ///   <see cref="ContentType"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="T"/> is not annotated with <see cref="ExtensionTypeAttribute"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The type URIs for <typeparamref name="T"/> and <paramref name="value"/> are different. 
        /// </exception>
        T IValueEncoder.Decode<T>(EncodedValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            if (!string.Equals(ContentType, value.ContentType, StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_ContentTypeMismatch, ContentType, value.ContentType), nameof(value));
            }

            var typeUri = GetTypeUri(typeof(T));
            if (typeUri == null) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_TypeIsNotAnExtensionType, typeof(T).FullName, typeof(ExtensionTypeAttribute).FullName), nameof(value));
            }
            if (!typeUri.Equals(value.TypeUri)) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_ExtensionTypeUriMismatch, typeof(T).FullName, typeUri, value.TypeUri), nameof(value));
            }

            return Decode<T>(value.Value);
        }


        /// <summary>
        /// Encodes the specified value to a sequence of bytes.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   The serialized value.
        /// </returns>
        protected abstract IEnumerable<byte> Encode<T>(T value);


        /// <summary>
        /// Decodes the specified byte sequence.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to decode the byte sequence to.
        /// </typeparam>
        /// <param name="value">
        ///   The byte sequence.
        /// </param>
        /// <returns>
        ///   The decoded value.
        /// </returns>
        protected abstract T Decode<T>(IEnumerable<byte> value);

    }
}
