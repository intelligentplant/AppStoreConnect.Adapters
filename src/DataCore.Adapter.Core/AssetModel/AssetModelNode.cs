using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Describes a node in an adapter's asset model.
    /// </summary>
    [JsonConverter(typeof(AssetModelNodeConverter))]
    public class AssetModelNode {

        /// <summary>
        /// The unique identifier for the node.
        /// </summary>
        [Required]
        public string Id { get; }

        /// <summary>
        /// The node name.
        /// </summary>
        [Required]
        public string Name { get; }

        /// <summary>
        /// The type of the node.
        /// </summary>
        public NodeType NodeType { get; }

        /// <summary>
        /// The subtype of the node. This is a text value supplied by the adapter, intended to 
        /// provide more information about the node type (e.g. if the node represents a conceptual 
        /// item such as a folder in a hierarchical structure).
        /// </summary>
        public string? NodeSubType { get; }

        /// <summary>
        /// The node description.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// The parent ID of the node. Top-level nodes will have a value of <see langword="null"/>.
        /// </summary>
        public string? Parent { get; }

        /// <summary>
        /// Indicates if this node has any child nodes.
        /// </summary>
        public bool HasChildren { get; }

        /// <summary>
        /// The first data reference associated with the node.
        /// </summary>
        [Obsolete("Use " + nameof(DataReferences) + " instead.", false)]
        public DataReference? DataReference => DataReferences?.FirstOrDefault();

        /// <summary>
        /// The data references associated with the node.
        /// </summary>
        public DataReference[]? DataReferences { get; }

        /// <summary>
        /// Additional properties associated with the node.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; }


        /// <summary>
        /// Creates a new <see cref="AssetModelNode"/> object.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier for the node.
        /// </param>
        /// <param name="name">
        ///   The node name.
        /// </param>
        /// <param name="nodeType">
        ///   The node type.
        /// </param>
        /// <param name="nodeSubType">
        ///   The subtype of the node. This is a text value supplied by the adapter, intended to 
        ///   provide more information about the node type (e.g. if the node represents a conceptual 
        ///   item such as a folder in a hierarchical structure).
        /// </param>
        /// <param name="description">
        ///   The node description.
        /// </param>
        /// <param name="parent">
        ///   The parent ID of the node. Specify <see langword="null"/> for top-level nodes.
        /// </param>
        /// <param name="hasChildren">
        ///   Specifies if this node has any children.
        /// </param>
        /// <param name="dataReference">
        ///   The data reference associated with the node.
        /// </param>
        /// <param name="properties">
        ///   Additional node properties.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public AssetModelNode(
            string id, 
            string name, 
            NodeType nodeType,
            string? nodeSubType,
            string? description, 
            string? parent, 
            bool hasChildren, 
            DataReference? dataReference, 
            IEnumerable<AdapterProperty>? properties
        ) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            NodeType = nodeType;
            NodeSubType = nodeSubType;
            Description = description;
            Parent = parent;
            HasChildren = hasChildren;
            DataReferences = dataReference != null
                ? new[] { dataReference }
                : null;
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }


        /// <summary>
        /// Creates a new <see cref="AssetModelNode"/> object.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier for the node.
        /// </param>
        /// <param name="name">
        ///   The node name.
        /// </param>
        /// <param name="nodeType">
        ///   The node type.
        /// </param>
        /// <param name="nodeSubType">
        ///   The subtype of the node. This is a text value supplied by the adapter, intended to 
        ///   provide more information about the node type (e.g. if the node represents a conceptual 
        ///   item such as a folder in a hierarchical structure).
        /// </param>
        /// <param name="description">
        ///   The node description.
        /// </param>
        /// <param name="parent">
        ///   The parent ID of the node. Specify <see langword="null"/> for top-level nodes.
        /// </param>
        /// <param name="hasChildren">
        ///   Specifies if this node has any children.
        /// </param>
        /// <param name="dataReferences">
        ///   The data references associated with the node.
        /// </param>
        /// <param name="properties">
        ///   Additional node properties.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public AssetModelNode(
            string id,
            string name,
            NodeType nodeType,
            string? nodeSubType,
            string? description,
            string? parent,
            bool hasChildren,
            IEnumerable<DataReference>? dataReferences,
            IEnumerable<AdapterProperty>? properties
        ) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            NodeType = nodeType;
            NodeSubType = nodeSubType;
            Description = description;
            Parent = parent;
            HasChildren = hasChildren;
            DataReferences = dataReferences?.ToArray();
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }

    }


    /// <summary>
    /// JSON converter for <see cref="AssetModelNode"/>.
    /// </summary>
    internal class AssetModelNodeConverter : AdapterJsonConverter<AssetModelNode> {

        /// <inheritdoc/>
        public override AssetModelNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null!;
            string name = null!;
            NodeType nodeType = NodeType.Unknown;
            string? nodeSubType = null;
            string? description = null;
            string? parent = null;
            bool hasChildren = false;
            DataReference? dataReference = null;
            DataReference[]? dataReferences = null;
            AdapterProperty[]? properties = null;

            var propertyNameComparer = options.PropertyNameCaseInsensitive
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            var startDepth = reader.CurrentDepth;

            do {
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, ConvertPropertyName(nameof(AssetModelNode.Id), options), propertyNameComparer)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, ConvertPropertyName(nameof(AssetModelNode.Name), options), propertyNameComparer)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, ConvertPropertyName(nameof(AssetModelNode.NodeType), options), propertyNameComparer)) {
                    nodeType = JsonSerializer.Deserialize<NodeType>(ref reader, options);
                }
                else if (string.Equals(propertyName, ConvertPropertyName(nameof(AssetModelNode.NodeSubType), options), propertyNameComparer)) {
                    nodeSubType = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, ConvertPropertyName(nameof(AssetModelNode.Description), options), propertyNameComparer)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, ConvertPropertyName(nameof(AssetModelNode.Parent), options), propertyNameComparer)) {
                    parent = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, ConvertPropertyName(nameof(AssetModelNode.HasChildren), options), propertyNameComparer)) {
                    hasChildren = JsonSerializer.Deserialize<bool>(ref reader, options);
                }
