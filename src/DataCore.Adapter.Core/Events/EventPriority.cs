﻿using System.Text.Json.Serialization;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Describes the priority associated with an event message.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
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
