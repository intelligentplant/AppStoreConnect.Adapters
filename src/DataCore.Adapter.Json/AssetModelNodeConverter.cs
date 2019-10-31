using System;
using System.Text.Json;
using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="AssetModelNode"/>.
    /// </summary>
    public class AssetModelNodeConverter : AdapterJsonConverter<AssetModelNode> {


        /// <inheritdoc/>
        public override AssetModelNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null;
            string name = null;
            string description = null;
            string parent = null;
            string[] children = null;
            AssetModelNodeMeasurement[] measurements = null;
            AdapterProperty[] properties = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(AssetModelNode.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AssetModelNode.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AssetModelNode.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AssetModelNode.Parent), StringComparison.OrdinalIgnoreCase)) {
                    parent = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AssetModelNode.Children), StringComparison.OrdinalIgnoreCase)) {
                    children = JsonSerializer.Deserialize<string[]>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AssetModelNode.Measurements), StringComparison.OrdinalIgnoreCase)) {
                    measurements = JsonSerializer.Deserialize<AssetModelNodeMeasurement[]>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AssetModelNode.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return AssetModelNode.Create(id, name, description, parent, children, measurements, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, AssetModelNode value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(AssetModelNode.Id), value.Id, options);
            WritePropertyValue(writer, nameof(AssetModelNode.Name), value.Name, options);
            WritePropertyValue(writer, nameof(AssetModelNode.Description), value.Description, options);
            WritePropertyValue(writer, nameof(AssetModelNode.Parent), value.Parent, options);
            WritePropertyValue(writer, nameof(AssetModelNode.Children), value.Children, options);
            WritePropertyValue(writer, nameof(AssetModelNode.Measurements), value.Measurements, options);
            WritePropertyValue(writer, nameof(AssetModelNode.Properties), value.Properties, options);
            writer.WriteEndObject();
        }

    }
}
