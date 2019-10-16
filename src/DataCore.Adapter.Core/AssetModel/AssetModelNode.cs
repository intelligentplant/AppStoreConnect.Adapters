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
        public string Id { get; set; }

        /// <summary>
        /// The node name.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The node description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The parent ID of the node. Top-level nodes will have a value of <see langword="null"/>.
        /// </summary>
        public string Parent { get; set; }

        /// <summary>
        /// The IDs of the child nodes of this node.
        /// </summary>
        public IEnumerable<string> Children { get; set; }

        /// <summary>
        /// The measurements associated with the node.
        /// </summary>
        public IEnumerable<AssetModelNodeMeasurement> Measurements { get; set; }

        /// <summary>
        /// Additional properties associated with the node.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; set; }


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
        /// <param name="children">
        ///   The IDs of the node's children.
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
        public static AssetModelNode Create(string id, string name, string description, string parent, IEnumerable<string> children, IEnumerable<AssetModelNodeMeasurement> measurements, IEnumerable<AdapterProperty> properties) {
            return new AssetModelNode() {
                Id = id ?? throw new ArgumentNullException(nameof(id)),
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                Description = description,
                Parent = parent,
                Children = children?.ToArray() ?? Array.Empty<string>(),
                Measurements = measurements?.ToArray() ?? Array.Empty<AssetModelNodeMeasurement>(),
                Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>()
            };
        }

    }
}
