using System;
using System.Text.Json;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="TagValueAnnotationQueryResult"/>.
    /// </summary>
    public class TagValueAnnotationQueryResultConverter : AdapterJsonConverter<TagValueAnnotationQueryResult> {


        /// <inheritdoc/>
        public override TagValueAnnotationQueryResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string tagId = null!;
            string tagName = null!;
            TagValueAnnotationExtended annotation = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagValueAnnotationQueryResult.TagId), StringComparison.OrdinalIgnoreCase)) {
                    tagId = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationQueryResult.TagName), StringComparison.OrdinalIgnoreCase)) {
                    tagName = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationQueryResult.Annotation), StringComparison.OrdinalIgnoreCase)) {
                    annotation = JsonSerializer.Deserialize<TagValueAnnotationExtended>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return TagValueAnnotationQueryResult.Create(tagId, tagName, annotation);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagValueAnnotationQueryResult value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagValueAnnotationQueryResult.TagId), value.TagId, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationQueryResult.TagName), value.TagName, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationQueryResult.Annotation), value.Annotation, options);
            writer.WriteEndObject();
        }

    }
}
