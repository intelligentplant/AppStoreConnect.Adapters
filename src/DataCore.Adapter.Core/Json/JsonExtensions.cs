using System;
using System.Text.Json;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON extensions.
    /// </summary>
    public static class JsonExtensions {

        /// <summary>
        /// Sets default settings on the <see cref="JsonSerializerOptions"/> for use with Data 
        /// Core adapter hosting.
        /// </summary>
        /// <param name="options">
        ///   The <see cref="JsonSerializerOptions"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="JsonSerializerOptions"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public static JsonSerializerOptions UseDataCoreAdapterDefaults(this JsonSerializerOptions options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            // We need to be able to read/write "NaN" etc.
            options.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;
            //options.AddContext<AdapterJsonContext>();

            // Ensure that DateTime values are always serialized/deserialized in UTC.
            options.Converters.Add(new UtcDateTimeConverter());
            options.Converters.Add(new NullableUtcDateTimeConverter());

            return options;
        }


        /// <summary>
        /// Reads an N-dimensional array from the specified JSON string.
        /// </summary>
        /// <typeparam name="T">
        ///   The array element type.
        /// </typeparam>
        /// <param name="json">
        ///   The JSON string.
        /// </param>
        /// <param name="dimensions">
        ///   The array dimensions.
        /// </param>
        /// <param name="options">
        ///   Serialization options.
        /// </param>
        /// <returns>
        ///   An <see cref="Array"/> instance containing the deserialized array contents.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="dimensions"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="dimensions"/> does not contain at least one dimension length.
        /// </exception>
        public static Array ReadArray<T>(string json, int[] dimensions, JsonSerializerOptions? options = null) {
            if (json == null) {
                throw new ArgumentNullException(nameof(json));
            }
            if (dimensions?.Length == 0) {
                throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, SharedResources.Error_ArrayDimensionsMustBeSpecified);
            }

            var el = JsonSerializer.Deserialize<JsonElement>(json, options);
            if (el.ValueKind != JsonValueKind.Array) {
                throw new JsonException(SharedResources.Error_NotAJsonArray);
            }

            return ReadArray<T>(el, dimensions!, options!);
        }


        /// <summary>
        /// Reads an N-dimensional array from the specified <see cref="JsonElement"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The array element type.
        /// </typeparam>
        /// <param name="element">
        ///   The <see cref="JsonElement"/> containing the array.
        /// </param>
        /// <param name="dimensions">
        ///   The array dimensions.
        /// </param>
        /// <param name="options">
        ///   Serialization options.
        /// </param>
        /// <returns>
        ///   The <see cref="Array"/> that was read from the <see cref="JsonElement"/>.
        /// </returns>
        public static Array ReadArray<T>(JsonElement element, int[] dimensions, JsonSerializerOptions? options) {
            if (element.ValueKind != JsonValueKind.Array) {
                throw new JsonException(SharedResources.Error_NotAJsonArray);
            }
            if (dimensions?.Length == 0) {
                throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, SharedResources.Error_ArrayDimensionsMustBeSpecified);
            }

            var result = Array.CreateInstance(typeof(T), dimensions);

            ReadArray<T>(element, result, 0, new int[dimensions!.Length], options);

            return result;
        }


        /// <summary>
        /// Reads an N-dimensional array from the specified <see cref="JsonElement"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The array element type.
        /// </typeparam>
        /// <param name="element">
        ///   The <see cref="JsonElement"/> containing the array.
        /// </param>
        /// <param name="array">
        ///   The <see cref="Array"/> instance to assign values to.
        /// </param>
        /// <param name="dimension">
        ///   The current array dimension that is being processed.
        /// </param>
        /// <param name="indices">
        ///   The indices for the array item that will be set when the next array item is read 
        ///   from the JSON <paramref name="element"/>.
        /// </param>
        /// <param name="options">
        ///   Serialization options.
        /// </param>
        private static void ReadArray<T>(JsonElement element, Array array, int dimension, int[] indices, JsonSerializerOptions? options) {
            var i = 0;
            foreach (var el in element.EnumerateArray()) {
                indices[dimension] = i;
                if (dimension + 1 == array.Rank) {
                    var val = JsonSerializer.Deserialize(el.GetRawText(), typeof(T), options);
                    array.SetValue(val, indices);
                }
                else {
                    ReadArray<T>(el, array, dimension + 1, indices, options);
                }
                ++i;
            }
        }


        /// <summary>
        /// Serializes an N-dimensional array to JSON.
        /// </summary>
        /// <param name="array">
        ///   The array.
        /// </param>
        /// <param name="options">
        ///   JSON serialization options.
        /// </param>
        /// <returns>
        ///   The serialized array.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="array"/> is <see langword="null"/>.
        /// </exception>
        public static byte[] WriteArray(Array array, JsonSerializerOptions? options = null) {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }

            using (var ms = new System.IO.MemoryStream())
            using (var writer = new Utf8JsonWriter(ms)) {
                WriteArray(writer, array, options);
                writer.Flush();
                return ms.ToArray();
            }
        }


        /// <summary>
        /// Writes an N-dimensional array to the specified <see cref="Utf8JsonWriter"/>.
        /// </summary>
        /// <param name="writer">
        ///   The JSON writer.
        /// </param>
        /// <param name="array">
        ///   The array.
        /// </param>
        /// <param name="options">
        ///   Serialization options.
        /// </param>
        internal static void WriteArray(Utf8JsonWriter writer, Array array, JsonSerializerOptions? options) {
            WriteArray(writer, array, 0, new int[array.Rank], options);
        }


        /// <summary>
        /// Writes an N-dimensional array to the specified <see cref="Utf8JsonWriter"/>.
        /// </summary>
        /// <param name="writer">
        ///   The JSON writer.
        /// </param>
        /// <param name="array">
        ///   The array.
        /// </param>
        /// <param name="dimension">
        ///   The current array dimension that is being processed.
        /// </param>
        /// <param name="indices">
        ///   The indices for the next array item to be written. An item will only be written 
        /// </param>
        /// <param name="options">
        ///   Serialization options.
        /// </param>
        private static void WriteArray(Utf8JsonWriter writer, Array array, int dimension, int[] indices, JsonSerializerOptions? options) {
            var length = array.GetLength(dimension);

            writer.WriteStartArray();

            for (var i = 0; i < length; i++) {
                indices[dimension] = i;

                if (dimension + 1 == array.Rank) {
                    var val = array.GetValue(indices);
                    JsonSerializer.Serialize(writer, val, array.GetType().GetElementType(), options);
                }
                else {
                    WriteArray(writer, array, dimension + 1, indices, options);
                }
            }

            writer.WriteEndArray();
        }

    }
}
