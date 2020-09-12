using System;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// JSON converter for <see cref="EventMessage"/>.
    /// </summary>
    public class EventMessageConverter : AdapterJsonConverter<EventMessage> {


        /// <inheritdoc/>
        public override EventMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null!;
            string topic = null!;
            DateTime utcEventTime = default;
            EventPriority priority = EventPriority.Unknown;
            string category = null!;
            string message = null!;
            AdapterProperty[] properties = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(EventMessage.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EventMessage.Topic), StringComparison.OrdinalIgnoreCase)) {
                    topic = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EventMessage.UtcEventTime), StringComparison.OrdinalIgnoreCase)) {
                    utcEventTime = JsonSerializer.Deserialize<DateTime>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EventMessage.Priority), StringComparison.OrdinalIgnoreCase)) {
                    priority = JsonSerializer.Deserialize<EventPriority>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EventMessage.Category), StringComparison.OrdinalIgnoreCase)) {
                    category = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EventMessage.Message), StringComparison.OrdinalIgnoreCase)) {
                    message = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(EventMessage.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return EventMessage.Create(id, topic, utcEventTime, priority, category, message, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, EventMessage value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(EventMessage.Id), value.Id, options);
            WritePropertyValue(writer, nameof(EventMessage.Topic), value.Topic, options);
            WritePropertyValue(writer, nameof(EventMessage.UtcEventTime), value.UtcEventTime, options);
            WritePropertyValue(writer, nameof(EventMessage.Priority), value.Priority, options);
            WritePropertyValue(writer, nameof(EventMessage.Category), value.Category, options);
            WritePropertyValue(writer, nameof(EventMessage.Message), value.Message, options);
            WritePropertyValue(writer, nameof(EventMessage.Properties), value.Properties, options);
            writer.WriteEndObject();
        }

    }
}
