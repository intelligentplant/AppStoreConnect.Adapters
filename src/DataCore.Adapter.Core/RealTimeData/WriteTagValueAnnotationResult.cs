using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

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
        public static WriteTagValueAnnotationResult Create(string tagId, string annotationId, WriteStatus status, string notes, IEnumerable<AdapterProperty> properties) {
            return new WriteTagValueAnnotationResult() {
                TagId = tagId ?? throw new ArgumentNullException(nameof(tagId)),
                AnnotationId = annotationId ?? throw new ArgumentNullException(nameof(annotationId)),
                Status = status,
                Notes = notes,
                Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>()
            };
        }

    }
}
