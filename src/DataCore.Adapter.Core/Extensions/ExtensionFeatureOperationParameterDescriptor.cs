using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Describes an input or output parameter on an <see cref="ExtensionFeatureOperationDescriptor"/>.
    /// </summary>
    public class ExtensionFeatureOperationParameterDescriptor {

        /// <summary>
        /// The ordinal position of the parameter in the operation's <see cref="InvocationRequest"/>, 
        /// <see cref="InvocationStreamItem"/>, or <see cref="InvocationResponse"/> message.
        /// </summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// The type ID for the parameter type.
        /// </summary>
        public Uri? TypeId { get; set; }

        /// <summary>
        /// A description of the parameter value.
        /// </summary>
        public string? Description { get; set; }

    }

}
