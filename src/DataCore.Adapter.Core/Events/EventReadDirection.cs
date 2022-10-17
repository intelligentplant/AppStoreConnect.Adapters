using System.Text.Json.Serialization;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Describes the read direction for a historical event message read operation.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EventReadDirection {

        /// <summary>
        /// Read forwards from the query start time or cursor position.
        /// </summary>
        Forwards,

        /// <summary>
        /// Read backwards from the query end time or cursor position.
        /// </summary>
        Backwards

    }
}
