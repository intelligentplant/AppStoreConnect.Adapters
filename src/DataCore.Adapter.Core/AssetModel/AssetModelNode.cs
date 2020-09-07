using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Describes a node in an adapter's asset model.
    /// </summary>
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
        public string NodeSubType { get; }

        /// <summary>
        /// The node description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The parent ID of the node. Top-level nodes will have a value of <see langword="null"/>.
        /// </summary>
        public string Parent { get; }

        /// <summary>
        /// Indicates if this node has any child nodes.
        /// </summary>
        public bool HasChildren { get; }

        /// <summary>
        /// The data reference associated with the node.
        /// </summary>
        public DataReference DataReference { get; }

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
            string nodeSubType,
            string description, 
            string parent, 
            bool hasChildren, 
            DataReference dataReference, 
            IEnumerable<AdapterProperty> properties
        ) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            NodeType = nodeType;
            NodeSubType = nodeSubType;
            Description = description;
            Parent = parent;
            HasChildren = hasChildren;
            DataReference = NodeType == NodeType.Variable
                ? dataReference
                : null;
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }

    }
}
