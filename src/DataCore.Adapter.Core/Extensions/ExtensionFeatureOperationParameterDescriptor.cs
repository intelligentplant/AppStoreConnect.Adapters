using System;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Describes an input or output parameter on an <see cref="ExtensionFeatureOperationDescriptor"/>.
    /// </summary>
    [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
    public class ExtensionFeatureOperationParameterDescriptor {

        /// <summary>
        /// The ordinal position of the parameter in the operation's <see cref="InvocationRequest"/>, 
        /// <see cref="InvocationStreamItem"/>, or <see cref="InvocationResponse"/> message.
        /// </summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// The <see cref="Common.VariantType"/> for the parameter.
        /// </summary>
        public Common.VariantType VariantType { get; set; }

        /// <summary>
        /// The array rank of the parameter. A value of less than one indicates that the parameter 
        /// is a single value rather than an array of values.
        /// </summary>
        public int ArrayRank { get; set; }

        /// <summary>
        /// The type ID for the parameter type, if <see cref="VariantType"/> is 
        /// <see cref="Common.VariantType.ExtensionObject"/>.
        /// </summary>
        public Uri? TypeId { get; set; }

        /// <summary>
        /// A description of the parameter value.
        /// </summary>
        public string? Description { get; set; }

    }

}
