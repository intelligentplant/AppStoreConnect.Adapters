using System;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Helper class for constructing <see cref="TagValueAnnotationExtended"/> objects using a fluent interface.
    /// </summary>
    public sealed class TagValueAnnotationBuilder : AdapterEntityBuilder<TagValueAnnotationExtended> {

        /// <summary>
        /// The annotation ID.
        /// </summary>
        private string? _id;

        /// <summary>
        /// The annotation type.
        /// </summary>
        private AnnotationType _annotationType;

        /// <summary>
        /// The annotation start time.
        /// </summary>
        private DateTime _utcStartTime;

        /// <summary>
        /// The annotation end time. Ignored if <see cref="_annotationType"/> is 
        /// <see cref="AnnotationType.Instantaneous"/>.
        /// </summary>
        private DateTime? _utcEndTime;

        /// <summary>
        /// The annotation value.
        /// </summary>
        private string? _value;

        /// <summary>
        /// The annotation description.
        /// </summary>
        private string? _description;


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationBuilder"/> object.
        /// </summary>
        public TagValueAnnotationBuilder() { }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationBuilder"/> object that is initialised using an 
        /// existing tag value annotation.
        /// </summary>
        /// <param name="existing">
        ///   The existing annotation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="existing"/> is <see langword="null"/>.
        /// </exception>
        public TagValueAnnotationBuilder(TagValueAnnotationExtended existing) : this((TagValueAnnotation) existing) {
            WithId(existing.Id);
        }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationBuilder"/> object that is initialised using an 
        /// existing tag value annotation.
        /// </summary>
        /// <param name="existing">
        ///   The existing annotation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="existing"/> is <see langword="null"/>.
        /// </exception>
        public TagValueAnnotationBuilder(TagValueAnnotation existing) {
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }

            WithType(existing.AnnotationType);
            WithUtcStartTime(existing.UtcStartTime);
            WithUtcEndTime(existing.UtcEndTime);
            WithValue(existing.Value);
            WithDescription(existing.Description);
            this.WithProperties(existing.Properties);
        }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationBuilder"/> object.
        /// </summary>
        [Obsolete("This method will be removed in a future release. Use TagValueAnnotationBuilder() instead.", false)]
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
        [Obsolete("This method will be removed in a future release. Use TagValueAnnotationBuilder(TagValueAnnotationExtended) instead.", false)]
        public static TagValueAnnotationBuilder CreateFromExisting(TagValueAnnotationExtended other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            return new TagValueAnnotationBuilder(other);
        }


        /// <summary>
        /// Creates a <see cref="TagValueAnnotationExtended"/> using the configured settings.
        /// </summary>
        /// <returns>
        ///   A new <see cref="TagValueAnnotationExtended"/> object.
        /// </returns>
        public override TagValueAnnotationExtended Build() {
            return TagValueAnnotationExtended.Create(_id!, _annotationType, _utcStartTime, _utcEndTime, _value, _description, GetProperties());
        }


        /// <summary>
        /// Updates the annotation ID.
        /// </summary>
        /// <param name="id">
        ///   The updated ID.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueAnnotationBuilder"/>.
        /// </returns>
        public TagValueAnnotationBuilder WithId(string id) {
            _id = id;
            return this;
        }


        /// <summary>
        /// Updates the annotation type.
        /// </summary>
        /// <param name="type">
        ///   The updated type.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueAnnotationBuilder"/>.
        /// </returns>
        public TagValueAnnotationBuilder WithType(AnnotationType type) {
            _annotationType = type;
            return this;
        }


        /// <summary>
        /// Updates the annotation start time.
        /// </summary>
        /// <param name="utcTime">
        ///   The updated UTC start time.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueAnnotationBuilder"/>.
        /// </returns>
        public TagValueAnnotationBuilder WithUtcStartTime(DateTime utcTime) {
            _utcStartTime = utcTime.ToUniversalTime();
            return this;
        }


        /// <summary>
        /// Updates the annotation end time.
        /// </summary>
        /// <param name="utcTime">
        ///   The updated UTC end time.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueAnnotationBuilder"/>.
        /// </returns>
        public TagValueAnnotationBuilder WithUtcEndTime(DateTime? utcTime) {
            _utcEndTime = utcTime?.ToUniversalTime();
            return this;
        }


        /// <summary>
        /// Updates the annotation value.
        /// </summary>
        /// <param name="value">
        ///   The updated value.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueAnnotationBuilder"/>.
        /// </returns>
        public TagValueAnnotationBuilder WithValue(string? value) {
            _value = value;
            return this;
        }


        /// <summary>
        /// Updates the annotation description.
        /// </summary>
        /// <param name="description">
        ///   The updated description.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueAnnotationBuilder"/>.
        /// </returns>
        public TagValueAnnotationBuilder WithDescription(string? description) {
            _description = description;
            return this;
        }

    }
}
