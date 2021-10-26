using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="WriteEventMessageResult"/>.
    /// </summary>
    public class WriteEventMessageResultConverter : AdapterJsonConverter<WriteEventMessageResult> {

        /// <inheritdoc/>
        public override WriteEventMessageResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string correlationId = null!;
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

                if (string.Equals(propertyName, nameof(WriteEventMessageResult.CorrelationId), StringComparison.OrdinalIgnoreCase)) {
                    correlationId = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(WriteEventMessageResult.StatusCode), StringComparison.OrdinalIgnoreCase)) {
                    status = JsonSerializer.Deserialize<StatusCode>(ref reader, options);
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
            WritePropertyValue(writer, nameof(WriteEventMessageResult.StatusCode), value.StatusCode, options);
            WritePropertyValue(writer, nameof(WriteEventMessageResult.Notes), value.Notes, options);
            WritePropertyValue(writer, nameof(WriteEventMessageResult.Properties), value.Properties, options);

            writer.WriteEndObject();
        }

    }
}
