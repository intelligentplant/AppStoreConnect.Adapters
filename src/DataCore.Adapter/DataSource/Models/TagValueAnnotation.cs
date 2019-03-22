using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DataCore.Adapter.DataSource.Models {

    /// <summary>
    /// Describes an annotation on a tag.
    /// </summary>
    public sealed class TagValueAnnotation {

        /// <summary>
        /// The unique identifier for the annotation.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The annotation type.
        /// </summary>
        public AnnotationType AnnotationType { get; private set; }

        /// <summary>
        /// The UTC start time for the annotation.
        /// </summary>
        public DateTime UtcStartTime { get; private set; } 

        /// <summary>
        /// The UTC end time for the annotation. If <see cref="AnnotationType"/> is 
        /// <see cref="AnnotationType.Instantaneous"/>, this property will always be 
        /// <see langword="null"/>.
        /// </summary>
        public DateTime? UtcEndTime { get; private set; }

        /// <summary>
        /// The annotation value.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// An additional description or explanation of the annotation.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Additional annotation properties.
        /// </summary>
        private readonly Dictionary<string, string> _properties;

        /// <summary>
        /// Additional annotation properties.
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties { get; private set; }


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
            _properties = properties == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(properties);
            Properties = new ReadOnlyDictionary<string, string>(_properties);
        }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotation"/> with a <see cref="UtcStartTime"/> set to 
        /// the current UTC time.
        /// </summary>
        public static TagValueAnnotation Create() {
            return new TagValueAnnotation();
        }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotation"/> that is a copy of an existing annotation.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="other"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueAnnotation CreateFromExisting(TagValueAnnotation other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            return new TagValueAnnotation(other.Id, other.AnnotationType, other.UtcStartTime, other.UtcEndTime, other.Value, other.Description, other._properties);
        }


        /// <summary>
        /// Updates the annotation ID.
        /// </summary>
        /// <param name="id">
        ///   The updated ID.
        /// </param>
        /// <returns>
        ///   The updated annotation.
        /// </returns>
        public TagValueAnnotation WithId(string id) {
            Id = id;
            return this;
        }


        /// <summary>
        /// Updates the annotation type.
        /// </summary>
        /// <param name="type">
        ///   The updated type.
        /// </param>
        /// <returns>
        ///   The updated annotation.
        /// </returns>
        public TagValueAnnotation WithType(AnnotationType type) {
            AnnotationType = type;
            if (AnnotationType == AnnotationType.Instantaneous) {
                UtcEndTime = null;
            }
            return this;
        }


        /// <summary>
        /// Updates the annotation start time.
        /// </summary>
        /// <param name="utcTime">
        ///   The updated UTC start time.
        /// </param>
        /// <returns>
        ///   The updated annotation.
        /// </returns>
        public TagValueAnnotation WithUtcStartTime(DateTime utcTime) {
            UtcStartTime = utcTime.ToUniversalTime();
            return this;
        }


        /// <summary>
        /// Updates the annotation end time.
        /// </summary>
        /// <param name="utcTime">
        ///   The updated UTC end time.
        /// </param>
        /// <returns>
        ///   The updated annotation.
        /// </returns>
        public TagValueAnnotation WithUtcEndTime(DateTime? utcTime) {
            if (AnnotationType == AnnotationType.TimeRange) {
                UtcEndTime = utcTime?.ToUniversalTime();
            }
            return this;
        }


        /// <summary>
        /// Updates the annotation value.
        /// </summary>
        /// <param name="value">
        ///   The updated value.
        /// </param>
        /// <returns>
        ///   The updated annotation.
        /// </returns>
        public TagValueAnnotation WithValue(string value) {
            Value = value;
            return this;
        }


        /// <summary>
        /// Updates the annotation description.
        /// </summary>
        /// <param name="description">
        ///   The updated description.
        /// </param>
        /// <returns>
        ///   The updated annotation.
        /// </returns>
        public TagValueAnnotation WithDescription(string description) {
            Description = description;
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
        ///   The updated annotation.
        /// </returns>
        public TagValueAnnotation WithProperty(string name, string value) {
            if (name != null) {
                _properties[name] = value;
                Properties = new ReadOnlyDictionary<string, string>(_properties);
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
        ///   The updated annotation.
        /// </returns>
        public TagValueAnnotation WithProperties(IDictionary<string, string> properties) {
            if (properties != null) {
                var dirty = false;
                foreach (var prop in properties) {
                    dirty = true;
                    _properties[prop.Key] = prop.Value;
                }

                if (dirty) {
                    Properties = new ReadOnlyDictionary<string, string>(_properties);
                }
            }
            return this;
        }

    }
}
