using System;
using System.Text.Json;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="AdapterDescriptorExtended"/>.
    /// </summary>
    public class AdapterDescriptorExtendedConverter : AdapterJsonConverter<AdapterDescriptorExtended> {

        /// <inheritdoc/>
        public override AdapterDescriptorExtended Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null!;
            string name = null!;
            string description = null!;
            string[] features = null!;
            string[] extensions = null!;
            AdapterProperty[] properties = null!;
            AdapterTypeDescriptor typeDescriptor = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.Features), StringComparison.OrdinalIgnoreCase)) {
                    features = JsonSerializer.Deserialize<string[]>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.Extensions), StringComparison.OrdinalIgnoreCase)) {
                    extensions = JsonSerializer.Deserialize<string[]>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.TypeDescriptor), StringComparison.OrdinalIgnoreCase)) {
                    typeDescriptor = JsonSerializer.Deserialize<AdapterTypeDescriptor>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return AdapterDescriptorExtended.Create(id, name, description, features, extensions, properties, typeDescriptor);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, AdapterDescriptorExtended value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.Id), value.Id, options);
            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.Name), value.Name, options);
            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.Description), value.Description, options);
            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.Features), value.Features, options);
            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.Extensions), value.Extensions, options);
            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.Properties), value.Properties, options);
            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.TypeDescriptor), value.TypeDescriptor, options);

            writer.WriteEndObject();
        }

    }
}
