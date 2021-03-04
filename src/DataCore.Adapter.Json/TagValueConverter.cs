using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="TagValue"/>.
    /// </summary>
    public class TagValueConverter : AdapterJsonConverter<TagValue> {

        /// <inheritdoc/>
        public override TagValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            DateTime utcSampleTime = default;
            Variant? value = null; 
            Variant[] values = null!;
            TagValueStatus status = TagValueStatus.Uncertain;
            string units = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagValue.UtcSampleTime), StringComparison.OrdinalIgnoreCase)) {
                    utcSampleTime = JsonSerializer.Deserialize<DateTime>(ref reader, options);
                }
                // Allow a "Value" property with a single value for backwards compatibility.
                else if (string.Equals(propertyName, "Value", StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<Variant>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValue.Values), StringComparison.OrdinalIgnoreCase)) {
                    values = JsonSerializer.Deserialize<Variant[]>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValue.Status), StringComparison.OrdinalIgnoreCase)) {
                    status = JsonSerializer.Deserialize<TagValueStatus>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValue.Units), StringComparison.OrdinalIgnoreCase)) {
                    units = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return value == null 
                ? new TagValue(utcSampleTime, values, status, units)
                : new TagValue(utcSampleTime, new[] { value.Value }, status, units);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagValue value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagValue.UtcSampleTime), value.UtcSampleTime, options);
            WritePropertyValue(writer, nameof(TagValue.Values), value.Values, options);
            WritePropertyValue(writer, nameof(TagValue.Status), value.Status, options);
            WritePropertyValue(writer, nameof(TagValue.Units), value.Units, options);
            writer.WriteEndObject();
        }

    }
}
