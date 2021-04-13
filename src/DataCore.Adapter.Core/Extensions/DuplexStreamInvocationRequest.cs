using System;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// A request to invoke an extension operation on an adapter of type <see cref="ExtensionFeatureOperationType.DuplexStream"/>.
    /// </summary>
    public class DuplexStreamInvocationRequest : AdapterRequest {

        /// <summary>
        /// The ID of the operation.
        /// </summary>
        [Required]
        public Uri OperationId { get; set; } = default!;

    }
}
