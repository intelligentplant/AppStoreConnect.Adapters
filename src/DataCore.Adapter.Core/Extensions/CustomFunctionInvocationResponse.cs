using System.Text.Json;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// A response to a custom function invocation.
    /// </summary>
    public class CustomFunctionInvocationResponse {

        /// <summary>
        /// The response payload.
        /// </summary>
        public JsonElement Body { get; set; }

    }

}
