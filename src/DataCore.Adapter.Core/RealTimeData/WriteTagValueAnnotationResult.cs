using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes the result of a tag value annotation write operation.
    /// </summary>
    [JsonConverter(typeof(WriteTagValueAnnotationResultConverter))]
    public class WriteTagValueAnnotationResult : WriteOperationResult {

        /// <summary>
        /// The ID of the tag that the annotation operation was performed on.
        /// </summary>
        [Required]
        public string TagId { get; }

        /// <summary>
        /// The annotation ID.
        /// </summary>
        [Required]
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
        public WriteTagValueAnnotationResult(
            string tagId, 
            string annotationId, 
            WriteStatus status, 
            string? notes, 
            IEnumerable<AdapterProperty>? properties
        ) : base(status, notes, properties) {
            TagId = tagId ?? throw new ArgumentNullException(nameof(tagId));
            AnnotationId = annotationId ?? throw new ArgumentNullException(nameof(annotationId));
        }


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
        public static WriteTagValueAnnotationResult Create(string tagId, string annotationId, WriteStatus status, string? notes, IEnumerable<AdapterProperty>? properties) {
            return new WriteTagValueAnnotationResult(tagId, annotationId, status, notes, properties);
        }

    }


    /// <summary>
    /// JSON converter for <see cref="WriteTagValueAnnotationResult"/>.
    /// </summary>
    internal class WriteTagValueAnnotationResultConverter : AdapterJsonConverter<WriteTagValueAnnotationResult> {

        /// <inheritdoc/>
        public override WriteTagValueAnnotationResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string tagId = null!;
            string annotationId = null!;
            WriteStatus status = WriteStatus.Unknown;
            string notes = null!;
            AdapterProperty[] properties = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(WriteTagValueAnnotationResult.TagId), StringComparison.OrdinalIgnoreCase)) {
                    tagId = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(WriteTagValueAnnotationResult.AnnotationId), StringComparison.OrdinalIgnoreCase)) {
                    annotationId = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(WriteTagValueAnnotationResult.Status), StringComparison.OrdinalIgnoreCase)) {
                    status = JsonSerializer.Deserialize<WriteStatus>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(WriteTagValueAnnotationResult.Notes), StringComparison.OrdinalIgnoreCase)) {
                    notes = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(WriteTagValueAnnotationResult.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return WriteTagValueAnnotationResult.Create(tagId, annotationId, status, notes, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, WriteTagValueAnnotationResult value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(WriteTagValueAnnotationResult.TagId), value.TagId, options);
            WritePropertyValue(writer, nameof(WriteTagValueAnnotationResult.AnnotationId), value.AnnotationId, options);
            WritePropertyValue(writer, nameof(WriteTagValueAnnotationResult.Status), value.Status, options);
            WritePropertyValue(writer, nameof(WriteTagValueAnnotationResult.Notes), value.Notes, options);
            WritePropertyValue(writer, nameof(WriteTagValueAnnotationResult.Properties), value.Properties, options);

            writer.WriteEndObject();
        }

    }

}
