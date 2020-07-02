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
        public string Tag { get; set; }

        /// <summary>
        /// The annotation ID.
        /// </summary>
        [Required]
        public string AnnotationId { get; set; }

    }
}
