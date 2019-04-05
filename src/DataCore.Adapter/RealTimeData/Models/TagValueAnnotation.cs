using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using DataCore.Adapter.RealTimeData.Utilities;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes an annotation on a tag. Use the <see cref="Create"/> or <see cref="CreateFromExisting(TagValueAnnotation)"/> 
    /// methods to build new annotations using a fluent interface.
    /// </summary>
    /// <seealso cref="TagValueAnnotationBuilder"/>
    public sealed class TagValueAnnotation {

        /// <summary>
        /// The unique identifier for the annotation.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The annotation type.
        /// </summary>
        public AnnotationType AnnotationType { get; }

        /// <summary>
        /// The UTC start time for the annotation.
        /// </summary>
        public DateTime UtcStartTime { get; } 

        /// <summary>
        /// The UTC end time for the annotation. If <see cref="AnnotationType"/> is 
        /// <see cref="AnnotationType.Instantaneous"/>, this property will always be 
        /// <see langword="null"/>.
        /// </summary>
        public DateTime? UtcEndTime { get; }

        /// <summary>
        /// The annotation value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// An additional description or explanation of the annotation.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Additional annotation properties.
        /// </summary>
        public IDictionary<string, string> Properties { get; }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotation"/> with a <see cref="UtcStartTime"/> set to 
        /// the current UTC time.
        /// </summary>
        private TagValueAnnotation() {
            UtcStartTime = DateTime.UtcNow;
        }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotation"/> object. The static <see cref="Create"/> and 
        /// <see cref="CreateFromExisting(TagValueAnnotation)"/> methods are also avalable, for easier 
        /// construction using a fluent interface.
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
        public TagValueAnnotation(string id, AnnotationType annotationType, DateTime utcStartTime, DateTime? utcEndTime, string value, string description, IDictionary<string, string> properties) : this() {
            Id = id;
            AnnotationType = annotationType;
            UtcStartTime = utcStartTime.ToUniversalTime();
            UtcEndTime = annotationType == AnnotationType.Instantaneous
                ? null
                : utcEndTime?.ToUniversalTime();
            Value = value;
            Description = description;
            Properties = new ReadOnlyDictionary<string, string>(properties ?? new Dictionary<string, string>());
        }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationBuilder"/> object.
        /// </summary>
        public static TagValueAnnotationBuilder Create() {
            return new TagValueAnnotationBuilder();
        }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationBuilder"/> that is configured using an existing 
        /// tag value annotation.
        /// </summary>
        /// <param name="other">
        ///   The tag value annotation to copy the initial values from.
        /// </param>
        /// <returns>
        ///   An <see cref="TagValueAnnotationBuilder"/> with pre-configured properties.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="other"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueAnnotationBuilder CreateFromExisting(TagValueAnnotation other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            return new TagValueAnnotationBuilder(other);
        }

    }
}
