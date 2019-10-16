using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes the base set of properties for a tag value annotation.
    /// </summary>
    public class TagValueAnnotationBase {

        /// <summary>
        /// The annotation type.
        /// </summary>
        public AnnotationType AnnotationType { get; set; }

        /// <summary>
        /// The UTC start time for the annotation.
        /// </summary>
        public DateTime UtcStartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The UTC end time for the annotation. If <see cref="AnnotationType"/> is 
        /// <see cref="AnnotationType.Instantaneous"/>, this property will always be 
        /// <see langword="null"/>.
        /// </summary>
        public DateTime? UtcEndTime { get; set; }

        /// <summary>
        /// The annotation value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// An additional description or explanation of the annotation.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Additional annotation properties.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; set; }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationBase"/> object.
        /// </summary>
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
        public static TagValueAnnotationBase Create(AnnotationType annotationType, DateTime utcStartTime, DateTime? utcEndTime, string value, string description, IEnumerable<AdapterProperty> properties) {
            return new TagValueAnnotationBase() {
                AnnotationType = annotationType,
                UtcStartTime = utcStartTime.ToUniversalTime(),
                UtcEndTime = annotationType == AnnotationType.Instantaneous
                ? null
                : utcEndTime?.ToUniversalTime(),
                Value = value,
                Description = description,
                Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>()
            };
        }

    }
}
