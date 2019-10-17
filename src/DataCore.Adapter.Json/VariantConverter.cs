using System;
using System.Buffers;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// System.Text.Json converter for <see cref="Variant"/>.
    /// </summary>
    public class VariantConverter : JsonConverter<Variant> { 

        /// <inheritdoc/>
        public override Variant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException(Resources.Error_InvalidVariantStructure);
            }

            VariantType? valueType = null;
            JsonElement valueElement = default;

            do {
                if (!reader.Read()) {
                    throw new JsonException(Resources.Error_InvalidVariantStructure);
                }

                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    throw new JsonException(Resources.Error_InvalidVariantStructure);
                }

                if (string.Equals(propertyName, nameof(Variant.Type), StringComparison.OrdinalIgnoreCase)) {
                    valueType = JsonSerializer.Deserialize<VariantType>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(Variant.Value), StringComparison.OrdinalIgnoreCase)) {
                    valueElement = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            } while (reader.TokenType != JsonTokenType.EndObject);

            if (valueType == VariantType.Null) {
                return Variant.Null;
            }

            switch (valueType) {
                case VariantType.Boolean:
                    return Variant.FromValue(valueElement.GetBoolean());
                case VariantType.Byte:
                    return Variant.FromValue(valueElement.GetByte());
                case VariantType.DateTime:
                    return Variant.FromValue(valueElement.GetDateTime());
                case VariantType.Double:
                    return Variant.FromValue(valueElement.GetDouble());
                case VariantType.Float:
                    return Variant.FromValue(valueElement.GetSingle());
                case VariantType.Int16:
                    return Variant.FromValue(valueElement.GetInt16());
                case VariantType.Int32:
                    return Variant.FromValue(valueElement.GetInt32());
                case VariantType.Int64:
                    return Variant.FromValue(valueElement.GetInt64());
                case VariantType.Object:
                    return Variant.FromValue(valueElement);
                case VariantType.SByte:
                    return Variant.FromValue(valueElement.GetSByte());
                case VariantType.String:
                    return Variant.FromValue(valueElement.GetString());
                case VariantType.TimeSpan:
                    return Variant.FromValue(TimeSpan.TryParse(valueElement.GetString(), out var ts) ? ts : default);
                case VariantType.UInt16:
                    return Variant.FromValue(valueElement.GetUInt16());
                case VariantType.UInt32:
                    return Variant.FromValue(valueElement.GetUInt32());
                case VariantType.UInt64:
                    return Variant.FromValue(valueElement.GetUInt64());
                case VariantType.Unknown:
                default:
                    return Variant.FromValue(valueElement);
            }
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Variant value, JsonSerializerOptions options) {
            if (writer == null) {
                return;
            }

            if (value.Type == VariantType.Null) {
                WriteNullValue(writer, options);
                return;
            }

            switch (value.Type) {
                case VariantType.Boolean:
                    WriteValue<bool>(writer, value, options);
                    break;
                case VariantType.Byte:
                    WriteValue<byte>(writer, value, options);
                    break;
                case VariantType.DateTime:
                    WriteValue<DateTime>(writer, value, options);
                    break;
                case VariantType.Double:
                    WriteValue<double>(writer, value, options);
                    break;
                case VariantType.Float:
                    WriteValue<float>(writer, value, options);
                    break;
                case VariantType.Int16:
                    WriteValue<short>(writer, value, options);
                    break;
                case VariantType.Int32:
                    WriteValue<int>(writer, value, options);
                    break;
                case VariantType.Int64:
                    WriteValue<long>(writer, value, options);
                    break;
                case VariantType.Object:
                    WriteValue<object>(writer, value, options);
                    break;
                case VariantType.SByte:
                    WriteValue<sbyte>(writer, value, options);
                    break;
                case VariantType.String:
                    WriteValue<string>(writer, value, options);
                    break;
                case VariantType.TimeSpan:
                    WriteValue<TimeSpan>(writer, value, options);
                    break;
                case VariantType.UInt16:
                    WriteValue<ushort>(writer, value, options);
                    break;
                case VariantType.UInt32:
                    WriteValue<uint>(writer, value, options);
                    break;
                case VariantType.UInt64:
                    WriteValue<ulong>(writer, value, options);
                    break;
                case VariantType.Unknown:
                default:
                    WriteValue<object>(writer, value, options);
                    break;

            }

        }


        /// <summary>
        /// Writes a "null" variant value.
        /// </summary>
        /// <param name="writer">
        ///   The JSON writer.
        /// </param>
        /// <param name="options">
        ///   The serializer options.
        /// </param>
        private void WriteNullValue(Utf8JsonWriter writer, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteString(options?.ConvertPropertyName(nameof(Variant.Type)), VariantType.Null.ToString());
            writer.WriteNull(options?.ConvertPropertyName(nameof(Variant.Value)));
            writer.WriteEndObject();
        }


        /// <summary>
        /// Writes a variant value of the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="writer">
        ///   The JSON writer.
        /// </param>
        /// <param name="value">
        ///   The value to write.
        /// </param>
        /// <param name="options">
        ///   The serializer options.
        /// </param>
        private void WriteValue<T>(Utf8JsonWriter writer, Variant value, JsonSerializerOptions options) {
            writer.WriteStartObject();

            writer.WritePropertyName(options?.ConvertPropertyName(nameof(Variant.Type)));
            JsonSerializer.Serialize(writer, value.Type, typeof(VariantType), options);

            writer.WritePropertyName(options?.ConvertPropertyName(nameof(Variant.Value)));
            if (typeof(T) == typeof(object)) {
                JsonSerializer.Serialize(writer, value.Value, typeof(T), options);
            }
            else if (typeof(T) == typeof(TimeSpan)) {
                JsonSerializer.Serialize(writer, value.Value.ToString(), typeof(string), options);
            }
            else {
                var converter = options?.GetConverter(typeof(T)) as JsonConverter<T>;
                if (converter == null) {
                    JsonSerializer.Serialize(writer, value.Value, typeof(T), options);
                }
                else {
                    converter.Write(writer, value.GetValueOrDefault<T>(), options);
                }
            }
            writer.WriteEndObject();

        }
    }
}
