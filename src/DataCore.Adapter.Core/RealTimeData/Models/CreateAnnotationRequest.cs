using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

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
