using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="TagIdentifier"/>.
    /// </summary>
    public class TagIdentifierConverter : AdapterJsonConverter<TagIdentifier> {


        /// <inheritdoc/>
        public override TagIdentifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null!;
            string name = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagIdentifier.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagIdentifier.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return TagIdentifier.Create(id, name);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagIdentifier value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagIdentifier.Id), value.Id, options);
            WritePropertyValue(writer, nameof(TagIdentifier.Name), value.Name, options);
            writer.WriteEndObject();
        }

    }
}
