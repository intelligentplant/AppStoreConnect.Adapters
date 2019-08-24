using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes a request to update an existing tag value annotation.
    /// </summary>
    public class UpdateAnnotationRequest {

        /// <summary>
        /// The tag ID.
        /// </summary>
        [Required]
        public string TagId { get; set; }

        /// <summary>
        /// The annotation ID.
        /// </summary>
        [Required]
        public string AnnotationId { get; set; }

        /// <summary>
        /// The updated annotation settings.
        /// </summary>
        [Required]
        public TagValueAnnotationBase Annotation { get; set; }

    }
}
