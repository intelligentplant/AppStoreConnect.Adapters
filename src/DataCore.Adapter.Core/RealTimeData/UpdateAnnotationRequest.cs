using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request to update an existing tag value annotation.
    /// </summary>
    public class UpdateAnnotationRequest : AdapterRequest {

        /// <summary>
        /// The tag name or ID.
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

        /// <summary>
        /// The updated annotation settings.
        /// </summary>
        [Required]
        public TagValueAnnotation Annotation { get; set; } = default!;

    }
}
