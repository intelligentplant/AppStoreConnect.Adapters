using System;
using System.Collections.Generic;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Helper class for constructing <see cref="EventMessage"/> objects using a fluent interface.
    /// </summary>
    public class EventMessageBuilder {

        /// <summary>
        /// The event ID.
        /// </summary>
        private string _id = Guid.NewGuid().ToString();

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
        private string _category;

        /// <summary>
        /// The event message.
        /// </summary>
        private string _message;

        /// <summary>
        /// Additional event properties.
        /// </summary>
        private readonly Dictionary<string, string> _properties;


        /// <summary>
        /// Creates a new <see cref="EventMessageBuilder"/> object.
        /// </summary>
        private EventMessageBuilder() {
            _properties = new Dictionary<string, string>();
        }


        /// <summary>
        /// Creates a new <see cref="EventMessageBuilder"/> object that is initialised using an existing 
        /// event message.
        /// </summary>
        /// <param name="existing">
        ///   The existing value.
        /// </param>
        private EventMessageBuilder(EventMessage existing) {
            if (existing == null) {
                _properties = new Dictionary<string, string>();
                return;
            }

            _id = existing.Id;
            _utcEventTime = existing.UtcEventTime;
            _priority = existing.Priority;
            _category = existing.Category;
            _message = existing.Message;
            _properties = new Dictionary<string, string>(existing.Properties);
        }


        /// <summary>
        /// Creates a new <see cref="EventMessageBuilder"/> object that can be used to create an 
        /// <see cref="EventMessage"/> using a fluent configuration interface.
        /// </summary>
        /// <returns>
        ///   A new <see cref="EventMessageBuilder"/> object.
        /// </returns>
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
        public EventMessage Build() {
            return EventMessage.Create(_id, _utcEventTime, _priority, _category, _message, _properties);
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
        public EventMessageBuilder WithCategory(string category) {
            _category = category;
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
        public EventMessageBuilder WithMessage(string message) {
            _message = message;
            return this;
        }


        /// <summary>
        /// Adds a property to the event.
        /// </summary>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The property value.
        /// </param>
        /// <returns>
        ///   The updated <see cref="EventMessageBuilder"/>.
        /// </returns>
        public EventMessageBuilder WithProperty(string name, string value) {
            if (name != null) {
                _properties[name] = value;
            }
            return this;
        }


        /// <summary>
        /// Adds a set of properties to the event.
        /// </summary>
        /// <param name="properties">
        ///   The properties.
        /// </param>
        /// <returns>
        ///   The updated <see cref="EventMessageBuilder"/>.
        /// </returns>
        public EventMessageBuilder WithProperties(IDictionary<string, string> properties) {
            if (properties != null) {
                foreach (var item in properties) {
                    _properties[item.Key] = item.Value;
                }
            }

            return this;
        }

    }
}
