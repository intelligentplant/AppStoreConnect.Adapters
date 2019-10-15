using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request to create a new tag value annotation.
    /// </summary>
    public class CreateAnnotationRequest {

        /// <summary>
        /// The ID of the tag that the annotation is associated with.
        /// </summary>
        [Required]
        public string TagId { get; set; }

        /// <summary>
        /// The annotation.
        /// </summary>
        [Required]
        public TagValueAnnotationBase Annotation { get; set; }

    }
}
