using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="TagSummary"/>.
    /// </summary>
    public class TagSummaryConverter : AdapterJsonConverter<TagSummary> {


        /// <inheritdoc/>
        public override TagSummary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null;
            string name = null;
            string description = null;
            string units = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagSummary.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagSummary.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagSummary.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagSummary.Units), StringComparison.OrdinalIgnoreCase)) {
                    units = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return TagSummary.Create(id, name, description, units);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagSummary value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagSummary.Id), value.Id, options);
            WritePropertyValue(writer, nameof(TagSummary.Name), value.Name, options);
            WritePropertyValue(writer, nameof(TagSummary.Description), value.Description, options);
            WritePropertyValue(writer, nameof(TagSummary.Units), value.Units, options);
            writer.WriteEndObject();
        }

    }
}
