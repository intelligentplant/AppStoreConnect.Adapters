namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes the data type for a tag.
    /// </summary>
    public enum TagDataType {

        /// <summary>
        /// The tag contains numeric values.
        /// </summary>
        Numeric,

        /// <summary>
        /// The tag contains text values.
        /// </summary>
        Text,

        /// <summary>
        /// The tag values represent discrete states.
        /// </summary>
        State

    }
}
