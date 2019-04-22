using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Base class for event message queries.
    /// </summary>
    public abstract class EventMessagesContainer {

        /// <summary>
        /// The event messages.
        /// </summary>
        public IEnumerable<EventMessage> Events { get; }


        /// <summary>
        /// Creates a new <see cref="EventMessagesContainer"/> object.
        /// </summary>
        /// <param name="events">
        ///   The event messages.
        /// </param>
        protected EventMessagesContainer(IEnumerable<EventMessage> events) {
            Events = events?.ToArray() ?? new EventMessage[0];
        }

    }
}
