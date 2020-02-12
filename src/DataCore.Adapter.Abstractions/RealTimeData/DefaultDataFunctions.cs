using System.Linq;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes default/commonly-supported data functions for aggregation.
    /// </summary>
    public static class DefaultDataFunctions {

        /// <summary>
        /// Interpolated data function name.
        /// </summary>
        private const string FunctionNameInterpolate = "INTERP";

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
        /// Percent good function name.
        /// </summary>
        private const string FunctionNamePercentGood = "PERCENTGOOD";
        
        /// <summary>
        /// Percent bad function name.
        /// </summary>
        private const string FunctionNamePercentBad= "PERCENTBAD";

        /// <summary>
        /// Interpolation between samples.
        /// </summary>
        public static DataFunctionDescriptor Interpolate { get; } = DataFunctionDescriptor.Create(
            FunctionNameInterpolate,
            Resources.DataFunction_Interp_Name,
            Resources.DataFunction_Interp_Description
        );

        /// <summary>
        /// Average value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Average { get; } = DataFunctionDescriptor.Create(
            FunctionNameAverage,
            Resources.DataFunction_Avg_Name, 
            Resources.DataFunction_Avg_Description
        );

        /// <summary>
        /// Minimum value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Minimum { get; } = DataFunctionDescriptor.Create(
            FunctionNameMinimum,
            Resources.DataFunction_Min_Name,
            Resources.DataFunction_Min_Description
        );

        /// <summary>
        /// Maximum value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Maximum { get; } = DataFunctionDescriptor.Create(
            FunctionNameMaximum,
            Resources.DataFunction_Max_Name,
            Resources.DataFunction_Max_Description
        );

        /// <summary>
        /// Recorded values over a time period.
        /// </summary>
        public static DataFunctionDescriptor Count { get; } = DataFunctionDescriptor.Create(
            FunctionNameCount,
            Resources.DataFunction_Count_Name,
            Resources.DataFunction_Count_Description
        );

        /// <summary>
        /// Difference between the minimum and maximum value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Range { get; } = DataFunctionDescriptor.Create(
            FunctionNameRange,
            Resources.DataFunction_Range_Name,
            Resources.DataFunction_Range_Description
        );

        /// <summary>
        /// Percentage of raw samples in a time period that have good-quality status. 
        /// </summary>
        public static DataFunctionDescriptor PercentGood { get; } = DataFunctionDescriptor.Create(
            FunctionNamePercentGood,
            Resources.DataFunction_PercentGood_Name,
            Resources.DataFunction_PercentGood_Description
        );

        /// <summary>
        /// Percentage of raw samples in a time period that have bad-quality status. 
        /// </summary>
        public static DataFunctionDescriptor PercentBad { get; } = DataFunctionDescriptor.Create(
            FunctionNamePercentBad,
            Resources.DataFunction_PercentBad_Name,
            Resources.DataFunction_PercentBad_Description
        );


        /// <summary>
        /// Collection of all default data functions, used by <see cref="FindById"/>.
        /// </summary>
        private static readonly DataFunctionDescriptor[] s_defaultDataFunctions = { 
            Interpolate,
            Average,
            Minimum,
            Maximum,
            Count,
            Range,
            PercentGood,
            PercentBad
        };


        /// <summary>
        /// Finds the specified default <see cref="DataFunctionDescriptor"/> for a given ID.
        /// </summary>
        /// <param name="id">
        ///   The ID of the data function to retrieve.
        /// </param>
        /// <returns>
        ///   The matching <see cref="DataFunctionDescriptor"/>, or <see langword="null"/> if no 
        ///   match was found.
        /// </returns>
        public static DataFunctionDescriptor FindById(string id) {
            if (string.IsNullOrEmpty(id)) {
                return null;
            }

            return s_defaultDataFunctions.FirstOrDefault(x => string.Equals(x.Id, id, System.StringComparison.Ordinal));
        }

    }
}
