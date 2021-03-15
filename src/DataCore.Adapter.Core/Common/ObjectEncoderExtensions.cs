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
        /// Gets a compatible encoding from the collection for the specified type.
        /// </summary>
        /// <param name="encoders">
        ///   The collection of <see cref="IObjectEncoder"/> instances.
        /// </param>
        /// <param name="type">
        ///   The type to encode.
        /// </param>
        /// <returns>
        ///   A compatible <see cref="IObjectEncoder"/>, or <see langword="null"/> if no compatible 
        ///   <see cref="IObjectEncoder"/> could be found.
        /// </returns>
        public static IObjectEncoder? GetCompatibleEncoder(this IEnumerable<IObjectEncoder> encoders, Type type) {
            if (encoders == null) {
                throw new ArgumentNullException(nameof(encoders));
            }

            return encoders.FirstOrDefault(x => x.CanEncode(type));
        }


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
        ///   The value type. The type must be resolvable by calling <see cref="TypeLibrary.TryGetTypeId{T}(out Uri?)"/>.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A new <see cref="EncodedObject"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The type ID for <typeparamref name="T"/> cannot be identified.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No item in <paramref name="encoders"/> can encode <typeparamref name="T"/>.
        /// </exception>
        /// <remarks>
        ///   The <see cref="EncodedObject.TypeId"/> is obtained by calling 
        ///   <see cref="TypeLibrary.TryGetTypeId{T}(out Uri?)"/>.
        /// </remarks>
        public static EncodedObject Encode<T>(this IEnumerable<IObjectEncoder> encoders, T? value) {
            if (encoders == null) {
                throw new ArgumentNullException(nameof(encoders));
            }

            if (!TypeLibrary.TryGetTypeId<T>(out var typeId)) {
                throw new ArgumentOutOfRangeException(nameof(value), typeof(T).FullName, string.Format(CultureInfo.CurrentCulture, SharedResources.Error_TypeIdIsUndefined, typeof(DataTypeIdAttribute).Name));
            }

            var encoder = encoders.GetCompatibleEncoder(typeof(T));
            if (encoder == null) {
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
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances.
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
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No item in <paramref name="encoders"/> can encode <typeparamref name="T"/>.
        /// </exception>
        public static EncodedObject Encode<T>(this IEnumerable<IObjectEncoder> encoders, Uri typeId, T? value) {
            if (encoders == null) {
                throw new ArgumentNullException(nameof(encoders));
            }
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }
            var encoder = encoders.GetCompatibleEncoder(typeof(T));
            if (encoder == null) {
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
        /// Creates a new <see cref="EncodedObject"/> with the specified value.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances.
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
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No item in <paramref name="encoders"/> can encode <typeparamref name="T"/>.
        /// </exception>
        public static EncodedObject Encode<T>(this IEnumerable<IObjectEncoder> encoders, string typeId, T? value) {
            if (encoders == null) {
                throw new ArgumentNullException(nameof(encoders));
            }
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }
            if (!Uri.TryCreate(typeId, UriKind.Absolute, out var uri)) {
                throw new ArgumentOutOfRangeException(nameof(typeId), typeId, SharedResources.Error_AbsoluteUriRequired);
            }
            var encoder = encoders.GetCompatibleEncoder(typeof(T));
            if (encoder == null) {
                throw new ArgumentOutOfRangeException(nameof(value), value, SharedResources.Error_CannotEncodeType);
            }

            return EncodedObject.Create(uri!, encoder.EncodingType, encoder.Encode(typeof(T), value)); ;
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="type">
        ///   The element type in the source array.
        /// </param>
        /// <param name="typeId">
        ///   The type ID for the <paramref name="type"/>.
        /// </param>
        /// <param name="sourceArray">
        ///   The source array.
        /// </param>
        /// <param name="destinationArray">
        ///   The destination array.
        /// </param>
        /// <param name="dimension">
        ///   The current dimension that is being processed.
        /// </param>
        /// <param name="indices">
        ///   The indices of the next item to be copied from the source array to the destination 
        ///   array.
        /// </param>
        private static void EncodeArray(IObjectEncoder encoder, Type type, Uri typeId, Array sourceArray, Array destinationArray, int dimension, int[] indices) {
            var length = sourceArray.GetLength(dimension);

            for (var i = 0; i < length; i++) {
                indices[dimension] = i;

                if (dimension + 1 == sourceArray.Rank) {
                    var sourceVal = sourceArray.GetValue(indices);
                    destinationArray.SetValue(EncodedObject.Create(typeId, encoder.EncodingType, encoder.Encode(type, sourceVal)), indices);
                }
                else {
                    EncodeArray(encoder, type, typeId, sourceArray, destinationArray, dimension + 1, indices);
                }
            }
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="typeId">
        ///   The type ID of the source array element type.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
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
        ///   The <paramref name="encoder"/> cannot encode the source array's element type.
        /// </exception>
        public static Array? Encode(this IObjectEncoder encoder, Uri typeId, Array? value) {
            if (encoder == null) {
                throw new ArgumentNullException(nameof(encoder));
            }
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }
            if (value == null) {
                return null;
            }

            var elementType = value.GetType().GetElementType();

            if (!encoder.CanEncode(elementType)) {
                throw new ArgumentOutOfRangeException(nameof(value), value, SharedResources.Error_CannotEncodeType);
            }

            var dimensions = new int[value.Rank];
            for (var i = 0; i < value.Rank; i++) {
                dimensions[i] = value.GetLength(i);
            }

            var result = Array.CreateInstance(typeof(EncodedObject), dimensions);
            EncodeArray(encoder, elementType, typeId, value, result, 0, new int[dimensions.Length]);

            return result;
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances.
        /// </param>
        /// <param name="typeId">
        ///   The type ID of the source array element type.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No item in <paramref name="encoders"/> can encode the source array's element type.
        /// </exception>
        public static Array? Encode(this IEnumerable<IObjectEncoder> encoders, Uri typeId, Array? value) {
            if (encoders == null) {
                throw new ArgumentNullException(nameof(encoders));
            }
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }
            if (value == null) {
                return null;
            }

            var elementType = value.GetType().GetElementType();

            var encoder = encoders.GetCompatibleEncoder(elementType);
            if (encoder == null) {
                throw new ArgumentOutOfRangeException(nameof(value), value, SharedResources.Error_CannotEncodeType);
            }

            return Encode(encoder, typeId, value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The type ID for the source array's element type cannot be identified.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoder"/> cannot encode the source array's element type.
        /// </exception>
        public static Array? Encode(this IObjectEncoder encoder, Array? value) {
            if (encoder == null) {
                throw new ArgumentNullException(nameof(encoder));
            }
            if (value == null) {
                return null;
            }

            var elementType = value.GetType().GetElementType();
            if (!TypeLibrary.TryGetTypeId(elementType, out var typeId)) {
                throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(CultureInfo.CurrentCulture, SharedResources.Error_TypeIdIsUndefined, typeof(DataTypeIdAttribute).Name));
            }

            return encoder.Encode(typeId!, value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The type ID for the source array's element type cannot be identified.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoders"/> cannot encode the source array's element type.
        /// </exception>
        public static Array? Encode(this IEnumerable<IObjectEncoder> encoders, Array? value) {
            if (encoders == null) {
                throw new ArgumentNullException(nameof(encoders));
            }
            if (value == null) {
                return null;
            }

            var elementType = value.GetType().GetElementType();
            if (!TypeLibrary.TryGetTypeId(elementType, out var typeId)) {
                throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(CultureInfo.CurrentCulture, SharedResources.Error_TypeIdIsUndefined, typeof(DataTypeIdAttribute).Name));
            }

            return encoders.Encode(typeId!, value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <typeparam name="T">
        ///   The source array element type.
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="typeId">
        ///   The type ID of the source array element type.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
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
        ///   The <paramref name="encoder"/> cannot encode the source array's element type.
        /// </exception>
        public static EncodedObject[]? Encode<T>(this IObjectEncoder encoder, Uri typeId, T[]? value) {
            return (EncodedObject[]?) encoder.Encode(typeId, (Array?) value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <typeparam name="T">
        ///   The source array element type.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances to choose a compatible encoder from.
        /// </param>
        /// <param name="typeId">
        ///   The type ID of the source array element type.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No item in <paramref name="encoders"/> can encode the source array's element type.
        /// </exception>
        public static EncodedObject[]? Encode<T>(this IEnumerable<IObjectEncoder> encoders, Uri typeId, T[]? value) {
            return (EncodedObject[]?) encoders.Encode(typeId, (Array?) value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <typeparam name="T">
        ///   The source array element type.
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The type ID for <typeparamref name="T"/> cannot be identified.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoder"/> cannot encode the source array's element type.
        /// </exception>
        public static EncodedObject[]? Encode<T>(this IObjectEncoder encoder, T[]? value) {
            if (!TypeLibrary.TryGetTypeId(typeof(T), out var typeId)) {
                throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(CultureInfo.CurrentCulture, SharedResources.Error_TypeIdIsUndefined, typeof(DataTypeIdAttribute).Name));
            }
            return (EncodedObject[]?) encoder.Encode(typeId!, (Array?) value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <typeparam name="T">
        ///   The source array element type.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances to choose a compatible encoder from.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The type ID for <typeparamref name="T"/> cannot be identified.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No item in <paramref name="encoders"/> can encode the source array's element type.
        /// </exception>
        public static EncodedObject[]? Encode<T>(this IEnumerable<IObjectEncoder> encoders, T[]? value) {
            if (!TypeLibrary.TryGetTypeId(typeof(T), out var typeId)) {
                throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(CultureInfo.CurrentCulture, SharedResources.Error_TypeIdIsUndefined, typeof(DataTypeIdAttribute).Name));
            }
            return (EncodedObject[]?) encoders.Encode(typeId!, (Array?) value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <typeparam name="T">
        ///   The source array element type.
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="typeId">
        ///   The type ID of the source array element type.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
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
        ///   The <paramref name="encoder"/> cannot encode the source array's element type.
        /// </exception>
        public static EncodedObject[,]? Encode<T>(this IObjectEncoder encoder, Uri typeId, T[,]? value) {
            return (EncodedObject[,]?) encoder.Encode(typeId, (Array?) value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <typeparam name="T">
        ///   The source array element type.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances to choose a compatible encoder from.
        /// </param>
        /// <param name="typeId">
        ///   The type ID of the source array element type.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No item in <paramref name="encoders"/> can encode the source array's element type.
        /// </exception>
        public static EncodedObject[,]? Encode<T>(this IEnumerable<IObjectEncoder> encoders, Uri typeId, T[,]? value) {
            return (EncodedObject[,]?) encoders.Encode(typeId, (Array?) value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <typeparam name="T">
        ///   The source array element type.
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The type ID for <typeparamref name="T"/> cannot be identified.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoder"/> cannot encode the source array's element type.
        /// </exception>
        public static EncodedObject[,]? Encode<T>(this IObjectEncoder encoder, T[,]? value) {
            if (!TypeLibrary.TryGetTypeId(typeof(T), out var typeId)) {
                throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(CultureInfo.CurrentCulture, SharedResources.Error_TypeIdIsUndefined, typeof(DataTypeIdAttribute).Name));
            }
            return (EncodedObject[,]?) encoder.Encode(typeId!, (Array?) value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <typeparam name="T">
        ///   The source array element type.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances to choose a compatible encoder from.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The type ID for <typeparamref name="T"/> cannot be identified.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No item in <paramref name="encoders"/> can encode the source array's element type.
        /// </exception>
        public static EncodedObject[,]? Encode<T>(this IEnumerable<IObjectEncoder> encoders, T[,]? value) {
            if (!TypeLibrary.TryGetTypeId(typeof(T), out var typeId)) {
                throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(CultureInfo.CurrentCulture, SharedResources.Error_TypeIdIsUndefined, typeof(DataTypeIdAttribute).Name));
            }
            return (EncodedObject[,]?) encoders.Encode(typeId!, (Array?) value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <typeparam name="T">
        ///   The source array element type.
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="typeId">
        ///   The type ID of the source array element type.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
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
        ///   The <paramref name="encoder"/> cannot encode the source array's element type.
        /// </exception>
        public static EncodedObject[,,]? Encode<T>(this IObjectEncoder encoder, Uri typeId, T[,,]? value) {
            return (EncodedObject[,,]?) encoder.Encode(typeId, (Array?) value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <typeparam name="T">
        ///   The source array element type.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances to choose a compaible encoder from.
        /// </param>
        /// <param name="typeId">
        ///   The type ID of the source array element type.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No item in <paramref name="encoders"/> can encode the source array's element type.
        /// </exception>
        public static EncodedObject[,,]? Encode<T>(this IEnumerable<IObjectEncoder> encoders, Uri typeId, T[,,]? value) {
            return (EncodedObject[,,]?) encoders.Encode(typeId, (Array?) value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <typeparam name="T">
        ///   The source array element type.
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The type ID for <typeparamref name="T"/> cannot be identified.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoder"/> cannot encode the source array's element type.
        /// </exception>
        public static EncodedObject[,,]? Encode<T>(this IObjectEncoder encoder, T[,,]? value) {
            if (!TypeLibrary.TryGetTypeId(typeof(T), out var typeId)) {
                throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(CultureInfo.CurrentCulture, SharedResources.Error_TypeIdIsUndefined, typeof(DataTypeIdAttribute).Name));
            }
            return (EncodedObject[,,]?) encoder.Encode(typeId!, (Array?) value);
        }


        /// <summary>
        /// Recursively encodes each item in a source array as an <see cref="EncodedObject"/> in a 
        /// destination array with the same dimensions.
        /// </summary>
        /// <typeparam name="T">
        ///   The source array element type.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances to choose a compatible encoder from.
        /// </param>
        /// <param name="value">
        ///   The array to encode.
        /// </param>
        /// <returns>
        ///   An array of <see cref="EncodedObject"/> instances that has the same dimensions as 
        ///   the source array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The type ID for <typeparamref name="T"/> cannot be identified.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No item in <paramref name="encoders"/> can encode the source array's element type.
        /// </exception>
        public static EncodedObject[,,]? Encode<T>(this IEnumerable<IObjectEncoder> encoders, T[,,]? value) {
            if (!TypeLibrary.TryGetTypeId(typeof(T), out var typeId)) {
                throw new ArgumentOutOfRangeException(nameof(value), value, string.Format(CultureInfo.CurrentCulture, SharedResources.Error_TypeIdIsUndefined, typeof(DataTypeIdAttribute).Name));
            }
            return (EncodedObject[,,]?) encoders.Encode(typeId!, (Array?) value);
        }


        /// <summary>
        /// Tests if the <see cref="IObjectEncoder"/> can decode to the specified type.
        /// </summary>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
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
        public static bool CanDecode(this IObjectEncoder encoder, Type type, string encoding) {
            if (encoder == null) {
                throw new ArgumentNullException(nameof(encoder));
            }
            if (type == null) {
                return false;
            }

            if (!string.Equals(encoding, encoder.EncodingType, StringComparison.OrdinalIgnoreCase)) {
                return false;
            }

            return encoder.CanDecode(type);
        }


        /// <summary>
        /// Gets a compatible decoder from the collection for the specified type and encoding.
        /// </summary>
        /// <param name="encoders">
        ///   The collection of <see cref="IObjectEncoder"/> instances.
        /// </param>
        /// <param name="type">
        ///   The type to decode to.
        /// </param>
        /// <param name="encoding">
        ///   The encoding for the object data.
        /// </param>
        /// <returns>
        ///   A compatible <see cref="IObjectEncoder"/>, or <see langword="null"/> if no compatible 
        ///   <see cref="IObjectEncoder"/> could be found.
        /// </returns>
        public static IObjectEncoder? GetCompatibleDecoder(this IEnumerable<IObjectEncoder> encoders, Type type, string encoding) {
            if (encoders == null) {
                throw new ArgumentNullException(nameof(encoders));
            }

            return encoders.FirstOrDefault(x => x.CanDecode(type, encoding));
        }


        /// <summary>
        /// Decodes an <see cref="EncodedObject"/> to the specified type.
        /// </summary>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="type">
        ///   The target type.
        /// </param>
        /// <param name="extensionObject">
        ///   The <see cref="EncodedObject"/>.
        /// </param>
        /// <returns>
        ///   An instance of the specified type, or <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   If <paramref name="extensionObject"/> is <see langword="null"/>, the result will be 
        ///   <see langword="null"/>.
        /// </remarks>
        public static object? Decode(this IObjectEncoder encoder, Type type, EncodedObject? extensionObject) {
            if (encoder == null) {
                throw new ArgumentNullException(nameof(encoder));
            }
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (extensionObject == null) {
                return null;
            }

            if (!encoder.CanDecode(type, extensionObject.Encoding)) {
                throw new ArgumentOutOfRangeException(nameof(extensionObject), extensionObject, SharedResources.Error_CannotDecodeType);
            }

            return encoder.Decode(type, Convert.FromBase64String(extensionObject.EncodedBody));
        }


        /// <summary>
        /// Decodes an <see cref="EncodedObject"/> to the specified type.
        /// </summary>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances.
        /// </param>
        /// <param name="type">
        ///   The target type.
        /// </param>
        /// <param name="extensionObject">
        ///   The <see cref="EncodedObject"/>.
        /// </param>
        /// <returns>
        ///   An instance of the specified type, or <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   If <paramref name="extensionObject"/> is <see langword="null"/>, the result will be 
        ///   <see langword="null"/>.
        /// </remarks>
        public static object? Decode(this IEnumerable<IObjectEncoder> encoders, Type type, EncodedObject? extensionObject) {
            if (encoders == null) {
                throw new ArgumentNullException(nameof(encoders));
            }
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (extensionObject == null) {
                return null;
            }

            var encoder = encoders.GetCompatibleDecoder(type, extensionObject.Encoding);
            if (encoder == null) {
                throw new ArgumentOutOfRangeException(nameof(extensionObject), extensionObject, SharedResources.Error_CannotDecodeType);
            }

            return encoder.Decode(type, Convert.FromBase64String(extensionObject.EncodedBody));
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
        /// Decodes an <see cref="EncodedObject"/> to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The target type.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances.
        /// </param>
        /// <param name="extensionObject">
        ///   The <see cref="EncodedObject"/>.
        /// </param>
        /// <returns>
        ///   An instance of <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   If <paramref name="extensionObject"/> is <see langword="null"/>, the default value 
        ///   of <typeparamref name="T"/> will be returned.
        /// </remarks>
        public static T? Decode<T>(this IEnumerable<IObjectEncoder> encoders, EncodedObject? extensionObject) {
            if (encoders == null) {
                throw new ArgumentNullException(nameof(encoders));
            }
            if (extensionObject == null) {
                return default;
            }

            var encoder = encoders.GetCompatibleDecoder(typeof(T), extensionObject.Encoding);
            if (encoder == null) {
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
        ///   If the type of the variant is not <see cref="VariantType.ExtensionObject"/>, the 
        ///   value of the <paramref name="variant"/> will be cast directly to <typeparamref name="T"/>.
        /// </remarks>
        public static T? Decode<T>(this IObjectEncoder encoder, Variant variant) {
            if (encoder == null) {
                throw new ArgumentNullException(nameof(encoder));
            }
            if (variant.Type != VariantType.ExtensionObject) {
                return (T?) variant.Value;
            }

            return Decode<T>(new[] { encoder }, variant);
        }


        /// <summary>
        /// Decodes a <see cref="Variant"/> containing an <see cref="EncodedObject"/> to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The target type.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances.
        /// </param>
        /// <param name="variant">
        ///   The <see cref="Variant"/>.
        /// </param>
        /// <returns>
        ///   An instance of <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   If the type of the variant is not <see cref="VariantType.ExtensionObject"/>, the 
        ///   value of the <paramref name="variant"/> will be cast directly to <typeparamref name="T"/>.
        /// </remarks>
        public static T? Decode<T>(this IEnumerable<IObjectEncoder> encoders, Variant variant) {
            if (encoders == null) {
                throw new ArgumentNullException(nameof(encoders));
            }
            if (variant.Type != VariantType.ExtensionObject) {
                return (T?) variant.Value;
            }

            var targetType = typeof(T);

            if (targetType.IsArray) {
                if (!variant.IsArray()) {
                    throw new ArgumentException("Cannot convert a non-array variant to an array type.", nameof(variant));
                }

                var targetArrayRank = targetType.GetArrayRank();

                if (targetArrayRank != variant.ArrayDimensions?.Length) {
                    throw new ArgumentException($"Cannot convert an array with rank {variant.ArrayDimensions?.Length ?? 0} to an array with rank {targetArrayRank}.", nameof(variant));
                }

                var elementType = targetType.GetElementType();
                var result = Array.CreateInstance(elementType, variant.ArrayDimensions);
                DecodeArray(encoders, elementType, (Array) variant.Value!, result, 0, new int[targetArrayRank]);

                return (T?) (object) result;
            }

            if (variant.IsArray()) {
                throw new ArgumentException("Cannot convert an array variant to a non-array type.", nameof(variant));
            }

            var extensionObject = (EncodedObject) variant!;
            return encoders.Decode<T>(extensionObject);
        }


        /// <summary>
        /// Recursively decodes a source array of <see cref="EncodedObject"/> into a destination 
        /// array of the specified type.
        /// </summary>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="type">
        ///   The element type for the destination array.
        /// </param>
        /// <param name="sourceArray">
        ///   The source array.
        /// </param>
        /// <param name="destinationArray">
        ///   The destination array. Must have the same dimensions as the source array.
        /// </param>
        /// <param name="dimension">
        ///   The current array dimension that is being processed.
        /// </param>
        /// <param name="indices">
        ///   The indices of the next element to be copied from the source array to the 
        ///   destination array.
        /// </param>
        private static void DecodeArray(IEnumerable<IObjectEncoder> encoders, Type type, Array sourceArray, Array destinationArray, int dimension, int[] indices) {
            var length = sourceArray.GetLength(dimension);

            for (var i = 0; i < length; i++) {
                indices[dimension] = i;

                if (dimension + 1 == sourceArray.Rank) {
                    var sourceVal = sourceArray.GetValue(indices) as EncodedObject;
                    if (sourceVal == null) {
                        continue;
                    }
                    destinationArray.SetValue(encoders.Decode(type, sourceVal), indices);
                }
                else {
                    DecodeArray(encoders, type, sourceArray, destinationArray, dimension + 1, indices);
                }
            }
        }


        /// <summary>
        /// Decodes a source array of <see cref="EncodedObject"/> instances into an array of the 
        /// specified element type.
        /// </summary>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="type">
        ///   The element type for the destination array.
        /// </param>
        /// <param name="source">
        ///   The source array of <see cref="EncodedObject"/> instances.
        /// </param>
        /// <returns>
        ///   An array of decoded objects.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="source"/> is not <see cref="EncodedObject"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoder"/> cannot decode the elements in the <paramref name="source"/>
        ///   array.
        /// </exception>
        /// <remarks>
        ///   <paramref name="source"/> can be a multidimensional array of <see cref="EncodedObject"/> 
        ///   instances.
        /// </remarks>
        public static Array? Decode(this IObjectEncoder encoder, Type type, Array? source) {
            if (encoder == null) {
                throw new ArgumentNullException(nameof(encoder));
            }
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (source == null) {
                return null;
            }

            return Decode(new[] { encoder }, type, source);
        }


        /// <summary>
        /// Decodes a source array of <see cref="EncodedObject"/> instances into an array of the 
        /// specified element type.
        /// </summary>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="type">
        ///   The element type for the destination array.
        /// </param>
        /// <param name="source">
        ///   The source array of <see cref="EncodedObject"/> instances.
        /// </param>
        /// <returns>
        ///   An array of decoded objects.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="source"/> is not <see cref="EncodedObject"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoders"/> cannot decode the elements in the <paramref name="source"/>
        ///   array.
        /// </exception>
        /// <remarks>
        ///   <paramref name="source"/> can be a multidimensional array of <see cref="EncodedObject"/> 
        ///   instances.
        /// </remarks>
        public static Array? Decode(this IEnumerable<IObjectEncoder> encoders, Type type, Array? source) {
            if (encoders == null) {
                throw new ArgumentNullException(nameof(encoders));
            }
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (source == null) {
                return null;
            }

            var elementType = source.GetType().GetElementType();
            if (elementType != typeof(EncodedObject)) {
                throw new ArgumentOutOfRangeException(nameof(source), source, SharedResources.Error_CannotDecodeType);
            }

            var dimensions = new int[source.Rank];
            for (var i = 0; i < source.Rank; i++) {
                dimensions[i] = source.GetLength(i);
            }

            var result = Array.CreateInstance(type, dimensions);
            DecodeArray(encoders, type, source, result, 0, new int[dimensions.Length]);

            return result;
        }


        /// <summary>
        /// Decodes a source array of <see cref="EncodedObject"/> instances into an array of 
        /// <typeparamref name="T"/> instances.
        /// </summary>
        /// <typeparam name="T">
        ///   The element type for the destination array.
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="source">
        ///   The source array of <see cref="EncodedObject"/> instances.
        /// </param>
        /// <returns>
        ///   An array of decoded objects.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="source"/> is not <see cref="EncodedObject"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoder"/> cannot decode the elements in the <paramref name="source"/>
        ///   array.
        /// </exception>
        /// <remarks>
        ///   <paramref name="source"/> can be a multidimensional array of <see cref="EncodedObject"/> 
        ///   instances.
        /// </remarks>
        public static Array? Decode<T>(this IObjectEncoder encoder, Array? source) {
            return encoder.Decode(typeof(T), source);
        }


        /// <summary>
        /// Decodes a source array of <see cref="EncodedObject"/> instances into an array of 
        /// <typeparamref name="T"/> instances.
        /// </summary>
        /// <typeparam name="T">
        ///   The element type for the destination array.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances.
        /// </param>
        /// <param name="source">
        ///   The source array of <see cref="EncodedObject"/> instances.
        /// </param>
        /// <returns>
        ///   An array of decoded objects.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="source"/> is not <see cref="EncodedObject"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No entry in <paramref name="encoders"/> can decode the elements in the 
        ///   <paramref name="source"/> array.
        /// </exception>
        /// <remarks>
        ///   <paramref name="source"/> can be a multidimensional array of <see cref="EncodedObject"/> 
        ///   instances.
        /// </remarks>
        public static Array? Decode<T>(this IEnumerable<IObjectEncoder> encoders, Array? source) {
            return encoders.Decode(typeof(T), source);
        }


        /// <summary>
        /// Decodes a 1-dimensional source array of <see cref="EncodedObject"/> instances into an 
        /// array of <typeparamref name="T"/> instances.
        /// </summary>
        /// <typeparam name="T">
        ///   The element type for the destination array.
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="source">
        ///   The source array of <see cref="EncodedObject"/> instances.
        /// </param>
        /// <returns>
        ///   An array of decoded objects.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="source"/> is not <see cref="EncodedObject"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoder"/> cannot decode the elements in the <paramref name="source"/>
        ///   array.
        /// </exception>
        public static T[]? Decode<T>(this IObjectEncoder encoder, EncodedObject[]? source) {
            return (T[]?) encoder.Decode(typeof(T), source);
        }


        /// <summary>
        /// Decodes a 1-dimensional source array of <see cref="EncodedObject"/> instances into an 
        /// array of <typeparamref name="T"/> instances.
        /// </summary>
        /// <typeparam name="T">
        ///   The element type for the destination array.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances.
        /// </param>
        /// <param name="source">
        ///   The source array of <see cref="EncodedObject"/> instances.
        /// </param>
        /// <returns>
        ///   An array of decoded objects.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="source"/> is not <see cref="EncodedObject"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No entry in <paramref name="encoders"/> can decode the elements in the 
        ///   <paramref name="source"/> array.
        /// </exception>
        public static T[]? Decode<T>(this IEnumerable<IObjectEncoder> encoders, EncodedObject[]? source) {
            return (T[]?) encoders.Decode(typeof(T), source);
        }


        /// <summary>
        /// Decodes a 2-dimensional source array of <see cref="EncodedObject"/> instances into an 
        /// array of <typeparamref name="T"/> instances.
        /// </summary>
        /// <typeparam name="T">
        ///   The element type for the destination array.
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="source">
        ///   The source array of <see cref="EncodedObject"/> instances.
        /// </param>
        /// <returns>
        ///   An array of decoded objects.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="source"/> is not <see cref="EncodedObject"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoder"/> cannot decode the elements in the <paramref name="source"/>
        ///   array.
        /// </exception>
        public static T[,]? Decode<T>(this IObjectEncoder encoder, EncodedObject[,]? source) {
            return (T[,]?) encoder.Decode(typeof(T), source);
        }


        /// <summary>
        /// Decodes a 2-dimensional source array of <see cref="EncodedObject"/> instances into an 
        /// array of <typeparamref name="T"/> instances.
        /// </summary>
        /// <typeparam name="T">
        ///   The element type for the destination array.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances.
        /// </param>
        /// <param name="source">
        ///   The source array of <see cref="EncodedObject"/> instances.
        /// </param>
        /// <returns>
        ///   An array of decoded objects.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="source"/> is not <see cref="EncodedObject"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No entry in <paramref name="encoders"/> can decode the elements in the 
        ///   <paramref name="source"/> array.
        /// </exception>
        public static T[,]? Decode<T>(this IEnumerable<IObjectEncoder> encoders, EncodedObject[,]? source) {
            return (T[,]?) encoders.Decode(typeof(T), source);
        }


        /// <summary>
        /// Decodes a 3-dimensional source array of <see cref="EncodedObject"/> instances into an 
        /// array of <typeparamref name="T"/> instances.
        /// </summary>
        /// <typeparam name="T">
        ///   The element type for the destination array.
        /// </typeparam>
        /// <param name="encoder">
        ///   The <see cref="IObjectEncoder"/>.
        /// </param>
        /// <param name="source">
        ///   The source array of <see cref="EncodedObject"/> instances.
        /// </param>
        /// <returns>
        ///   An array of decoded objects.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="source"/> is not <see cref="EncodedObject"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="encoder"/> cannot decode the elements in the <paramref name="source"/>
        ///   array.
        /// </exception>
        public static T[,,]? Decode<T>(this IObjectEncoder encoder, EncodedObject[,,]? source) {
            return (T[,,]?) encoder.Decode(typeof(T), source);
        }


        /// <summary>
        /// Decodes a 3-dimensional source array of <see cref="EncodedObject"/> instances into an 
        /// array of <typeparamref name="T"/> instances.
        /// </summary>
        /// <typeparam name="T">
        ///   The element type for the destination array.
        /// </typeparam>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances.
        /// </param>
        /// <param name="source">
        ///   The source array of <see cref="EncodedObject"/> instances.
        /// </param>
        /// <returns>
        ///   An array of decoded objects.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encoders"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="source"/> is not <see cref="EncodedObject"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   No entry in <paramref name="encoders"/> can decode the elements in the 
        ///   <paramref name="source"/> array.
        /// </exception>
        public static T[,,]? Decode<T>(this IEnumerable<IObjectEncoder> encoders, EncodedObject[,,]? source) {
            return (T[,,]?) encoders.Decode(typeof(T), source);
        }

    }

}
