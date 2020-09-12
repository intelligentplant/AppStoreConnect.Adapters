using System;
using System.Text.Json;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="TagValueQueryResult"/>.
    /// </summary>
    public class TagValueQueryResultConverter : AdapterJsonConverter<TagValueQueryResult> {


        /// <inheritdoc/>
        public override TagValueQueryResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string tagId = null!;
            string tagName = null!;
            TagValueExtended value = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagValueQueryResult.TagId), StringComparison.OrdinalIgnoreCase)) {
                    tagId = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueQueryResult.TagName), StringComparison.OrdinalIgnoreCase)) {
                    tagName = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueQueryResult.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<TagValueExtended>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return TagValueQueryResult.Create(tagId, tagName, value);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagValueQueryResult value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagValueQueryResult.TagId), value.TagId, options);
            WritePropertyValue(writer, nameof(TagValueQueryResult.TagName), value.TagName, options);
            WritePropertyValue(writer, nameof(TagValueQueryResult.Value), value.Value, options);
            writer.WriteEndObject();
        }

    }
}