#pragma warning disable CS0618 // Type or member is obsolete
                else if (string.Equals(propertyName, ConvertPropertyName(nameof(AssetModelNode.DataReference), options), propertyNameComparer)) {
                    // Single data reference might be present when deserializing a pre v3.0 asset
                    // model node.
                    dataReference = JsonSerializer.Deserialize<DataReference>(ref reader, options);
                }
#pragma warning restore CS0618 // Type or member is obsolete
                else if (string.Equals(propertyName, ConvertPropertyName(nameof(AssetModelNode.DataReferences), options), propertyNameComparer)) {
                    dataReferences = JsonSerializer.Deserialize<DataReference[]>(ref reader, options);
                }
                else if (string.Equals(propertyName, ConvertPropertyName(nameof(AssetModelNode.Properties), options), propertyNameComparer)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            } while (reader.CurrentDepth != startDepth || reader.TokenType != JsonTokenType.EndObject);

            return dataReference == null
                ? new AssetModelNode(id, name, nodeType, nodeSubType, description, parent, hasChildren, dataReferences, properties)
                : new AssetModelNode(id, name, nodeType, nodeSubType, description, parent, hasChildren, dataReference, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, AssetModelNode value, JsonSerializerOptions options) {
            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(AssetModelNode.Id), value.Id, options);
            WritePropertyValue(writer, nameof(AssetModelNode.Name), value.Name, options);
            WritePropertyValue(writer, nameof(AssetModelNode.NodeType), value.NodeType, options);
            WritePropertyValue(writer, nameof(AssetModelNode.NodeSubType), value.NodeSubType, options);
            WritePropertyValue(writer, nameof(AssetModelNode.Description), value.Description, options);
            WritePropertyValue(writer, nameof(AssetModelNode.Parent), value.Parent, options);
            WritePropertyValue(writer, nameof(AssetModelNode.HasChildren), value.HasChildren, options);
            WritePropertyValue(writer, nameof(AssetModelNode.DataReferences), value.DataReferences, options);
            WritePropertyValue(writer, nameof(AssetModelNode.Properties), value.Properties, options);

            writer.WriteEndObject();
        }

    }

}
