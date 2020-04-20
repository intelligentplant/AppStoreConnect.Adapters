namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes how the quality status for a data function calculation is computed.
    /// </summary>
    public enum DataFunctionStatusType {

        /// <summary>
        /// The computation method is not specified.
        /// </summary>
        Unspecified,

        /// <summary>
        /// The status is calculated based on a percentage of value counts.
        /// </summary>
        PercentValues,

        /// <summary>
        /// The status is calculated based on a percentage of the time interval.
        /// </summary>
        PercentTime,

        /// <summary>
        /// The status calculation is custom to the data function.
        /// </summary>
        Custom

    }
}
