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

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(AdapterProperty.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = reader.GetString();
                }
                else if (string.Equals(propertyName, nameof(AdapterProperty.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<Variant>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return AdapterProperty.Create(name, value);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, AdapterProperty value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WriteString(ConvertPropertyName(nameof(AdapterProperty.Name), options), value.Name);
            writer.WritePropertyName(ConvertPropertyName(nameof(AdapterProperty.Value), options));
            JsonSerializer.Serialize(writer, value.Value, options);

            writer.WriteEndObject();
        }

    }
}
