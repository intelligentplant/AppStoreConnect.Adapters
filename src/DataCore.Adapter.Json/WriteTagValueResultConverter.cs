using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {
    /// <summary>
    /// JSON converter for <see cref="WriteTagValueResult"/>.
    /// </summary>
    public class WriteTagValueResultConverter : AdapterJsonConverter<WriteTagValueResult> {

        /// <inheritdoc/>
        public override WriteTagValueResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string correlationId = null!;
            string tagId = null!;
            StatusCode status = StatusCodes.Uncertain;
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
                    status = JsonSerializer.Deserialize<StatusCode>(ref reader, options);
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
