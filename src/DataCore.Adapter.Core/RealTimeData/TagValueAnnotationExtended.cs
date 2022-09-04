using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes an annotation on a tag.
    /// </summary>
    [JsonConverter(typeof(TagValueAnnotationExtendedConverter))]
    public sealed class TagValueAnnotationExtended : TagValueAnnotation {

        /// <summary>
        /// The unique identifier for the annotation.
        /// </summary>
        [Required]
        public string Id { get; }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationExtended"/> object.
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
        public TagValueAnnotationExtended(
            string id, 
            AnnotationType annotationType, 
            DateTime utcStartTime, 
            DateTime? utcEndTime, 
            string? value, 
            string? description, 
            IEnumerable<AdapterProperty>? properties
        ) : base(annotationType, utcStartTime, utcEndTime, value, description, properties) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationExtended"/> object.
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
        public static TagValueAnnotationExtended Create(string id, AnnotationType annotationType, DateTime utcStartTime, DateTime? utcEndTime, string? value, string? description, IEnumerable<AdapterProperty>? properties) {
            return new TagValueAnnotationExtended(
                id,
                annotationType,
                utcStartTime,
                utcEndTime,
                value,
                description,
                properties
            );
        }

    }


    /// <summary>
    /// JSON converter for <see cref="TagValueAnnotationExtended"/>.
    /// </summary>
    internal class TagValueAnnotationExtendedConverter : AdapterJsonConverter<TagValueAnnotationExtended> {

        /// <inheritdoc/>
        public override TagValueAnnotationExtended Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null!;
            AnnotationType annotationType = AnnotationType.Instantaneous;
            DateTime utcStartTime = default;
            DateTime? utcEndTime = null;
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

                if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.AnnotationType), StringComparison.OrdinalIgnoreCase)) {
                    annotationType = JsonSerializer.Deserialize<AnnotationType>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.UtcStartTime), StringComparison.OrdinalIgnoreCase)) {
                    utcStartTime = JsonSerializer.Deserialize<DateTime>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.UtcEndTime), StringComparison.OrdinalIgnoreCase)) {
                    utcEndTime = JsonSerializer.Deserialize<DateTime?>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagValueAnnotationExtended.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return TagValueAnnotationExtended.Create(id, annotationType, utcStartTime, utcEndTime, value, description, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagValueAnnotationExtended value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.Id), value.Id, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.AnnotationType), value.AnnotationType, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.UtcStartTime), value.UtcStartTime, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.UtcEndTime), value.UtcEndTime, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.Value), value.Value, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.Description), value.Description, options);
            WritePropertyValue(writer, nameof(TagValueAnnotationExtended.Properties), value.Properties, options);
            writer.WriteEndObject();
        }

    }

}
