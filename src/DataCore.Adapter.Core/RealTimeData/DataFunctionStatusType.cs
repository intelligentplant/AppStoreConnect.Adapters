using System.Text.Json.Serialization;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes how the quality status for a data function calculation is computed.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DataFunctionStatusType {

        /// <summary>
        /// The computation method is not specified.
        /// </summary>
        Unspecified = 1,

        /// <summary>
        /// The status is calculated based on a percentage of value counts.
        /// </summary>
        PercentValues = 2,

        /// <summary>
        /// The status is calculated based on a percentage of the time interval.
        /// </summary>
        PercentTime = 3,

        /// <summary>
        /// The status calculation is custom to the data function.
        /// </summary>
        Custom = 4,

        /// <summary>
        /// The quality status matches the status of a raw value selected by the function.
        /// </summary>
        Raw = 5,

        /// <summary>
        /// The status calculation always returns <see cref="TagValueStatus.Good"/>.
        /// </summary>
        AlwaysGood = 6,

        /// <summary>
        /// The quality status is the worst-case of all of the samples in the sample bucket used 
        /// to calculate the data function result.
        /// </summary>
        WorstCase = 7

    }
}
