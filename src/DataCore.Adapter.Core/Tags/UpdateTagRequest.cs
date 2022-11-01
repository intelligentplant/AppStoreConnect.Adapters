using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// A request to update an existing tag definition.
    /// </summary>
    /// <seealso cref="GetTagSchemaRequest"/>
    public class UpdateTagRequest : AdapterRequest {

        /// <summary>
        /// The name or ID of the tag to modify.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Tag { get; set; } = default!;

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
