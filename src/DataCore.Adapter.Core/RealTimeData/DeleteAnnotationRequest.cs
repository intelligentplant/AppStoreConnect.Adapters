using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request to delete a tag value annotation.
    /// </summary>
    public class DeleteAnnotationRequest : AdapterRequest {

        /// <summary>
        /// The tag ID or name.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Tag { get; set; } = default!;

        /// <summary>
        /// The annotation ID.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string AnnotationId { get; set; } = default!;

    }
}
