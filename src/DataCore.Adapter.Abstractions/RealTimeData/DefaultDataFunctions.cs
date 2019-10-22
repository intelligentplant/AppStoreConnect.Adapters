using System.Linq;

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
        /// Count data function name.
        /// </summary>
        private const string FunctionNameCount = "COUNT";

        /// <summary>
        /// Range data function name.
        /// </summary>
        private const string FunctionNameRange = "RANGE";

        /// <summary>
        /// Average value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Average { get; } = DataFunctionDescriptor.Create(
            FunctionNameAverage,
            FunctionNameAverage, 
            Resources.DataFunction_Avg_Description
        );

        /// <summary>
        /// Minimum value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Minimum { get; } = DataFunctionDescriptor.Create(
            FunctionNameMinimum,
            FunctionNameMinimum,
            Resources.DataFunction_Min_Description
        );

        /// <summary>
        /// Maximum value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Maximum { get; } = DataFunctionDescriptor.Create(
            FunctionNameMaximum,
            FunctionNameMaximum,
            Resources.DataFunction_Max_Description
        );

        /// <summary>
        /// Recorded values over a time period.
        /// </summary>
        public static DataFunctionDescriptor Count { get; } = DataFunctionDescriptor.Create(
            FunctionNameCount,
            FunctionNameCount,
            Resources.DataFunction_Count_Description
        );

        /// <summary>
        /// Difference between the minimum and maximum value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Range { get; } = DataFunctionDescriptor.Create(
            FunctionNameRange,
            FunctionNameRange,
            Resources.DataFunction_Range_Description
        );


        /// <summary>
        /// Collection of all default data functions, used by <see cref="FindByNameOrId"/>.
        /// </summary>
        private static readonly DataFunctionDescriptor[] s_defaultDataFunctions = { 
            Average,
            Minimum,
            Maximum,
            Count,
            Range
        };


        /// <summary>
        /// Finds the specified default <see cref="DataFunctionDescriptor"/> for name or ID.
        /// </summary>
        /// <param name="nameOrId">
        ///   The name or ID of the data function to retrieve.
        /// </param>
        /// <returns>
        ///   The matching <see cref="DataFunctionDescriptor"/>, or <see langword="null"/> if no 
        ///   match was found.
        /// </returns>
        public static DataFunctionDescriptor FindByNameOrId(string nameOrId) {
            if (string.IsNullOrEmpty(nameOrId)) {
                return null;
            }

            return s_defaultDataFunctions.FirstOrDefault(x => string.Equals(x.Id, nameOrId, System.StringComparison.Ordinal) || string.Equals(x.Name, nameOrId, System.StringComparison.OrdinalIgnoreCase));
        }

    }
}
