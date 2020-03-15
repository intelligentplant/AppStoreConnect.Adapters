using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="DataFunctionDescriptor"/>.
    /// </summary>
    public class DataFunctionDescriptorConverter : AdapterJsonConverter<DataFunctionDescriptor> {


        /// <inheritdoc/>
        public override DataFunctionDescriptor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null;
            string name = null;
            string description = null;
            AdapterProperty[] properties = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(DataFunctionDescriptor.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(DataFunctionDescriptor.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(DataFunctionDescriptor.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(DataFunctionDescriptor.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return DataFunctionDescriptor.Create(id, name, description, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DataFunctionDescriptor value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(DataFunctionDescriptor.Id), value.Id, options);
            WritePropertyValue(writer, nameof(DataFunctionDescriptor.Name), value.Name, options);
            WritePropertyValue(writer, nameof(DataFunctionDescriptor.Description), value.Description, options);
            WritePropertyValue(writer, nameof(DataFunctionDescriptor.Properties), value.Properties, options);
            writer.WriteEndObject();
        }

    }
}
