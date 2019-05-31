using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Describes the priority associated with an event message.
    /// </summary>
    public enum EventPriority {

        /// <summary>
        /// The priority is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Low priority.
        /// </summary>
        Low = 1,

        /// <summary>
        /// Medium priority.
        /// </summary>
        Medium = 2,

        /// <summary>
        /// High priority.
        /// </summary>
        High = 3,

        /// <summary>
        /// Critical priority.
        /// </summary>
        Critical = 4

    }
}
