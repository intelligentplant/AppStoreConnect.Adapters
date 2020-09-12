using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {
    /// <summary>
    /// JSON converter for <see cref="WriteTagValueAnnotationResult"/>.
    /// </summary>
    public class WriteTagValueAnnotationResultConverter : AdapterJsonConverter<WriteTagValueAnnotationResult> {

        /// <inheritdoc/>
        public override WriteTagValueAnnotationResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string tagId = null!;
            string annotationId = null!;
            WriteStatus status = WriteStatus.Unknown;
            string notes = null!;
            AdapterProperty[] properties = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(WriteTagValueAnnotationResult.TagId), StringComparison.OrdinalIgnoreCase)) {
                    tagId = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(WriteTagValueAnnotationResult.AnnotationId), StringComparison.OrdinalIgnoreCase)) {
                    annotationId = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(WriteTagValueAnnotationResult.Status), StringComparison.OrdinalIgnoreCase)) {
                    status = JsonSerializer.Deserialize<WriteStatus>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(WriteTagValueAnnotationResult.Notes), StringComparison.OrdinalIgnoreCase)) {
                    notes = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(WriteTagValueAnnotationResult.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return WriteTagValueAnnotationResult.Create(tagId, annotationId, status, notes, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, WriteTagValueAnnotationResult value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(WriteTagValueAnnotationResult.TagId), value.TagId, options);
            WritePropertyValue(writer, nameof(WriteTagValueAnnotationResult.AnnotationId), value.AnnotationId, options);
            WritePropertyValue(writer, nameof(WriteTagValueAnnotationResult.Status), value.Status, options);
            WritePropertyValue(writer, nameof(WriteTagValueAnnotationResult.Notes), value.Notes, options);
            WritePropertyValue(writer, nameof(WriteTagValueAnnotationResult.Properties), value.Properties, options);

            writer.WriteEndObject();
        }

    }
}
