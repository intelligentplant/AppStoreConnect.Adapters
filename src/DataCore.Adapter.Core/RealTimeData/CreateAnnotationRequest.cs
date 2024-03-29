﻿using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request to create a new tag value annotation.
    /// </summary>
    public class CreateAnnotationRequest : AdapterRequest {

        /// <summary>
        /// The ID or name of the tag that the annotation is associated with.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Tag { get; set; } = default!;

        /// <summary>
        /// The annotation.
        /// </summary>
        [Required]
        public TagValueAnnotation Annotation { get; set; } = default!;

    }
}
