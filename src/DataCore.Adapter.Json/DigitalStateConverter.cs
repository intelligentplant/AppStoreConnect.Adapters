using System;
using System.Text.Json;

using DataCore.Adapter.Tags;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="DigitalState"/>.
    /// </summary>
    public class DigitalStateConverter : AdapterJsonConverter<DigitalState> {


        /// <inheritdoc/>
        public override DigitalState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string name = null!;
            int value = -1;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(DigitalState.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(DigitalState.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<int>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return DigitalState.Create(name, value);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DigitalState value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(DigitalState.Name), value.Name, options);
            WritePropertyValue(writer, nameof(DigitalState.Value), value.Value, options);
            writer.WriteEndObject();
        }

    }
}
