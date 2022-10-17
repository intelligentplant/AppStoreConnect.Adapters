using System.Text.Json.Serialization;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes the quality status of a tag value.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TagValueStatus {

        /// <summary>
        /// The quality of the value is bad. This could indicate, for example, an instrument failure.
        /// </summary>
        Bad = 0,

        /// <summary>
        /// The quality of the value is unknown or uncertain.
        /// </summary>
        Uncertain = 64,

        /// <summary>
        /// The quality of the status is good.
        /// </summary>
        Good = 192

    }
}
