using System;
using System.Collections.Generic;
using System.Text;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Describes an event message pushed from an adapter to a subscriber.
    /// </summary>
    public sealed class PushedEventMessage {

        /// <summary>
        /// The ID of the adapter that emitted the message.
        /// </summary>
        public string AdapterId { get; }

        /// <summary>
        /// The event message.
        /// </summary>
        public EventMessage Event { get; }


        /// <summary>
        /// Creates a new <see cref="PushedEventMessage"/> object.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter that emitted the message.
        /// </param>
        /// <param name="event">
        ///   The event message.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="event"/> is <see langword="null"/>.
        /// </exception>
        public PushedEventMessage(string adapterId, EventMessage @event) {
            AdapterId = adapterId ?? throw new ArgumentNullException(nameof(adapterId));
            Event = @event ?? throw new ArgumentNullException(nameof(@event));
        }

    }
}
