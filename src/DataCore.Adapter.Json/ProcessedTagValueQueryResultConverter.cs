using System;
using System.Text.Json;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="TagValueQueryResult"/>.
    /// </summary>
    public class ProcessedTagValueQueryResultConverter : AdapterJsonConverter<ProcessedTagValueQueryResult> {


        /// <inheritdoc/>
        public override ProcessedTagValueQueryResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string tagId = null!;
            string tagName = null!;
            TagValueExtended value = null!;
            string dataFunction = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(ProcessedTagValueQueryResult.TagId), StringComparison.OrdinalIgnoreCase)) {
                    tagId = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(ProcessedTagValueQueryResult.TagName), StringComparison.OrdinalIgnoreCase)) {
                    tagName = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(ProcessedTagValueQueryResult.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<TagValueExtended>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(ProcessedTagValueQueryResult.DataFunction), StringComparison.OrdinalIgnoreCase)) {
                    dataFunction = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return ProcessedTagValueQueryResult.Create(tagId, tagName, value, dataFunction);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ProcessedTagValueQueryResult value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(ProcessedTagValueQueryResult.TagId), value.TagId, options);
            WritePropertyValue(writer, nameof(ProcessedTagValueQueryResult.TagName), value.TagName, options);
            WritePropertyValue(writer, nameof(ProcessedTagValueQueryResult.Value), value.Value, options);
            WritePropertyValue(writer, nameof(ProcessedTagValueQueryResult.DataFunction), value.DataFunction, options);
            writer.WriteEndObject();
        }

    }
}
