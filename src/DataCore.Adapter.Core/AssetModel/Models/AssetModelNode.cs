using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataCore.Adapter.AssetModel.Models {

    /// <summary>
    /// Describes a node in an adapter's asset model.
    /// </summary>
    public class AssetModelNode {

        /// <summary>
        /// The unique identifier for the node.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The node name.
        /// </summary>
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
        /// The IDs of the child nodes of this node.
        /// </summary>
        public IEnumerable<string> Children { get; }

        /// <summary>
        /// The measurements associated with the node.
        /// </summary>
        public IEnumerable<AssetModelNodeMeasurement> Measurements { get; }

        /// <summary>
        /// Additional properties associated with the node.
        /// </summary>
        public IDictionary<string, string> Properties { get; }


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
        /// <param name="parentId">
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
        public AssetModelNode(string id, string name, string description, string parentId, IEnumerable<string> children, IEnumerable<AssetModelNodeMeasurement> measurements, IDictionary<string, string> properties) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Parent = parentId;
            Children = children?.ToArray() ?? Array.Empty<string>();
            Measurements = measurements?.ToArray() ?? Array.Empty<AssetModelNodeMeasurement>();
            Properties = new ReadOnlyDictionary<string, string>(properties ?? new Dictionary<string, string>());
        }

    }
}
