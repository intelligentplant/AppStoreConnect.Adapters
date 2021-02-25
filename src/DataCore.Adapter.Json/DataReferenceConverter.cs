using System;
using System.Text.Json;

using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="DataReference"/>.
    /// </summary>
    public class DataReferenceConverter : AdapterJsonConverter<DataReference> {

        /// <inheritdoc/>
        public override DataReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string adapterId = null!;
            string tag = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(DataReference.AdapterId), StringComparison.OrdinalIgnoreCase)) {
                    adapterId = reader.GetString()!;
                }
                else if (string.Equals(propertyName, nameof(DataReference.Tag), StringComparison.OrdinalIgnoreCase)) {
                    tag = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return new DataReference(adapterId, tag);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DataReference value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(DataReference.AdapterId), value.AdapterId, options);
            WritePropertyValue(writer, nameof(DataReference.Tag), value.Tag, options);

            writer.WriteEndObject();
        }
    }
}
