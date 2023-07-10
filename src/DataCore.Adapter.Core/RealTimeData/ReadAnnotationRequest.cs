using System.ComponentModel.DataAnnotations;
using DataCore.Adapter.Common;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request to get an annotation by ID.
    /// </summary>
    public class ReadAnnotationRequest : AdapterRequest {

        /// <summary>
        /// The tag ID or name for the annotation.
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
