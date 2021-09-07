using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="TagValue"/>.
    /// </summary>
    public class TagValueExtendedConverter : AdapterJsonConverter<TagValueExtended> {


        /// <inheritdoc/>
        public override TagValueExtended Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            DateTime utcSampleTime = default;
            Variant value = Variant.Null;
            StatusCode status = StatusCodes.Uncertain;
            string units = null!;
            string notes = null!;
            string error = null!;
            AdapterProperty[] properties = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagValueExtended.UtcSampleTime), StringComparison.OrdinalIgnoreCase)) {
                    utcSampleTime = JsonSerializer.Deserialize<DateTime>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueExtended.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<Variant>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueExtended.Status), StringComparison.OrdinalIgnoreCase)) {
                    status = JsonSerializer.Deserialize<StatusCode>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueExtended.Units), StringComparison.OrdinalIgnoreCase)) {
                    units = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueExtended.Notes), StringComparison.OrdinalIgnoreCase)) {
                    notes = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueExtended.Error), StringComparison.OrdinalIgnoreCase)) {
                    error = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueExtended.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return new TagValueExtended(utcSampleTime, value, status, units, notes, error, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagValueExtended value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagValueExtended.UtcSampleTime), value.UtcSampleTime, options);
            WritePropertyValue(writer, nameof(TagValueExtended.Value), value.Value, options);
            WritePropertyValue(writer, nameof(TagValueExtended.Status), value.Status, options);
            WritePropertyValue(writer, nameof(TagValueExtended.Units), value.Units, options);
            WritePropertyValue(writer, nameof(TagValueExtended.Notes), value.Notes, options);
            WritePropertyValue(writer, nameof(TagValueExtended.Error), value.Error, options);
            WritePropertyValue(writer, nameof(TagValueExtended.Properties), value.Properties, options);
            writer.WriteEndObject();
        }

    }
}
