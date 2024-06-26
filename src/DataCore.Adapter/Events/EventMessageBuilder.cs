using System;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Helper class for constructing <see cref="EventMessage"/> objects using a fluent interface.
    /// </summary>
    public sealed class EventMessageBuilder : AdapterEntityBuilder<EventMessage> {

        /// <summary>
        /// The event ID.
        /// </summary>
        private string _id = Guid.NewGuid().ToString();

        /// <summary>
        /// The event message topic (e.g. the MQTT channel that emitted the message).
        /// </summary>
        private string? _topic;

        /// <summary>
        /// The UTC event timestamp.
        /// </summary>
        private DateTime _utcEventTime = DateTime.UtcNow;

        /// <summary>
        /// The event priority.
        /// </summary>
        private EventPriority _priority = EventPriority.Unknown;

        /// <summary>
        /// The event category.
        /// </summary>
        private string? _category;

        /// <summary>
        /// The event type.
        /// </summary>
        private string? _type;

        /// <summary>
        /// The event message.
        /// </summary>
        private string? _message;


        /// <summary>
        /// Creates a new <see cref="EventMessageBuilder"/> object.
        /// </summary>
        public EventMessageBuilder() { }


        /// <summary>
        /// Creates a new <see cref="EventMessageBuilder"/> object that is initialised using an existing 
        /// event message.
        /// </summary>
        /// <param name="existing">
        ///   The existing value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="existing"/> is <see langword="null"/>.
        /// </exception>
        public EventMessageBuilder(EventMessage existing) {
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }

            WithId(existing.Id);
            WithTopic(existing.Topic);
            WithUtcEventTime(existing.UtcEventTime);
            WithPriority(existing.Priority);
            WithCategory(existing.Category);
            WithType(existing.Type);
            WithMessage(existing.Message);
            this.WithProperties(existing.Properties);
        }


        /// <summary>
        /// Creates a new <see cref="EventMessageBuilder"/> object that can be used to create an 
        /// <see cref="EventMessage"/> using a fluent configuration interface.
        /// </summary>
        /// <returns>
        ///   A new <see cref="EventMessageBuilder"/> object.
        /// </returns>
        [Obsolete("This method will be removed in a future release. Use EventMessageBuilder() instead.", false)]
        public static EventMessageBuilder Create() {
            return new EventMessageBuilder();
        }


        /// <summary>
        /// Creates a new <see cref="EventMessageBuilder"/> that is configured using an existing 
        /// event message.
        /// </summary>
        /// <param name="other">
        ///   The event message to copy values from.
        /// </param>
        /// <returns>
        ///   An <see cref="EventMessageBuilder"/> with pre-configured event properties.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="other"/> is <see langword="null"/>.
        /// </exception>
        [Obsolete("This method will be removed in a future release. Use EventMessageBuilder(EventMessage) instead.", false)]
        public static EventMessageBuilder CreateFromExisting(EventMessage other) {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            return new EventMessageBuilder(other);
        }


        /// <summary>
        /// Creates a <see cref="EventMessage"/> using the configured settings.
        /// </summary>
        /// <returns>
        ///   A new <see cref="EventMessage"/> object.
        /// </returns>
        public override EventMessage Build() {
            return new EventMessage(_id, _topic, _utcEventTime, _priority, _category, _type, _message, GetProperties());
        }


        /// <summary>
        /// Creates a new <see cref="EventMessageWithCursorPosition"/> using the configured 
        /// settings and the specified cursor position.
        /// </summary>
        /// <param name="cursorPosition">
        ///   The cursor position.
        /// </param>
        /// <returns>
        ///   A new <see cref="EventMessageWithCursorPosition"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="cursorPosition"/> is <see langword="null"/>.
        /// </exception>
        public EventMessageWithCursorPosition Build(string cursorPosition) {
            return new EventMessageWithCursorPosition(
                _id, 
                _topic,
                _utcEventTime, 
                _priority, 
                _category, 
                _type,
                _message, 
                GetProperties(), 
                cursorPosition ?? throw new ArgumentNullException(nameof(cursorPosition))
            );
        }


        /// <summary>
        /// Updates the unique identifier for the event message.
        /// </summary>
        /// <param name="id">
        ///   The event identifier.
        /// </param>
        /// <returns>
        ///   The updated <see cref="EventMessageBuilder"/>.
        /// </returns>
        /// <remarks>
        ///   If <paramref name="id"/> is <see langword="null"/>, a new unique identifier will be 
        ///   generated using <see cref="Guid.NewGuid"/>.
        /// </remarks>
        public EventMessageBuilder WithId(string id) {
            _id = id ?? Guid.NewGuid().ToString();
            return this;
        }


        /// <summary>
        /// Updates the topic (i.e. the source channel) for the event.
        /// </summary>
        /// <param name="topic">
        ///   The topic name.
        /// </param>
        /// <returns>
        ///   The updated <see cref="EventMessageBuilder"/>.
        /// </returns>
        public EventMessageBuilder WithTopic(string? topic) {
            _topic = topic;
            return this;
        }


        /// <summary>
        /// Updates the UTC timestamp for the event.
        /// </summary>
        /// <param name="utcEventTime">
        ///   The UTC timestamp.
        /// </param>
        /// <returns>
        ///   The updated <see cref="EventMessageBuilder"/>.
        /// </returns>
        public EventMessageBuilder WithUtcEventTime(DateTime utcEventTime) {
            _utcEventTime = utcEventTime.ToUniversalTime();
            return this;
        }


        /// <summary>
        /// Updates the event priority.
        /// </summary>
        /// <param name="priority">
        ///   The priority.
        /// </param>
        /// <returns>
        ///   The updated <see cref="EventMessageBuilder"/>.
        /// </returns>
        public EventMessageBuilder WithPriority(EventPriority priority) {
            _priority = priority;
            return this;
        }


        /// <summary>
        /// Updates the event category.
        /// </summary>
        /// <param name="category">
        ///   The category.
        /// </param>
        /// <returns>
        ///   The updated <see cref="EventMessageBuilder"/>.
        /// </returns>
        public EventMessageBuilder WithCategory(string? category) {
            _category = category;
            return this;
        }


        /// <summary>
        /// Updates the event type.
        /// </summary>
        /// <param name="type">
        ///   The event type.
        /// </param>
        /// <returns>
        ///   The updated <see cref="EventMessageBuilder"/>.
        /// </returns>
        public EventMessageBuilder WithType(string? type) {
            _type = type;
            return this;
        }


        /// <summary>
        /// Updates the event message.
        /// </summary>
        /// <param name="message">
        ///   The message.
        /// </param>
        /// <returns>
        ///   The updated <see cref="EventMessageBuilder"/>.
        /// </returns>
        public EventMessageBuilder WithMessage(string? message) {
            _message = message;
            return this;
        }

    }
}
