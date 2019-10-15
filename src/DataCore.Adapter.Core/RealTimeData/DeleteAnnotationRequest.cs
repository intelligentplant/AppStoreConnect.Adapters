using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request to delete a tag value annotation.
    /// </summary>
    public class DeleteAnnotationRequest {

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

    }
}
