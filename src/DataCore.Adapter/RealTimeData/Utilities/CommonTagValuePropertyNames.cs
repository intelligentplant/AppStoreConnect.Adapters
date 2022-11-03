
namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Defines common tag property names.
    /// </summary>
    public static class CommonTagValuePropertyNames {

        /// <summary>
        /// Values calculated using <see cref="AggregationHelper"/> will contain a property with 
        /// this name.
        /// </summary>
        public const string XPoweredBy = "X-Powered-By";

        /// <summary>
        /// Describes the start time for a tag value bucket used in an aggregation calculation.
        /// </summary>
        public const string BucketStart = "Bucket-Start";

        /// <summary>
        /// Describes the end time for a tag value bucket used in an aggregation calculation.
        /// </summary>
        public const string BucketEnd = "Bucket-End";

        /// <summary>
        /// Describes the criteria that were used to select or compute a sample.
        /// </summary>
        public const string Criteria = "Criteria";

        /// <summary>
        /// Specifies the average value used in variance and standard deviation calculations.
        /// </summary>
        public const string Average = "Average";

        /// <summary>
        /// Specifies the variance value used in standard deviation calculations.
        /// </summary>
        public const string Variance = "Variance";

        /// <summary>
        /// Specifies the upper bound calculated in standard deviation calculations.
        /// </summary>
        public const string UpperBound = "Upper-Bound";

        /// <summary>
        /// Specifies the lower bound calculated in standard deviation calculations.
        /// </summary>
        public const string LowerBound = "Lower-Bound";

        /// <summary>
        /// Specifies the sigma value used when calculating the bounds for standard deviation calculations.
        /// </summary>
        public const string Sigma = "Sigma";

    }
}
