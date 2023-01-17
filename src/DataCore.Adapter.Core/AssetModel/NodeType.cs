using System.Text.Json.Serialization;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Describes the type of a node in an asset model.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Object is the best description")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NodeType {

        /// <summary>
        /// The node type is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The node is an object (i.e. a physical or conceptual entity, such as a piece of machinery).
        /// </summary>
        Object,

        /// <summary>
        /// The node is a variable (i.e. some sort of measurement).
        /// </summary>
        Variable,

        /// <summary>
        /// The node is an object type (i.e. a template for an object).
        /// </summary>
        ObjectType,

        /// <summary>
        /// The node is a variable type (i.e. a template for a measurement).
        /// </summary>
        VariableType,

        /// <summary>
        /// Any other node type not covered by the <see cref="NodeType"/> enumeration.
        /// </summary>
        Other

    }
}
