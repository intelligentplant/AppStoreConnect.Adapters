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
        /// The input parameter descriptor for the operation.
        /// </summary>
        public ExtensionFeatureOperationParameterDescriptor Input { get; set; } = default!;

        /// <summary>
        /// The output parameter descriptor for the operation.
        /// </summary>
        public ExtensionFeatureOperationParameterDescriptor Output { get; set; } = default!;

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


    /// <summary>
    /// Describes an input or output parameter on an <see cref="ExtensionFeatureOperationDescriptor"/>.
    /// </summary>
    public class ExtensionFeatureOperationParameterDescriptor {

        /// <summary>
        /// A description of the parameter value.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// An example value.
        /// </summary>
        public string? ExampleValue { get; set; }

    }

}
