using System.Text.Json;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Describes a response to an <see cref="InvocationRequest"/>.
    /// </summary>
    public class InvocationResponse {

        /// <summary>
        /// The status code associated with the response.
        /// </summary>
        public StatusCode Status { get; set; }

        /// <summary>
        /// The invocation results.
        /// </summary>
        public JsonElement? Results { get; set; }

    }
}
