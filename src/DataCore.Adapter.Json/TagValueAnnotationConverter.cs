using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="TagValueAnnotation"/>.
    /// </summary>
    public class TagValueAnnotationConverter : AdapterJsonConverter<TagValueAnnotation> {


        /// <inheritdoc/>
        public override TagValueAnnotation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            AnnotationType annotationType = AnnotationType.Instantaneous;
            DateTime utcStartTime = default;
            DateTime? utcEndTime = null!;
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

                if (string.Equals(propertyName, nameof(TagValueAnnotation.AnnotationType), StringComparison.OrdinalIgnoreCase)) {
                    annotationType = JsonSerializer.Deserialize<AnnotationType>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotation.UtcStartTime), StringComparison.OrdinalIgnoreCase)) {
                    utcStartTime = JsonSerializer.Deserialize<DateTime>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotation.UtcEndTime), StringComparison.OrdinalIgnoreCase)) {
                    utcEndTime = JsonSerializer.Deserialize<DateTime?>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotation.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotation.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotation.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return TagValueAnnotation.Create(annotationType, utcStartTime, utcEndTime, value, description, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagValueAnnotation value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagValueAnnotation.AnnotationType), value.AnnotationType, options);
            WritePropertyValue(writer, nameof(TagValueAnnotation.UtcStartTime), value.UtcStartTime, options);
            WritePropertyValue(writer, nameof(TagValueAnnotation.UtcEndTime), value.UtcEndTime, options);
            WritePropertyValue(writer, nameof(TagValueAnnotation.Value), value.Value, options);
            WritePropertyValue(writer, nameof(TagValueAnnotation.Description), value.Description, options);
            WritePropertyValue(writer, nameof(TagValueAnnotation.Properties), value.Properties, options);
            writer.WriteEndObject();
        }

    }
}
