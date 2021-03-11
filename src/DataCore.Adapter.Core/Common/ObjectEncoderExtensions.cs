using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The type ID for <typeparamref name="T"/> cannot be identified.
        /// </exception>
        /// <remarks>
        ///   The <see cref="EncodedObject.TypeId"/> is obtained by calling 
        ///   <see cref="TypeLibrary.TryGetTypeId{T}(out Uri?)"/>.
        /// </remarks>
        public static EncodedObject Encode<T>(this IObjectEncoder encoder, T value) {
            if (!TypeLibrary.TryGetTypeId<T>(out var typeId)) {
                throw new ArgumentOutOfRangeException(nameof(value), typeof(T).FullName, string.Format(CultureInfo.CurrentCulture, SharedResources.Error_TypeIdIsUndefined, typeof(DataTypeIdAttribute).Name));
            }

            return encoder.Encode(typeId!, value);
        }


        /// <summary>
        /// Creates a new <see cref="EncodedObject"/> with the specified type ID and value.
        /// </summary>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="typeId">
        ///   The type ID.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A new <see cref="EncodedObject"/>.
        /// </returns>
        public static EncodedObject Encode(this IObjectEncoder encoder, string typeId, object? value) {
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }
            if (!Uri.TryCreate(typeId, UriKind.Absolute, out var uri)) {
                throw new ArgumentOutOfRangeException(nameof(typeId), typeId, SharedResources.Error_AbsoluteUriRequired);
            }

            return encoder.Encode(uri, value);
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
        /// <remarks>
        ///   If <paramref name="extensionObject"/> is <see langword="null"/>, the default value 
        ///   of <typeparamref name="T"/> will be returned.
        /// </remarks>
        public static T? Decode<T>(this IObjectEncoder encoder, EncodedObject? extensionObject) {
            if (extensionObject == null) {
                return default;
            }

            return (T?) encoder.Decode(typeof(T), extensionObject);
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
        /// <remarks>
        ///   If the type of the variant is not <see cref="VariantType.ExtensionObject"/>, or the 
        ///   variant value is <see langword="null"/>, the default value of <typeparamref name="T"/> 
        ///   will be returned.
        /// </remarks>
        public static T? Decode<T>(this IObjectEncoder encoder, Variant variant) {
            if (variant.Type != VariantType.ExtensionObject) {
                return default;
            }

            var extensionObject = (EncodedObject) variant!;
            return (T?) encoder.Decode(typeof(T), extensionObject);
        }


    }

}
