using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Input arguments for a duplex streaming extension feature operation. 
    /// </summary>
    public class InvocationStreamItem {

        /// <summary>
        /// The invocation arguments.
        /// </summary>
        public JsonElement? Arguments { get; set; }

    }

}
