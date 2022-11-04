using System;
using System.Collections.Generic;
using System.Linq;

using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Helper class for constructing <see cref="AssetModelNode"/> objects using a fluent interface.
    /// </summary>
    public class AssetModelNodeBuilder {

        /// <summary>
        /// The node ID.
        /// </summary>
        private string? _id;

        /// <summary>
        /// The node name.
        /// </summary>
        private string? _name;

        /// <summary>
        /// The node type.
        /// </summary>
        private NodeType _nodeType;

        /// <summary>
        /// The node sub type.
        /// </summary>
        private string? _nodeSubType;

        /// <summary>
        /// The node description.
        /// </summary>
        private string? _description;

        /// <summary>
        /// The ndoe's parent ID.
        /// </summary>
        private string? _parentId;

        /// <summary>
        /// Flags if the node has children.
        /// </summary>
        private bool _hasChildren;

        /// <summary>
        /// The node's data reference.
        /// </summary>
        private readonly List<DataReference> _dataReferences = new List<DataReference>();

        /// <summary>
        /// Bespoke node properties.
        /// </summary>
        private readonly List<AdapterProperty> _properties = new List<AdapterProperty>();


        /// <summary>
        /// Creates a new <see cref="AssetModelNodeBuilder"/> object.
        /// </summary>
        public AssetModelNodeBuilder() { }


        /// <summary>
        /// Creates a new <see cref="AssetModelNodeBuilder"/> object that is initialised using an 
        /// existing node definition.
        /// </summary>
        /// <param name="existing">
        ///   The existing node definition.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="existing"/> is <see langword="null"/>.
        /// </exception>
        public AssetModelNodeBuilder(AssetModelNode existing) {
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }

            WithId(existing.Id);
            WithName(existing.Name);
            WithNodeType(existing.NodeType, existing.NodeSubType);
            WithDescription(existing.Description);
            WithParent(existing.Parent);
            WithChildren(existing.HasChildren);
            if (existing.DataReferences != null) {
                foreach (var item in existing.DataReferences) {
                    _dataReferences.Add(item);
                }
            }
            WithProperties(existing.Properties);
        }


        /// <summary>
        /// Creates a new <see cref="AssetModelNodeBuilder"/> object.
        /// </summary>
        public static AssetModelNodeBuilder Create() {
            return new AssetModelNodeBuilder();
        }


        /// <summary>
        /// Creates a new <see cref="AssetModelNodeBuilder"/> object that is initialised using an 
        /// existing node definition.
        /// </summary>
        /// <param name="other">
        ///   The existing node definition.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="other"/> is <see langword="null"/>.
        /// </exception>
        public static AssetModelNodeBuilder CreateFromExisting(AssetModelNode other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            return new AssetModelNodeBuilder(other);
        }


        /// <summary>
        /// Creates a <see cref="AssetModelNode"/> using the configured settings.
        /// </summary>
        /// <returns>
        ///   A new <see cref="AssetModelNode"/> object.
        /// </returns>
        public AssetModelNode Build() {
            return new AssetModelNode(
                _id!, 
                _name!, 
                _nodeType, 
                _nodeSubType, 
                _description, 
                _parentId, 
                _hasChildren, 
                _dataReferences.Count == 0 
                    ? null 
                    : _dataReferences, 
                _properties
            );
        }


        /// <summary>
        /// Updates the node ID.
        /// </summary>
        /// <param name="id">
        ///   The node ID.
        /// </param>
        /// <returns>
        ///   The updated <see cref="AssetModelNodeBuilder"/>.
        /// </returns>
        public AssetModelNodeBuilder WithId(string id) {
            _id = id;
            return this;
        }


        /// <summary>
        /// Updates the node name.
        /// </summary>
        /// <param name="name">
        ///   The node name.
        /// </param>
        /// <returns>
        ///   The updated <see cref="AssetModelNodeBuilder"/>.
        /// </returns>
        public AssetModelNodeBuilder WithName(string name) {
            _name = name;
            return this;
        }


        /// <summary>
        /// Updates the node type and sub-type.
        /// </summary>
        /// <param name="type">
        ///   The node type.
        /// </param>
        /// <param name="subType">
        ///   The node sub-type.
        /// </param>
        /// <returns>
        ///   The updated <see cref="AssetModelNodeBuilder"/>.
        /// </returns>
        public AssetModelNodeBuilder WithNodeType(NodeType type, string? subType = null) {
            _nodeType = type;
            _nodeSubType = subType;
            return this;
        }


        /// <summary>
        /// Updates the node description.
        /// </summary>
        /// <param name="description">
        ///   The node description.
        /// </param>
        /// <returns>
        ///   The updated <see cref="AssetModelNodeBuilder"/>.
        /// </returns>
        public AssetModelNodeBuilder WithDescription(string? description) {
            _description = description;
            return this;
        }


        /// <summary>
        /// Updates the parent node ID.
        /// </summary>
        /// <param name="parentId">
        ///   The parent node ID.
        /// </param>
        /// <returns>
        ///   The updated <see cref="AssetModelNodeBuilder"/>.
        /// </returns>
        public AssetModelNodeBuilder WithParent(string? parentId) {
            _parentId = parentId;
            return this;
        }


        /// <summary>
        /// Updates the flag indicating if the node has children.
        /// </summary>
        /// <param name="hasChildren">
        ///   The flag indicating if the node has children.
        /// </param>
        /// <returns>
        ///   The updated <see cref="AssetModelNodeBuilder"/>.
        /// </returns>
        public AssetModelNodeBuilder WithChildren(bool hasChildren) {
            _hasChildren = hasChildren;
            return this;
        }


        /// <summary>
        /// Adds a collection of data references to the node.
        /// </summary>
        /// <param name="dataReferences">
        ///   The data references.
        /// </param>
        /// <returns>
        ///   The updated <see cref="AssetModelNodeBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="dataReferences"/> is <see langword="null"/>.
        /// </exception>
        public AssetModelNodeBuilder WithDataReferences(params DataReference[] dataReferences) {
            return WithDataReferences((IEnumerable<DataReference>) dataReferences);
        }


        /// <summary>
        /// Adds a collection of data references to the node.
        /// </summary>
        /// <param name="dataReferences">
        ///   The data references.
        /// </param>
        /// <returns>
        ///   The updated <see cref="AssetModelNodeBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="dataReferences"/> is <see langword="null"/>.
        /// </exception>
        public AssetModelNodeBuilder WithDataReferences(IEnumerable<DataReference> dataReferences) {
            if (dataReferences == null) {
                throw new ArgumentNullException(nameof(dataReferences));
            }
            foreach (var item in dataReferences) {
                if (item == null) {
                    continue;
                }
                WithDataReference(item);
            }
            return this;
        }


        /// <summary>
        /// Adds a data reference to the node.
        /// </summary>
        /// <param name="dataReference">
        ///   The data reference.
        /// </param>
        /// <returns>
        ///   The updated <see cref="AssetModelNodeBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="dataReference"/> is <see langword="null"/>.
        /// </exception>
        public AssetModelNodeBuilder WithDataReference(DataReference dataReference) {
            _dataReferences.Add(dataReference ?? throw new ArgumentNullException(nameof(dataReference)));
            return this;
        }


        /// <summary>
        /// Adds a data reference to the node.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID for the data reference.
        /// </param>
        /// <param name="tag">
        ///   The tag identifier for the data reference.
        /// </param>
        /// <param name="name">
        ///   The optional display name for the data reference.
        /// </param>
        /// <returns>
        ///   The updated <see cref="AssetModelNodeBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        public AssetModelNodeBuilder WithDataReference(string adapterId, TagSummary tag, string? name = null) {
            _dataReferences.Add(new DataReference(adapterId, tag?.Name!, name));
            return this;
        }


        /// <summary>
        /// Adds a data reference to the node.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID for the data reference.
        /// </param>
        /// <param name="tag">
        ///   The tag ID or name for the data reference.
        /// </param>
        /// <param name="name">
        ///   The optional display name for the data reference.
        /// </param>
        /// <returns>
        ///   The updated <see cref="AssetModelNodeBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        public AssetModelNodeBuilder WithDataReference(
            string adapterId, 
            string tag,
            string? name = null
        ) {
            _dataReferences.Add(new DataReference(adapterId, tag, name));
            return this;
        }


        /// <summary>
        /// Adds a property to the node.
        /// </summary>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The property value.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public AssetModelNodeBuilder WithProperty(string name, object value) {
            if (name != null) {
                _properties.Add(AdapterProperty.Create(name, value));
            }
            return this;
        }


        /// <summary>
        /// Adds a set of properties to the node.
        /// </summary>
        /// <param name="properties">
        ///   The properties.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public AssetModelNodeBuilder WithProperties(params AdapterProperty[] properties) {
            return WithProperties((IEnumerable<AdapterProperty>) properties);
        }


        /// <summary>
        /// Adds a set of properties to the node.
        /// </summary>
        /// <param name="properties">
        ///   The properties.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        public AssetModelNodeBuilder WithProperties(IEnumerable<AdapterProperty> properties) {
            if (properties != null) {
                _properties.AddRange(properties.Where(x => x != null));
            }
            return this;
        }

    }
}
