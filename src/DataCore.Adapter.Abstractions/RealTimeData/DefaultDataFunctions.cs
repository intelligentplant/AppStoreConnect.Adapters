using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes default/commonly-supported data functions for aggregation.
    /// </summary>
    public static class DefaultDataFunctions {

        /// <summary>
        /// Average data function name.
        /// </summary>
        private const string FunctionNameAverage = "AVG";

        /// <summary>
        /// Minimum data function name.
        /// </summary>
        private const string FunctionNameMinimum = "MIN";

        /// <summary>
        /// Maximum data function name.
        /// </summary>
        private const string FunctionNameMaximum = "MAX";

        /// <summary>
        /// Average value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Average { get; } = DataFunctionDescriptor.Create(
            FunctionNameAverage, 
            Resources.DataFunction_Avg_Description
        );

        /// <summary>
        /// Minimum value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Minimum { get; } = DataFunctionDescriptor.Create(
            FunctionNameMinimum, 
            Resources.DataFunction_Min_Description
        );

        /// <summary>
        /// Maximum value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Maximum { get; } = DataFunctionDescriptor.Create(
            FunctionNameMaximum, 
            Resources.DataFunction_Max_Description
        );

    }
}
