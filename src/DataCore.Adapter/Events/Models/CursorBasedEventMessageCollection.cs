using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Describes the results of a historical event message query performed using a cursor to 
    /// represent the query start position.
    /// </summary>
    public sealed class CursorBasedEventMessageCollection : EventMessagesContainer {

        /// <summary>
        /// The cursor position that represents the position immediately after the last event in the 
        /// collection.
        /// </summary>
        public string Cursor { get; }


        /// <summary>
        /// Creates a new <see cref="CursorBasedEventMessageCollection"/> object.
        /// </summary>
        /// <param name="cursor">
        ///   The cursor position that represents the position immediately after the last event in the 
        ///   collection.
        /// </param>
        /// <param name="events">
        ///   The event messages.
        /// </param>
        public CursorBasedEventMessageCollection(string cursor, IEnumerable<EventMessage> events) : base(events) {
            Cursor = cursor;
        }

    }
}
