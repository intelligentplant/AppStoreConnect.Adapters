﻿using System.Text.Json.Serialization;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Describes a category for a configuration change.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ConfigurationChangeType {

        /// <summary>
        /// The change type is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// An item was created.
        /// </summary>
        Created,

        /// <summary>
        /// An item was updated.
        /// </summary>
        Updated,

        /// <summary>
        /// An item was deleted.
        /// </summary>
        Deleted

    }
}
