using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        /// The measurements associated with the node.
        /// </summary>
        public IEnumerable<AssetModelNodeMeasurement> Measurements { get; }

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
        /// <param name="description">
        ///   The node description.
        /// </param>
        /// <param name="parent">
        ///   The parent ID of the node. Specify <see langword="null"/> for top-level nodes.
        /// </param>
        /// <param name="hasChildren">
        ///   Specifies if this node has any children.
        /// </param>
        /// <param name="measurements">
        ///   The measurements associated with the node.
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
        public AssetModelNode(string id, string name, string description, string parent, bool hasChildren, IEnumerable<AssetModelNodeMeasurement> measurements, IEnumerable<AdapterProperty> properties) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Parent = parent;
            HasChildren = hasChildren;
            Measurements = measurements?.ToArray() ?? Array.Empty<AssetModelNodeMeasurement>();
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
        /// <param name="description">
        ///   The node description.
        /// </param>
        /// <param name="parent">
        ///   The parent ID of the node. Specify <see langword="null"/> for top-level nodes.
        /// </param>
        /// <param name="hasChildren">
        ///   Specifies if the node has any children.
        /// </param>
        /// <param name="measurements">
        ///   The measurements associated with the node.
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
        public static AssetModelNode Create(string id, string name, string description, string parent, bool hasChildren, IEnumerable<AssetModelNodeMeasurement> measurements, IEnumerable<AdapterProperty> properties) {
            return new AssetModelNode(id, name, description, parent, hasChildren, measurements, properties);
        }

    }
}
