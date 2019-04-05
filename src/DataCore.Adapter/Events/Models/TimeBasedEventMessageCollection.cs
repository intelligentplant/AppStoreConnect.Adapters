using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Describes the results of a historical event message query performed using a query time range.
    /// </summary>
    public sealed class TimeBasedEventMessageCollection : EventMessagesContainer {

        /// <summary>
        /// Creates a new <see cref="TimeBasedEventMessageCollection"/> object.
        /// </summary>
        /// <param name="events">
        ///   The event messages.
        /// </param>
        public TimeBasedEventMessageCollection(IEnumerable<EventMessage> events) : base(events) { }

    }
}
