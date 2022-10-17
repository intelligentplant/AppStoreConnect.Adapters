using System.Text.Json.Serialization;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes the status of a write operation.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WriteStatus {

        /// <summary>
        /// Write status is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The write was successful.
        /// </summary>
        Success,

        /// <summary>
        /// The write was unsuccessful.
        /// </summary>
        Fail,

        /// <summary>
        /// The write is pending (for example, it may have been added to a processing queue or 
        /// scheduled for later).
        /// </summary>
        Pending

    }
}
