using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes an annotation on a tag.
    /// </summary>
    public sealed class TagValueAnnotation : TagValueAnnotationBase {

        /// <summary>
        /// The unique identifier for the annotation.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Creates a new <see cref="TagValueAnnotation"/> object.
        /// </summary>
        /// <param name="id">
        ///   The annotation ID.
        /// </param>
        /// <param name="annotationType">
        ///   The annotation type.
        /// </param>
        /// <param name="utcStartTime">
        ///   The UTC start time for the annotation.
        /// </param>
        /// <param name="utcEndTime">
        ///   The UTC end time for the annotation. Ignored when <paramref name="annotationType"/> is 
        ///   <see cref="AnnotationType.Instantaneous"/>.
        /// </param>
        /// <param name="value">
        ///   The annotation value.
        /// </param>
        /// <param name="description">
        ///   An additional description or explanation of the annotation.
        /// </param>
        /// <param name="properties">
        ///   Additional annotation properties.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        public TagValueAnnotation(string id, AnnotationType annotationType, DateTime utcStartTime, DateTime? utcEndTime, string value, string description, IDictionary<string, string> properties) 
            : base(annotationType, utcStartTime, utcEndTime, value, description, properties) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

    }
}
