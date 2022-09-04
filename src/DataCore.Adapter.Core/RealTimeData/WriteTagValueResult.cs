using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes the result of a tag value write operation.
    /// </summary>
    [JsonConverter(typeof(WriteTagValueResultConverter))]
    public sealed class WriteTagValueResult : WriteOperationResult {

        /// <summary>
        /// The optional correlation ID for the operation.
        /// </summary>
        public string? CorrelationId { get; }

        /// <summary>
        /// The ID of the tag.
        /// </summary>
        [Required]
        public string TagId { get; }


        /// <summary>
        /// Creates a new <see cref="WriteTagValueResult"/> object.
        /// </summary>
        /// <param name="correlationId">
        ///   The optional correlation ID for the operation. Can be <see langword="null"/>.
        /// </param>
        /// <param name="tagId">
        ///   The ID of the tag that was written to.
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
        public WriteTagValueResult(
            string? correlationId, 
            string tagId, 
            WriteStatus status, 
            string? notes, 
            IEnumerable<AdapterProperty>? properties
        ) : base(status, notes, properties) {
            CorrelationId = correlationId;
            TagId = tagId ?? throw new ArgumentNullException(nameof(tagId));
        }


        /// <summary>
        /// Creates a new <see cref="WriteTagValueResult"/> object.
        /// </summary>
        /// <param name="correlationId">
        ///   The optional correlation ID for the operation. Can be <see langword="null"/>.
        /// </param>
        /// <param name="tagId">
        ///   The ID of the tag that was written to.
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
        public static WriteTagValueResult Create(string? correlationId, string tagId, WriteStatus status, string? notes, IEnumerable<AdapterProperty>? properties) {
            return new WriteTagValueResult(correlationId, tagId, status, notes, properties);
        }

    }


    /// <summary>
    /// JSON converter for <see cref="WriteTagValueResult"/>.
    /// </summary>
    internal class WriteTagValueResultConverter : AdapterJsonConverter<WriteTagValueResult> {

        /// <inheritdoc/>
        public override WriteTagValueResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string correlationId = null!;
            string tagId = null!;
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

                if (string.Equals(propertyName, nameof(WriteTagValueResult.CorrelationId), StringComparison.OrdinalIgnoreCase)) {
                    correlationId = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(WriteTagValueResult.TagId), StringComparison.OrdinalIgnoreCase)) {
                    tagId = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(WriteTagValueResult.Status), StringComparison.OrdinalIgnoreCase)) {
                    status = JsonSerializer.Deserialize<WriteStatus>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(WriteTagValueResult.Notes), StringComparison.OrdinalIgnoreCase)) {
                    notes = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(WriteTagValueResult.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return WriteTagValueResult.Create(correlationId, tagId, status, notes, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, WriteTagValueResult value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(WriteTagValueResult.CorrelationId), value.CorrelationId, options);
            WritePropertyValue(writer, nameof(WriteTagValueResult.TagId), value.TagId, options);
            WritePropertyValue(writer, nameof(WriteTagValueResult.Status), value.Status, options);
            WritePropertyValue(writer, nameof(WriteTagValueResult.Notes), value.Notes, options);
            WritePropertyValue(writer, nameof(WriteTagValueResult.Properties), value.Properties, options);

            writer.WriteEndObject();
        }

    }

}
