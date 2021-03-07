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
            Variant value = Variant.Null;
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
                else if (string.Equals(propertyName, nameof(TagValue.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<Variant>(ref reader, options)!;
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

            return new TagValue(utcSampleTime, value, status, units);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagValue value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagValue.UtcSampleTime), value.UtcSampleTime, options);
            WritePropertyValue(writer, nameof(TagValue.Value), value.Value, options);
            WritePropertyValue(writer, nameof(TagValue.Status), value.Status, options);
            WritePropertyValue(writer, nameof(TagValue.Units), value.Units, options);
            writer.WriteEndObject();
        }

    }
}
