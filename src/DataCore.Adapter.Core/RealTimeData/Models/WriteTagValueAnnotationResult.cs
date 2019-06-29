using System;
using System.Collections.Generic;
using System.Text;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes the result of a tag value annotation write operation.
    /// </summary>
    public class WriteTagValueAnnotationResult : WriteOperationResult {

        /// <summary>
        /// The ID of the tag that the annotation operation was performed on.
        /// </summary>
        public string TagId { get; }

        /// <summary>
        /// The annotation ID.
        /// </summary>
        public string AnnotationId { get; }


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
        public WriteTagValueAnnotationResult(string tagId, string annotationId, WriteStatus status, string notes, IDictionary<string, string> properties)
            : base(status, notes, properties) {
            TagId = tagId ?? throw new ArgumentNullException(nameof(tagId));
            AnnotationId = annotationId ?? throw new ArgumentNullException(nameof(annotationId));
        }

    }
}
