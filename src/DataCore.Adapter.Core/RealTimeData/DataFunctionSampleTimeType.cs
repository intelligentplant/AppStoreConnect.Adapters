using System.Text.Json.Serialization;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes how the sample time for a data function calculation is computed.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DataFunctionSampleTimeType {

        /// <summary>
        /// The computation method is not specified.
        /// </summary>
        Unspecified,

        /// <summary>
        /// The sample time for a calculated value is the start time of the interval that the 
        /// calculation is being performed for.
        /// </summary>
        StartTime,

        /// <summary>
        /// The sample time for a calculated value is the end time of the interval that the 
        /// calculation is being performed for.
        /// </summary>
        EndTime,

        /// <summary>
        /// The sample time for a calculated value is the sample time of a raw sample in the 
        /// interval that the calculation is being performed for.
        /// </summary>
        Raw,

        /// <summary>
        /// A custom method is used to compute the sample time.
        /// </summary>
        Custom

    }
}
