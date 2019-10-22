using System;
using System.Text.Json;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="AdapterDescriptor"/>.
    /// </summary>
    public class AdapterDescriptorConverter : AdapterJsonConverter<AdapterDescriptor> {

        /// <inheritdoc/>
        public override AdapterDescriptor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null;
            string name = null;
            string description = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(AdapterDescriptor.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = reader.GetString();
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptor.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = reader.GetString();
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptor.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = reader.GetString();
                }
                else {
                    reader.Skip();
                }
            }

            return AdapterDescriptor.Create(id, name, description);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, AdapterDescriptor value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WriteString(ConvertPropertyName(nameof(AdapterDescriptor.Id), options), value.Id);
            writer.WriteString(ConvertPropertyName(nameof(AdapterDescriptor.Name), options), value.Name);
            writer.WriteString(ConvertPropertyName(nameof(AdapterDescriptor.Description), options), value.Description);

            writer.WriteEndObject();
        }

    }
}
