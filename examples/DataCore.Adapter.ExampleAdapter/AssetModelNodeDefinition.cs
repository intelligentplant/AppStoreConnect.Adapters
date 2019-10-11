using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Example {

    /// <summary>
    /// Describes an asset model node.
    /// </summary>
    internal class AssetModelNodeDefinition {

        /// <summary>
        /// The unique identifier for the node.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The node name.
        /// </summary>
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
        public IEnumerable<AssetModelNodeMeasurementDefinition> Measurements { get; set; }

        /// <summary>
        /// Additional properties associated with the node.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }

    }


    /// <summary>
    /// Describes a measurement on an asset model node.
    /// </summary>
    internal class AssetModelNodeMeasurementDefinition {

        /// <summary>
        /// The measurement name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The tag ID or name.
        /// </summary>
        public string Tag { get; set; }

    }
}
