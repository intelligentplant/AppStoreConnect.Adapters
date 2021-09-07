using System;
using System.Text.Json;

using DataCore.Adapter.Common;

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
        /// The JSON schema for the operation's request payload.
        /// </summary>
        public JsonElement? RequestSchema { get; set; }

        /// <summary>
        /// The JSON schema for the operation's response payload.
        /// </summary>
        public JsonElement? ResponseSchema { get; set; }

    }

}
