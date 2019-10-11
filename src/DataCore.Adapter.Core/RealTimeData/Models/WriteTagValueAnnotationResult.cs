using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes the result of a tag value annotation write operation.
    /// </summary>
    public class WriteTagValueAnnotationResult : WriteOperationResult {

        /// <summary>
        /// The ID of the tag that the annotation operation was performed on.
        /// </summary>
        [Required]
        public string TagId { get; set; }

        /// <summary>
        /// The annotation ID.
        /// </summary>
        [Required]
        public string AnnotationId { get; set; }


        /// <summary>
        /// Creates a new <see cref="WriteTagValueAnnotationResult"/> object.
        /// </summary>
        /// <param name="tagId">
        ///   The ID of the tag that the annotation was written to.
        /// </param>
        /// <param name="annotationId">
        ///   The ID of the annotation that was written.
        /// </param>
        /// <param name="status">
        ///   A flag indicating if the write was successful.
        /// </param>
        /// <param name="notes">
        ///   Notes associated with the write.
        /// </param>
        /// <param name="properties">
        ///   Additional properties related to the write.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        public static WriteTagValueAnnotationResult Create(string tagId, string annotationId, WriteStatus status, string notes, IDictionary<string, string> properties) {
            return new WriteTagValueAnnotationResult() {
                TagId = tagId ?? throw new ArgumentNullException(nameof(tagId)),
                AnnotationId = annotationId ?? throw new ArgumentNullException(nameof(annotationId)),
                Status = status,
                Notes = notes,
                Properties = properties ?? new Dictionary<string, string>()
            };
        }

    }
}
