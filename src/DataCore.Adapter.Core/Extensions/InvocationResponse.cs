using System.Text.Json;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Describes a response to an <see cref="InvocationRequest"/>.
    /// </summary>
    public class InvocationResponse {

        /// <summary>
        /// The invocation results.
        /// </summary>
        public JsonElement? Results { get; set; }

    }
}
