using System;
using System.Globalization;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Extensions for <see cref="IObjectEncoder"/>.
    /// </summary>
    public static class ObjectEncoderExtensions {

        /// <summary>
        /// Creates a new <see cref="EncodedObject"/> with the specified value.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type. The type must be resolvable by calling <see cref="TypeLibrary.TryGetTypeId{T}(out Uri?)"/>.
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A new <see cref="EncodedObject"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The type ID for <typeparamref name="T"/> cannot be identified.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoder"/> cannot encode <typeparamref name="T"/>.
        /// </exception>
        /// <remarks>
        ///   The <see cref="EncodedObject.TypeId"/> is obtained by calling 
        ///   <see cref="TypeLibrary.TryGetTypeId{T}(out Uri?)"/>.
        /// </remarks>
        public static EncodedObject Encode<T>(this IObjectEncoder encoder, T? value) {
            if (encoder == null) {
                throw new ArgumentNullException(nameof(encoder));
            }
            
            if (!TypeLibrary.TryGetTypeId<T>(out var typeId)) {
                throw new ArgumentOutOfRangeException(nameof(value), typeof(T).FullName, string.Format(CultureInfo.CurrentCulture, SharedResources.Error_TypeIdIsUndefined, typeof(DataTypeIdAttribute).Name));
            }

            if (!encoder.CanEncode(typeof(T))) {
                throw new ArgumentOutOfRangeException(nameof(value), value, SharedResources.Error_CannotEncodeType);
            }

            return EncodedObject.Create(typeId!, encoder.EncodingType, encoder.Encode(typeof(T), value));
        }


        /// <summary>
        /// Creates a new <see cref="EncodedObject"/> with the specified value.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type..
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="typeId">
        ///   The type ID for the encoded object.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A new <see cref="EncodedObject"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoder"/> cannot encode <typeparamref name="T"/>.
        /// </exception>
        public static EncodedObject Encode<T>(this IObjectEncoder encoder, Uri typeId, T? value) {
            if (encoder == null) {
                throw new ArgumentNullException(nameof(encoder));
            }
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }
            if (!encoder.CanEncode(typeof(T))) {
                throw new ArgumentOutOfRangeException(nameof(value), value, SharedResources.Error_CannotEncodeType);
            }

            return EncodedObject.Create(typeId!, encoder.EncodingType, encoder.Encode(typeof(T), value)); ;
        }


        /// <summary>
        /// Creates a new <see cref="EncodedObject"/> with the specified value.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type..
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="typeId">
        ///   The type ID for the encoded object.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A new <see cref="EncodedObject"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoder"/> cannot encode <typeparamref name="T"/>.
        /// </exception>
        public static EncodedObject Encode<T>(this IObjectEncoder encoder, string typeId, T? value) {
            if (encoder == null) {
                throw new ArgumentNullException(nameof(encoder));
            }
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }
            if (!Uri.TryCreate(typeId, UriKind.Absolute, out var uri)) {
                throw new ArgumentOutOfRangeException(nameof(typeId), typeId, SharedResources.Error_AbsoluteUriRequired);
            }
            if (!encoder.CanEncode(typeof(T))) {
                throw new ArgumentOutOfRangeException(nameof(value), value, SharedResources.Error_CannotEncodeType);
            }

            return EncodedObject.Create(uri!, encoder.EncodingType, encoder.Encode(typeof(T), value)); ;
        }


        /// <summary>
        /// Decodes an <see cref="EncodedObject"/> to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The target type.
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="extensionObject">
        ///   The <see cref="EncodedObject"/>.
        /// </param>
        /// <returns>
        ///   An instance of <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   If <paramref name="extensionObject"/> is <see langword="null"/>, the default value 
        ///   of <typeparamref name="T"/> will be returned.
        /// </remarks>
        public static T? Decode<T>(this IObjectEncoder encoder, EncodedObject? extensionObject) {
            if (encoder == null) {
                throw new ArgumentNullException(nameof(encoder));
            }
            if (extensionObject == null) {
                return default;
            }
            if (!encoder.CanDecode(typeof(T), extensionObject.Encoding)) {
                throw new ArgumentOutOfRangeException(nameof(extensionObject), extensionObject, SharedResources.Error_CannotDecodeType);
            }

            return (T?) encoder.Decode(typeof(T), Convert.FromBase64String(extensionObject.EncodedBody));
        }


        /// <summary>
        /// Decodes a <see cref="Variant"/> containing an <see cref="EncodedObject"/> to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The target type.
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="variant">
        ///   The <see cref="Variant"/>.
        /// </param>
        /// <returns>
        ///   An instance of <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   If the type of the variant is not <see cref="VariantType.ExtensionObject"/>, or the 
        ///   variant value is <see langword="null"/>, the default value of <typeparamref name="T"/> 
        ///   will be returned.
        /// </remarks>
        public static T? Decode<T>(this IObjectEncoder encoder, Variant variant) {
            if (encoder == null) {
                throw new ArgumentNullException(nameof(encoder));
            }
            if (variant.Type != VariantType.ExtensionObject) {
                return default;
            }

            var extensionObject = (EncodedObject) variant!;
            return encoder.Decode<T>(extensionObject);
        }


    }

}
