using System;

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
        /// The display name.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// The description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The input parameter descriptors for the operation.
        /// </summary>
        public ExtensionFeatureOperationParameterDescriptor[] Inputs { get; set; } = Array.Empty<ExtensionFeatureOperationParameterDescriptor>();

        /// <summary>
        /// The output parameter descriptors for the operation.
        /// </summary>
        public ExtensionFeatureOperationParameterDescriptor[] Outputs { get; set; } = Array.Empty<ExtensionFeatureOperationParameterDescriptor>();

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
