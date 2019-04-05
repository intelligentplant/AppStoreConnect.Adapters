using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.Events.Models {
    public abstract class EventMessagesContainer {

        public IEnumerable<EventMessage> Events { get; }


        protected EventMessagesContainer(IEnumerable<EventMessage> events) {
            Events = events?.ToArray() ?? new EventMessage[0];
        }

    }
}
