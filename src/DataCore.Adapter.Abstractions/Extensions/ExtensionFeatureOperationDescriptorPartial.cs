using System;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Describes an operation on an extension adapter feature.
    /// </summary>
    public class ExtensionFeatureOperationDescriptorPartial {

        /// <summary>
        /// The name for the operation.
        /// </summary>
        public string? Name { get; set; }

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

}
