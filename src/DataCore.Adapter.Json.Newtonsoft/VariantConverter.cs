using System;

using DataCore.Adapter.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataCore.Adapter.NewtonsoftJson {

    /// <summary>
    /// <see cref="JsonConverter{T}"/> for <see cref="Common.Variant"/>.
    /// </summary>
    public class VariantConverter : JsonConverter<Variant> {

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, Variant value, JsonSerializer serializer) {
            writer.WriteStartObject();

            writer.WritePropertyName(nameof(Variant.Type));
            serializer.Serialize(writer, value.Type);
            
            writer.WritePropertyName(nameof(Variant.ArrayDimensions));
            serializer.Serialize(writer, value.ArrayDimensions);

            writer.WritePropertyName(nameof(Variant.Value));
            serializer.Serialize(writer, value.Value);

            writer.WriteEndObject();
        }


        /// <inheritdoc/>
        public override Variant ReadJson(JsonReader reader, Type objectType, Variant existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.StartObject) {
                return default;
            }

            VariantType type = VariantType.Unknown;
            int[]? arrayDimensions = null;
            JToken? valueToken = null;

            do { 
                if (!reader.Read()) {
                    continue;
                }

                if (reader.TokenType != JsonToken.PropertyName) {
                    continue;
                }

                var propName = (string) reader.Value!;

                if (string.Equals(propName, nameof(Variant.Type), StringComparison.OrdinalIgnoreCase)) {
                    if (reader.Read()) {
                        type = serializer.Deserialize<VariantType>(reader);
                    }
                }
                else if (string.Equals(propName, nameof(Variant.ArrayDimensions), StringComparison.OrdinalIgnoreCase)) {
                    if (reader.Read()) {
                        arrayDimensions = serializer.Deserialize<int[]?>(reader);
                    }
                }
                else if (string.Equals(propName, nameof(Variant.Value), StringComparison.OrdinalIgnoreCase)) {
                    if (reader.Read()) {
                        valueToken = serializer.Deserialize<JToken>(reader);
                    }
                }
            } while (reader.TokenType != JsonToken.EndObject);

            if (type == VariantType.Unknown || valueToken == null) {
                return Variant.Null;
            }

            var isArray = arrayDimensions?.Length > 0;

            switch (type) {
                case VariantType.Boolean:
                    return isArray
                        ? new Variant(ReadArray<bool>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<bool>());
                case VariantType.Byte:
                    return isArray
                        ? new Variant(ReadArray<byte>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<byte>());
                case VariantType.DateTime:
                    return isArray
                        ? new Variant(ReadArray<DateTime>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<DateTime>());
                case VariantType.Double:
                    return isArray
                        ? new Variant(ReadArray<double>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<double>());
                case VariantType.ExtensionObject:
                    return isArray
                        ? new Variant(ReadArray<System.Text.Json.JsonElement>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<System.Text.Json.JsonElement>(serializer));
                case VariantType.Float:
                    return isArray
                        ? new Variant(ReadArray<float>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<float>());
                case VariantType.Int16:
                    return isArray
                        ? new Variant(ReadArray<short>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<short>());
                case VariantType.Int32:
                    return isArray
                        ? new Variant(ReadArray<int>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<int>());
                case VariantType.Int64:
                    return isArray
                        ? new Variant(ReadArray<long>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<long>());
                case VariantType.SByte:
                    return isArray
                        ? new Variant(ReadArray<sbyte>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<sbyte>());
                case VariantType.String:
                    return isArray
                        ? new Variant(ReadArray<string>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<string>());
                case VariantType.TimeSpan:
                    return isArray
                        ? new Variant(ReadArray<TimeSpan>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<TimeSpan>());
                case VariantType.UInt16:
                    return isArray
                        ? new Variant(ReadArray<ushort>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<ushort>());
                case VariantType.UInt32:
                    return isArray
                        ? new Variant(ReadArray<uint>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<uint>());
                case VariantType.UInt64:
                    return isArray
                        ? new Variant(ReadArray<ulong>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<ulong>());
                case VariantType.Url:
                    return isArray
                        ? new Variant(ReadArray<Uri>(valueToken, arrayDimensions!, serializer))
                        : new Variant(valueToken.ToObject<Uri>());
            }

            return Variant.Null;
        }


        /// <summary>
        /// Reads an N-dimensional array from the specified <see cref="JToken"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The array element type.
        /// </typeparam>
        /// <param name="element">
        ///   The <see cref="JArray"/> containing the array.
        /// </param>
        /// <param name="dimensions">
        ///   The array dimensions.
        /// </param>
        /// <param name="serializer">
        ///   The serializer to use.
        /// </param>
        /// <returns>
        ///   The <see cref="Array"/> that was read from the <paramref name="element"/>.
        /// </returns>
        private static Array? ReadArray<T>(JToken element, int[] dimensions, JsonSerializer serializer) {
            if (element is not JArray arr) {
                return null;
            }
            var result = Array.CreateInstance(typeof(T), dimensions);

            ReadArray<T>(arr, result, 0, new int[dimensions.Length], serializer);

            return result;
        }


        /// <summary>
        /// Reads an N-dimensional array from the specified <see cref="JArray"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The array element type.
        /// </typeparam>
        /// <param name="element">
        ///   The <see cref="JArray"/> containing the array.
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
        /// <param name="serializer">
        ///   The serializer to use.
        /// </param>
        private static void ReadArray<T>(JArray element, Array array, int dimension, int[] indices, JsonSerializer serializer) {
            var i = 0;
            foreach (var el in element.Children()) {
                indices[dimension] = i;
                if (dimension + 1 == array.Rank) {
                    array.SetValue(el.ToObject<T>(serializer), indices);
                }
                else {
                    ReadArray<T>((JArray) el, array, dimension + 1, indices, serializer);
                }
                ++i;
            }
        }

    }
}
