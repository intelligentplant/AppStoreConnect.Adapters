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
            NodeType nodeType = NodeType.Unknown;
            string description = null;
            string parent = null;
            bool hasChildren = false;
            DataReference dataReference = null;
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
                else if (string.Equals(propertyName, nameof(AssetModelNode.NodeType), StringComparison.OrdinalIgnoreCase)) {
                    nodeType = JsonSerializer.Deserialize<NodeType>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AssetModelNode.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AssetModelNode.Parent), StringComparison.OrdinalIgnoreCase)) {
                    parent = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AssetModelNode.HasChildren), StringComparison.OrdinalIgnoreCase)) {
                    hasChildren = JsonSerializer.Deserialize<bool>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AssetModelNode.DataReference), StringComparison.OrdinalIgnoreCase)) {
                    dataReference = JsonSerializer.Deserialize<DataReference>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AssetModelNode.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return new AssetModelNode(id, name, nodeType, description, parent, hasChildren, dataReference, properties);
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
            WritePropertyValue(writer, nameof(AssetModelNode.NodeType), value.NodeType, options);
            WritePropertyValue(writer, nameof(AssetModelNode.Description), value.Description, options);
            WritePropertyValue(writer, nameof(AssetModelNode.Parent), value.Parent, options);
            WritePropertyValue(writer, nameof(AssetModelNode.HasChildren), value.HasChildren, options);
            WritePropertyValue(writer, nameof(AssetModelNode.DataReference), value.DataReference, options);
            WritePropertyValue(writer, nameof(AssetModelNode.Properties), value.Properties, options);
            writer.WriteEndObject();
        }

    }
}
