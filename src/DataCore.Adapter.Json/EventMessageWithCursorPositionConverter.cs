using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="EventMessageWithCursorPosition"/>.
    /// </summary>
    public class EventMessageWithCursorPositionConverter : AdapterJsonConverter<EventMessageWithCursorPosition> {


        /// <inheritdoc/>
        public override EventMessageWithCursorPosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null;
            DateTime utcEventTime = default;
            EventPriority priority = EventPriority.Unknown;
            string category = null;
            string message = null;
            AdapterProperty[] properties = null;
            string cursorPosition = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(EventMessageWithCursorPosition.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EventMessageWithCursorPosition.UtcEventTime), StringComparison.OrdinalIgnoreCase)) {
                    utcEventTime = JsonSerializer.Deserialize<DateTime>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EventMessageWithCursorPosition.Priority), StringComparison.OrdinalIgnoreCase)) {
                    priority = JsonSerializer.Deserialize<EventPriority>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EventMessageWithCursorPosition.Category), StringComparison.OrdinalIgnoreCase)) {
                    category = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EventMessageWithCursorPosition.Message), StringComparison.OrdinalIgnoreCase)) {
                    message = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EventMessageWithCursorPosition.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EventMessageWithCursorPosition.CursorPosition), StringComparison.OrdinalIgnoreCase)) {
                    cursorPosition = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return EventMessageWithCursorPosition.Create(id, utcEventTime, priority, category, message, properties, cursorPosition);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, EventMessageWithCursorPosition value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(EventMessageWithCursorPosition.Id), value.Id, options);
            WritePropertyValue(writer, nameof(EventMessageWithCursorPosition.UtcEventTime), value.UtcEventTime, options);
            WritePropertyValue(writer, nameof(EventMessageWithCursorPosition.Priority), value.Priority, options);
            WritePropertyValue(writer, nameof(EventMessageWithCursorPosition.Category), value.Category, options);
            WritePropertyValue(writer, nameof(EventMessageWithCursorPosition.Message), value.Message, options);
            WritePropertyValue(writer, nameof(EventMessageWithCursorPosition.Properties), value.Properties, options);
            WritePropertyValue(writer, nameof(EventMessageWithCursorPosition.CursorPosition), value.CursorPosition, options);
            writer.WriteEndObject();
        }

    }
}
