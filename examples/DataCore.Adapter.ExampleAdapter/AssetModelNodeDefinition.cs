using System.Collections.Generic;

using DataCore.Adapter.AssetModel;

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
        /// The node type.
        /// </summary>
        public NodeType NodeType { get; set; }

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
        /// The tag associated with the node.
        /// </summary>
        public string DataReference { get; set; }

        /// <summary>
        /// Additional properties associated with the node.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }

    }

}
