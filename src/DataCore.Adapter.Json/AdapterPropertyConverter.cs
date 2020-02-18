using System;
using System.Text.Json;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="AdapterProperty"/>.
    /// </summary>
    public class AdapterPropertyConverter : AdapterJsonConverter<AdapterProperty> {

        /// <inheritdoc/>
        public override AdapterProperty Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string name = null;
            Variant value = Variant.Null;
            string description = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(AdapterProperty.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AdapterProperty.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<Variant>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AdapterProperty.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return AdapterProperty.Create(name, value, description);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, AdapterProperty value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(AdapterProperty.Name), value.Name, options);
            WritePropertyValue(writer, nameof(AdapterProperty.Value), value.Value, options);
            WritePropertyValue(writer, nameof(AdapterProperty.Description), value.Description, options);

            writer.WriteEndObject();
        }

    }
}
