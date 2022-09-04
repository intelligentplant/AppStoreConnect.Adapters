using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Describes the result of an event message write operation.
    /// </summary>
    [JsonConverter(typeof(WriteEventMessageResultConverter))]
    public sealed class WriteEventMessageResult : WriteOperationResult {

        /// <summary>
        /// The optional correlation ID for the operation.
        /// </summary>
        public string? CorrelationId { get; }


        /// <summary>
        /// Creates a new <see cref="WriteEventMessageResult"/> object.
        /// </summary>
        /// <param name="correlationId">
        ///   The optional correlation ID for the operation.
        /// </param>
        /// <param name="status">
        ///   A flag indicating if the write was successful.
        /// </param>
        /// <param name="notes">
        ///   Notes associated with the write.
        /// </param>
        /// <param name="properties">
        ///   Additional properties related to the write.
        /// </param>,
        /// 
        public WriteEventMessageResult(
            string? correlationId,
            WriteStatus status, 
            string? notes, 
            IEnumerable<AdapterProperty>? properties
        ) : base(status, notes, properties) {
            CorrelationId = correlationId;
        }


        /// <summary>
        /// Creates a new <see cref="WriteEventMessageResult"/> object.
        /// </summary>
        /// <param name="correlationId">
        ///   The optional correlation ID for the operation.
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
        public static WriteEventMessageResult Create(string? correlationId, WriteStatus status, string? notes, IEnumerable<AdapterProperty>? properties)  {
            return new WriteEventMessageResult(correlationId, status, notes, properties);
        }

    }


    /// <summary>
    /// JSON converter for <see cref="WriteEventMessageResult"/>.
    /// </summary>
    internal class WriteEventMessageResultConverter : AdapterJsonConverter<WriteEventMessageResult> {

        /// <inheritdoc/>
        public override WriteEventMessageResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string correlationId = null!;
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

                if (string.Equals(propertyName, nameof(WriteEventMessageResult.CorrelationId), StringComparison.OrdinalIgnoreCase)) {
                    correlationId = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(WriteEventMessageResult.Status), StringComparison.OrdinalIgnoreCase)) {
                    status = JsonSerializer.Deserialize<WriteStatus>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(WriteEventMessageResult.Notes), StringComparison.OrdinalIgnoreCase)) {
                    notes = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(WriteEventMessageResult.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return WriteEventMessageResult.Create(correlationId, status, notes, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, WriteEventMessageResult value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(WriteEventMessageResult.CorrelationId), value.CorrelationId, options);
            WritePropertyValue(writer, nameof(WriteEventMessageResult.Status), value.Status, options);
            WritePropertyValue(writer, nameof(WriteEventMessageResult.Notes), value.Notes, options);
            WritePropertyValue(writer, nameof(WriteEventMessageResult.Properties), value.Properties, options);

            writer.WriteEndObject();
        }

    }

}
