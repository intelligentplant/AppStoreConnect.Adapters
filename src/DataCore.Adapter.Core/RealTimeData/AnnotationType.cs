﻿using System.Text.Json.Serialization;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Specifies the type of an annotation.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AnnotationType {

        /// <summary>
        /// The annotation type is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The annotation is instantaneous, and applies to a single point in time.
        /// </summary>
        Instantaneous = 1,

        /// <summary>
        /// The annotation applies to a time range, rather than a single point in time.
        /// </summary>
        TimeRange = 2

    }
}
