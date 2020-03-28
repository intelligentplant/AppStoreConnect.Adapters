using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request to update an existing tag value annotation.
    /// </summary>
    public class UpdateAnnotationRequest {

        /// <summary>
        /// The tag name or ID.
        /// </summary>
        [Required]
        public string Tag { get; set; }

        /// <summary>
        /// The annotation ID.
        /// </summary>
        [Required]
        public string AnnotationId { get; set; }

        /// <summary>
        /// The updated annotation settings.
        /// </summary>
        [Required]
        public TagValueAnnotation Annotation { get; set; }

    }
}
