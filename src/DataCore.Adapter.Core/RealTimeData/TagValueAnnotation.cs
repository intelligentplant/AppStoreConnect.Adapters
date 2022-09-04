using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes the base set of properties for a tag value annotation.
    /// </summary>
    [JsonConverter(typeof(TagValueAnnotationConverter))]
    public class TagValueAnnotation {

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
        public string? Value { get; }

        /// <summary>
        /// An additional description or explanation of the annotation.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Additional annotation properties.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotation"/> object.
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
        public TagValueAnnotation(AnnotationType annotationType, DateTime utcStartTime, DateTime? utcEndTime, string? value, string? description, IEnumerable<AdapterProperty>? properties) {
            AnnotationType = annotationType;
            UtcStartTime = utcStartTime.ToUniversalTime();
            UtcEndTime = annotationType == AnnotationType.Instantaneous
                ? null
                : utcEndTime?.ToUniversalTime();
            Value = value;
            Description = description;
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotation"/> object.
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
        public static TagValueAnnotation Create(AnnotationType annotationType, DateTime utcStartTime, DateTime? utcEndTime, string? value, string? description, IEnumerable<AdapterProperty>? properties) {
            return new TagValueAnnotation(annotationType, utcStartTime, utcEndTime, value, description, properties);
        }

    }


    /// <summary>
    /// JSON converter for <see cref="TagValueAnnotation"/>.
    /// </summary>
    internal class TagValueAnnotationConverter : AdapterJsonConverter<TagValueAnnotation> {

        /// <inheritdoc/>
        public override TagValueAnnotation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            AnnotationType annotationType = AnnotationType.Instantaneous;
            DateTime utcStartTime = default;
            DateTime? utcEndTime = null!;
            string value = null!;
            string description = null!;
            AdapterProperty[] properties = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagValueAnnotation.AnnotationType), StringComparison.OrdinalIgnoreCase)) {
                    annotationType = JsonSerializer.Deserialize<AnnotationType>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotation.UtcStartTime), StringComparison.OrdinalIgnoreCase)) {
                    utcStartTime = JsonSerializer.Deserialize<DateTime>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotation.UtcEndTime), StringComparison.OrdinalIgnoreCase)) {
                    utcEndTime = JsonSerializer.Deserialize<DateTime?>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotation.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotation.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotation.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return TagValueAnnotation.Create(annotationType, utcStartTime, utcEndTime, value, description, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagValueAnnotation value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagValueAnnotation.AnnotationType), value.AnnotationType, options);
            WritePropertyValue(writer, nameof(TagValueAnnotation.UtcStartTime), value.UtcStartTime, options);
            WritePropertyValue(writer, nameof(TagValueAnnotation.UtcEndTime), value.UtcEndTime, options);
            WritePropertyValue(writer, nameof(TagValueAnnotation.Value), value.Value, options);
            WritePropertyValue(writer, nameof(TagValueAnnotation.Description), value.Description, options);
            WritePropertyValue(writer, nameof(TagValueAnnotation.Properties), value.Properties, options);
            writer.WriteEndObject();
        }

    }

}
