using System;
using System.Text.Json;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Describes an operation on an extension adapter feature.
    /// </summary>
    public class ExtensionFeatureOperationDescriptor {

        /// <summary>
        /// The operation URI.
        /// </summary>
        public Uri OperationId { get; set; } = default!;

        /// <summary>
        /// The operation type.
        /// </summary>
        public ExtensionFeatureOperationType OperationType { get; set; }

        /// <summary>
        /// The name for the operation.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// The description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The JSON schema for the operation's request object.
        /// </summary>
        public JsonElement? RequestSchema { get; set; }

        /// <summary>
        /// The JSON schema for the operation's response object.
        /// </summary>
        public JsonElement? ResponseSchema { get; set; }

    }


    /// <summary>
    /// Describes the operation type for an extension adapter feature operation.
    /// </summary>
    public enum ExtensionFeatureOperationType {

        /// <summary>
        /// A request-response operation.
        /// </summary>
        Invoke,

        /// <summary>
        /// A request-streamed response operation.
        /// </summary>
        Stream,

        /// <summary>
        /// A streamed request-streamed response operation.
        /// </summary>
        DuplexStream

    }

}
