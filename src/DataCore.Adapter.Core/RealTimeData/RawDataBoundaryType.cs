namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a boundary type used when making a raw data query.
    /// </summary>
    public enum RawDataBoundaryType {

        /// <summary>
        /// Only raw values inside the requested time range should be returned.
        /// </summary>
        Inside,

        /// <summary>
        /// If available, the raw value immediately before the request start time and immediately 
        /// after the request end time should be included in the result.
        /// </summary>
        Outside

    }
}
