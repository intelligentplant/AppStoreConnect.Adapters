using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// A request to invoke an extension operation on an adapter of type <see cref="ExtensionFeatureOperationType.Invoke"/> 
    /// or <see cref="ExtensionFeatureOperationType.Stream"/>. 
    /// </summary>
    public class InvocationRequest : AdapterRequest {

        /// <summary>
        /// The ID of the operation.
        /// </summary>
        [Required]
        public Uri OperationId { get; set; } = default!;

        /// <summary>
        /// The invocation arguments.
        /// </summary>
        public JsonElement? Arguments { get; set; }

    }

}
