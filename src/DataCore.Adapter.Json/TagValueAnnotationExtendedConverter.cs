using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="TagValueAnnotationExtended"/>.
    /// </summary>
    public class TagValueAnnotationExtendedConverter : AdapterJsonConverter<TagValueAnnotationExtended> {


        /// <inheritdoc/>
        public override TagValueAnnotationExtended Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null!;
            AnnotationType annotationType = AnnotationType.Instantaneous;
            DateTime utcStartTime = default;
            DateTime? utcEndTime = null;
            string value = null!;
            string description = null!;
            AdapterProperty[] properties = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.AnnotationType), StringComparison.OrdinalIgnoreCase)) {
                    annotationType = JsonSerializer.Deserialize<AnnotationType>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.UtcStartTime), StringComparison.OrdinalIgnoreCase)) {
                    utcStartTime = JsonSerializer.Deserialize<DateTime>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.UtcEndTime), StringComparison.OrdinalIgnoreCase)) {
                    utcEndTime = JsonSerializer.Deserialize<DateTime?>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return TagValueAnnotationExtended.Create(id, annotationType, utcStartTime, utcEndTime, value, description, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagValueAnnotationExtended value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.Id), value.Id, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.AnnotationType), value.AnnotationType, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.UtcStartTime), value.UtcStartTime, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.UtcEndTime), value.UtcEndTime, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.Value), value.Value, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.Description), value.Description, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.Properties), value.Properties, options);
            writer.WriteEndObject();
        }

    }
}
