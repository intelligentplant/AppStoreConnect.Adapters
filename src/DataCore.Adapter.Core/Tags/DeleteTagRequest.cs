using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// A request to delete tag definition.
    /// </summary>
    public class DeleteTagRequest : AdapterRequest {

        /// <summary>
        /// The name or ID of the tag to delete.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Tag { get; set; } = default!;

    }
}
