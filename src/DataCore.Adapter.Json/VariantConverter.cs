using System;
using System.Text.Json;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="Variant"/>.
    /// </summary>
    public class VariantConverter : AdapterJsonConverter<Variant> { 

        /// <inheritdoc/>
        public override Variant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            VariantType? valueType = null;
            int[]? arrayDimensions = null;
            JsonElement valueElement = default;

            var startDepth = reader.CurrentDepth;

            do {
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(Variant.Type), StringComparison.OrdinalIgnoreCase)) {
                    valueType = JsonSerializer.Deserialize<VariantType>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(Variant.ArrayDimensions), StringComparison.OrdinalIgnoreCase)) {
                    arrayDimensions = JsonSerializer.Deserialize<int[]?>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(Variant.Value), StringComparison.OrdinalIgnoreCase)) {
                    valueElement = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            } while (reader.CurrentDepth != startDepth || reader.TokenType != JsonTokenType.EndObject);
            
            if (valueType == VariantType.Null) {
                return Variant.Null;
            }

            var isArray = arrayDimensions?.Length > 0;

            switch (valueType) {
                case VariantType.Boolean:
                    return isArray 
                        ? new Variant(ReadArray<bool>(valueElement, arrayDimensions!, options)) 
                        : valueElement.GetBoolean();
                case VariantType.Byte:
                    return isArray
                        ? new Variant(ReadArray<byte>(valueElement, arrayDimensions!, options))
                        : valueElement.GetByte();
                case VariantType.DateTime:
                    return isArray
                        ? new Variant(ReadArray<DateTime>(valueElement, arrayDimensions!, options))
                        : valueElement.GetDateTime();
                case VariantType.ExtensionObject:
                    return isArray
                        ? new Variant(ReadArray<EncodedObject>(valueElement, arrayDimensions!, options))
                        : JsonSerializer.Deserialize<EncodedObject>(valueElement.GetRawText(), options)!;
                case VariantType.Double:
                    return isArray
                        ? new Variant(ReadArray<double>(valueElement, arrayDimensions!, options))
                        : valueElement.GetDouble();
                case VariantType.Float:
                    return isArray
                        ? new Variant(ReadArray<float>(valueElement, arrayDimensions!, options))
                        : valueElement.GetSingle();
                case VariantType.Int16:
                    return isArray
                        ? new Variant(ReadArray<short>(valueElement, arrayDimensions!, options))
                        : valueElement.GetInt16();
                case VariantType.Int32:
                    return isArray
                        ? new Variant(ReadArray<int>(valueElement, arrayDimensions!, options))
                        : valueElement.GetInt32();
                case VariantType.Int64:
                    return isArray
                        ? new Variant(ReadArray<long>(valueElement, arrayDimensions!, options))
                        : valueElement.GetInt64();
                case VariantType.SByte:
                    return isArray
                        ? new Variant(ReadArray<sbyte>(valueElement, arrayDimensions!, options))
                        : valueElement.GetSByte();
                case VariantType.String:
                    return isArray
                        ? new Variant(ReadArray<string>(valueElement, arrayDimensions!, options))
                        : valueElement.GetString();
                case VariantType.TimeSpan:
                    return isArray
                        ? new Variant(ReadArray<TimeSpan>(valueElement, arrayDimensions!, options))
                        : TimeSpan.TryParse(valueElement.GetString(), out var ts) 
                            ? ts 
                            : default;
                case VariantType.UInt16:
                    return isArray
                        ? new Variant(ReadArray<ushort>(valueElement, arrayDimensions!, options))
                        : valueElement.GetUInt16();
                case VariantType.UInt32:
                    return isArray
                        ? new Variant(ReadArray<uint>(valueElement, arrayDimensions!, options))
                        : valueElement.GetUInt32();
                case VariantType.UInt64:
                    return isArray
                        ? new Variant(ReadArray<ulong>(valueElement, arrayDimensions!, options))
                        : valueElement.GetUInt64();
                case VariantType.Url:
                    return isArray
                        ? new Variant(ReadArray<Uri>(valueElement, arrayDimensions!, options))
                        : new Uri(valueElement.GetString(), UriKind.Absolute);
                case VariantType.Unknown:
                default:
                    return Variant.Null;
            }
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Variant value, JsonSerializerOptions options) {
            if (writer == null) {
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(Variant.Type), value.Type, options);
            if (value.Value is Array arr) {
                writer.WritePropertyName(ConvertPropertyName(nameof(Variant.Value), options));
                WriteArray(writer, arr, options);
            }
            else {
                WritePropertyValue(writer, nameof(Variant.Value), value.Value, options);
            }
            WritePropertyValue(writer, nameof(Variant.ArrayDimensions), value.ArrayDimensions, options);

            writer.WriteEndObject();
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
                throw new ArgumentOutOfRangeException(nameof(dimensions), dimensions, Resources.Error_ArrayDimensionsMustBeSpecified);
            }

            var el = JsonSerializer.Deserialize<JsonElement>(json, options);
            if (el.ValueKind != JsonValueKind.Array) {
                throw new JsonException(Resources.Error_NotAJsonArray);
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
        private static Array ReadArray<T>(JsonElement element, int[] dimensions, JsonSerializerOptions? options) {
            var result = Array.CreateInstance(typeof(T), dimensions);

            ReadArray<T>(element, result, 0, new int[dimensions.Length], options);

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
        /// <returns>
        ///   The <see cref="Array"/> that was read from the <see cref="JsonElement"/>.
        /// </returns>
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
        private static void WriteArray(Utf8JsonWriter writer, Array array, JsonSerializerOptions? options) {
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
