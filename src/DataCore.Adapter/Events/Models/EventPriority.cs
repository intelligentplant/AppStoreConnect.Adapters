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
        Unknown,

        /// <summary>
        /// Low priority.
        /// </summary>
        Low,

        /// <summary>
        /// Medium priority.
        /// </summary>
        Medium,

        /// <summary>
        /// High priority.
        /// </summary>
        High,

        /// <summary>
        /// Critical priority.
        /// </summary>
        Critical

    }
}
