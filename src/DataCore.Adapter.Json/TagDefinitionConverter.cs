using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="TagDefinition"/>.
    /// </summary>
    public class TagDefinitionConverter : AdapterJsonConverter<TagDefinition> {


        /// <inheritdoc/>
        public override TagDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null;
            string name = null;
            string description = null;
            string units = null;
            VariantType dataType = VariantType.Unknown;
            DigitalState[] states = null;
            AdapterProperty[] properties = null;
            string[] labels = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagDefinition.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.Units), StringComparison.OrdinalIgnoreCase)) {
                    units = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.DataType), StringComparison.OrdinalIgnoreCase)) {
                    dataType = JsonSerializer.Deserialize<VariantType>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.States), StringComparison.OrdinalIgnoreCase)) {
                    states = JsonSerializer.Deserialize<DigitalState[]>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.Labels), StringComparison.OrdinalIgnoreCase)) {
                    labels = JsonSerializer.Deserialize<string[]>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return TagDefinition.Create(id, name, description, units, dataType, states, properties, labels);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagDefinition value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagDefinition.Id), value.Id, options);
            WritePropertyValue(writer, nameof(TagDefinition.Name), value.Name, options);
            WritePropertyValue(writer, nameof(TagDefinition.Description), value.Description, options);
            WritePropertyValue(writer, nameof(TagDefinition.Units), value.Units, options);
            WritePropertyValue(writer, nameof(TagDefinition.DataType), value.DataType, options);
            WritePropertyValue(writer, nameof(TagDefinition.States), value.States, options);
            WritePropertyValue(writer, nameof(TagDefinition.Properties), value.Properties, options);
            WritePropertyValue(writer, nameof(TagDefinition.Labels), value.Labels, options);
            writer.WriteEndObject();
        }

    }
}
