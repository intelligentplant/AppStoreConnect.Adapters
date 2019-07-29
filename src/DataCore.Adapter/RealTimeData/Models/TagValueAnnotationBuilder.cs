using System;
using System.Collections.Generic;
using System.Text;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Helper class for constructing <see cref="TagValueAnnotation"/> objects using a fluent interface.
    /// </summary>
    public class TagValueAnnotationBuilder {

        /// <summary>
        /// The annotation ID.
        /// </summary>
        private string _id;

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
        private string _value;

        /// <summary>
        /// The annotation description.
        /// </summary>
        private string _description;

        /// <summary>
        /// Additional annotation properties.
        /// </summary>
        private readonly Dictionary<string, string> _properties;


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationBuilder"/> object.
        /// </summary>
        private TagValueAnnotationBuilder() {
            _properties = new Dictionary<string, string>();
        }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationBuilder"/> object that is initialised using an 
        /// existing tag value annotation.
        /// </summary>
        /// <param name="existing">
        ///   The existing annotation.
        /// </param>
        private TagValueAnnotationBuilder(TagValueAnnotation existing) {
            if (existing == null) {
                _properties = new Dictionary<string, string>();
                return;
            }

            _id = existing.Id;
            _annotationType = existing.AnnotationType;
            _utcStartTime = existing.UtcStartTime;
            _utcEndTime = existing.UtcEndTime;
            _value = existing.Value;
            _description = existing.Description;
            _properties = new Dictionary<string, string>(existing.Properties);
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


        /// <summary>
        /// Creates a <see cref="TagValueAnnotation"/> using the configured settings.
        /// </summary>
        /// <returns>
        ///   A new <see cref="TagValueAnnotation"/> object.
        /// </returns>
        public TagValueAnnotation Build() {
            return new TagValueAnnotation(_id, _annotationType, _utcStartTime, _utcEndTime, _value, _description, _properties);
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
        public TagValueAnnotationBuilder WithValue(string value) {
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
        public TagValueAnnotationBuilder WithDescription(string description) {
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
        public TagValueAnnotationBuilder WithProperty(string name, string value) {
            if (name != null) {
                _properties[name] = value;
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
        public TagValueAnnotationBuilder WithProperties(IDictionary<string, string> properties) {
            if (properties != null) {
                foreach (var prop in properties) {
                    _properties[prop.Key] = prop.Value;
                }
            }
            return this;
        }

    }
}
