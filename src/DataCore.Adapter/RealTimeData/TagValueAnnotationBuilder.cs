using System;
using System.Collections.Generic;
using System.Linq;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Helper class for constructing <see cref="TagValueAnnotationExtended"/> objects using a fluent interface.
    /// </summary>
    public class TagValueAnnotationBuilder {

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
        /// Additional annotation properties.
        /// </summary>
        private readonly List<AdapterProperty> _properties = new List<AdapterProperty>();


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
        public TagValueAnnotationBuilder(TagValueAnnotationExtended existing) {
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }

            _id = existing.Id;
            _annotationType = existing.AnnotationType;
            _utcStartTime = existing.UtcStartTime;
            _utcEndTime = existing.UtcEndTime;
            _value = existing.Value;
            _description = existing.Description;
            if (existing.Properties != null) {
                _properties.AddRange(existing.Properties.Where(x => x != null));
            }
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
        public TagValueAnnotationExtended Build() {
            return TagValueAnnotationExtended.Create(_id!, _annotationType, _utcStartTime, _utcEndTime, _value, _description, _properties);
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


        /// <summary>
        /// Adds a property to the annotation.
        /// </summary>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The property value.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueAnnotationBuilder"/>.
        /// </returns>
        public TagValueAnnotationBuilder WithProperty(string name, object value) {
            if (name != null) {
                _properties.Add(AdapterProperty.Create(name, value));
            }
            return this;
        }


        /// <summary>
        /// Adds a set of properties to the annotation.
        /// </summary>
        /// <param name="properties">
        ///   The properties.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueAnnotationBuilder"/>.
        /// </returns>
        public TagValueAnnotationBuilder WithProperties(params AdapterProperty[] properties) {
            return WithProperties((IEnumerable<AdapterProperty>) properties);
        }


        /// <summary>
        /// Adds a set of properties to the annotation.
        /// </summary>
        /// <param name="properties">
        ///   The properties.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueAnnotationBuilder"/>.
        /// </returns>
        public TagValueAnnotationBuilder WithProperties(IEnumerable<AdapterProperty> properties) {
            if (properties != null) {
                _properties.AddRange(properties.Where(x => x != null));
            }
            return this;
        }

    }
}
