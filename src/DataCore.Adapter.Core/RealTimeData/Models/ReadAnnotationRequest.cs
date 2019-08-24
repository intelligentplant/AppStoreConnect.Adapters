using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes a request to get an annotation by ID.
    /// </summary>
    public class ReadAnnotationRequest {

        /// <summary>
        /// The tag ID for the annotation.
        /// </summary>
        [Required]
        public string TagId { get; set; }

        /// <summary>
        /// The annotation ID.
        /// </summary>
        [Required]
        public string AnnotationId { get; set; }

    }
}
