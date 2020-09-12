using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="DigitalStateSet"/>.
    /// </summary>
    public class DigitalStateSetConverter : AdapterJsonConverter<DigitalStateSet> {

        /// <inheritdoc/>
        public override DigitalStateSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null!;
            string name = null!;
            DigitalState[] states = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(DigitalStateSet.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(DigitalStateSet.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(DigitalStateSet.States), StringComparison.OrdinalIgnoreCase)) {
                    states = JsonSerializer.Deserialize<DigitalState[]>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return DigitalStateSet.Create(id, name, states);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DigitalStateSet value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(DigitalStateSet.Id), value.Id, options);
            WritePropertyValue(writer, nameof(DigitalStateSet.Name), value.Name, options);
            WritePropertyValue(writer, nameof(DigitalStateSet.States), value.States, options);
            writer.WriteEndObject();
        }

    }
}
