using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// A request to create a new tag definition.
    /// </summary>
    /// <seealso cref="GetTagSchemaRequest"/>
    public class CreateTagRequest : AdapterRequest {

        /// <summary>
        /// The request body.
        /// </summary>
        /// <remarks>
        ///   The schema for the <see cref="Body"/> is obtained by sending a <see cref="GetTagSchemaRequest"/> 
        ///   to the adapter.
        /// </remarks>
        [Required]
        public JsonElement Body { get; set; }

    }
}
