using System.Linq;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes default/commonly-supported data functions for aggregation.
    /// </summary>
    public static class DefaultDataFunctions {

        /// <summary>
        /// Constants associated with <see cref="DefaultDataFunctions"/>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Organisation of static class")]
        public static class Constants {

            /// <summary>
            /// Interpolated data function ID.
            /// </summary>
            public const string FunctionIdInterpolate = "INTERP";

            /// <summary>
            /// Average data function ID.
            /// </summary>
            public const string FunctionIdAverage = "AVG";

            /// <summary>
            /// Minimum data function ID.
            /// </summary>
            public const string FunctionIdMinimum = "MIN";

            /// <summary>
            /// Maximum data function ID.
            /// </summary>
            public const string FunctionIdMaximum = "MAX";

            /// <summary>
            /// Count data function ID.
            /// </summary>
            public const string FunctionIdCount = "COUNT";

            /// <summary>
            /// Range data function ID.
            /// </summary>
            public const string FunctionIdRange = "RANGE";

            /// <summary>
            /// Delta data function ID.
            /// </summary>
            public const string FunctionIdDelta = "DELTA";

            /// <summary>
            /// Percent good function ID.
            /// </summary>
            public const string FunctionIdPercentGood = "PERCENTGOOD";

            /// <summary>
            /// Percent bad function ID.
            /// </summary>
            public const string FunctionIdPercentBad = "PERCENTBAD";

            /// <summary>
            /// Variance function ID.
            /// </summary>
            public const string FunctionIdVariance = "VARIANCE";

            /// <summary>
            /// Standard deviation function ID.
            /// </summary>
            public const string FunctionIdStandardDeviation = "STDDEV";

        }

        

        /// <summary>
        /// Interpolation between samples.
        /// </summary>
        public static DataFunctionDescriptor Interpolate { get; } = DataFunctionDescriptor.Create(
            Constants.FunctionIdInterpolate,
            DataCoreAdapterAbstractionsResources.DataFunction_Interp_Name,
            DataCoreAdapterAbstractionsResources.DataFunction_Interp_Description,
            DataFunctionSampleTimeType.StartTime,
            DataFunctionStatusType.Custom,
            new [] {
                AdapterProperty.Create(
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation, 
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_ValueWorstCase, 
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_Description
                )
            }
        );

        /// <summary>
        /// Average value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Average { get; } = DataFunctionDescriptor.Create(
            Constants.FunctionIdAverage,
            DataCoreAdapterAbstractionsResources.DataFunction_Avg_Name, 
            DataCoreAdapterAbstractionsResources.DataFunction_Avg_Description,
            DataFunctionSampleTimeType.StartTime,
            DataFunctionStatusType.Custom,
            new[] {
                AdapterProperty.Create(
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_ValueGoodUnlessNonGoodSkipped,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_Description
                )
            }
        );

        /// <summary>
        /// Minimum value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Minimum { get; } = DataFunctionDescriptor.Create(
            Constants.FunctionIdMinimum,
            DataCoreAdapterAbstractionsResources.DataFunction_Min_Name,
            DataCoreAdapterAbstractionsResources.DataFunction_Min_Description,
            DataFunctionSampleTimeType.Raw,
            DataFunctionStatusType.Custom,
            new[] {
                AdapterProperty.Create(
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_TimestampCalculation,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_TimestampCalculation_ValueMinimum,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_TimestampCalculation_Description
                ),
                AdapterProperty.Create(
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_ValueGoodUnlessNonGoodSkipped,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_Description
                )
            }
        );

        /// <summary>
        /// Maximum value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Maximum { get; } = DataFunctionDescriptor.Create(
            Constants.FunctionIdMaximum,
            DataCoreAdapterAbstractionsResources.DataFunction_Max_Name,
            DataCoreAdapterAbstractionsResources.DataFunction_Max_Description,
            DataFunctionSampleTimeType.Raw,
            DataFunctionStatusType.Custom,
            new[] {
                AdapterProperty.Create(
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_TimestampCalculation,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_TimestampCalculation_ValueMaximum,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_TimestampCalculation_Description
                ),
                AdapterProperty.Create(
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_ValueGoodUnlessNonGoodSkipped,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_Description
                )
            }
        );

        /// <summary>
        /// Recorded values over a time period.
        /// </summary>
        public static DataFunctionDescriptor Count { get; } = DataFunctionDescriptor.Create(
            Constants.FunctionIdCount,
            DataCoreAdapterAbstractionsResources.DataFunction_Count_Name,
            DataCoreAdapterAbstractionsResources.DataFunction_Count_Description,
            DataFunctionSampleTimeType.StartTime,
            DataFunctionStatusType.Custom,
            new[] {
                AdapterProperty.Create(
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_ValueGoodUnlessNonGoodSkipped,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_Description
                )
            }
        );

        /// <summary>
        /// Absolute difference between the minimum and maximum value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Range { get; } = DataFunctionDescriptor.Create(
            Constants.FunctionIdRange,
            DataCoreAdapterAbstractionsResources.DataFunction_Range_Name,
            DataCoreAdapterAbstractionsResources.DataFunction_Range_Description,
            DataFunctionSampleTimeType.StartTime,
            DataFunctionStatusType.Custom,
            new[] {
                AdapterProperty.Create(
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_ValueGoodUnlessNonGoodSkipped,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_Description
                )
            }
        );

        /// <summary>
        /// Signed difference between the earliest and latest value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Delta { get; } = DataFunctionDescriptor.Create(
            Constants.FunctionIdDelta,
            DataCoreAdapterAbstractionsResources.DataFunction_Delta_Name,
            DataCoreAdapterAbstractionsResources.DataFunction_Delta_Description,
            DataFunctionSampleTimeType.StartTime,
            DataFunctionStatusType.Custom,
            new[] {
                AdapterProperty.Create(
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_ValueGoodUnlessNonGoodSkipped,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_Description
                )
            }
        );

        /// <summary>
        /// Percentage of raw samples in a time period that have good-quality status. 
        /// </summary>
        public static DataFunctionDescriptor PercentGood { get; } = DataFunctionDescriptor.Create(
            Constants.FunctionIdPercentGood,
            DataCoreAdapterAbstractionsResources.DataFunction_PercentGood_Name,
            DataCoreAdapterAbstractionsResources.DataFunction_PercentGood_Description,
            DataFunctionSampleTimeType.StartTime,
            DataFunctionStatusType.Custom,
            new[] {
                AdapterProperty.Create(
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_ValueGood,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_Description
                )
            }
        );

        /// <summary>
        /// Percentage of raw samples in a time period that have bad-quality status. 
        /// </summary>
        public static DataFunctionDescriptor PercentBad { get; } = DataFunctionDescriptor.Create(
            Constants.FunctionIdPercentBad,
            DataCoreAdapterAbstractionsResources.DataFunction_PercentBad_Name,
            DataCoreAdapterAbstractionsResources.DataFunction_PercentBad_Description,
            DataFunctionSampleTimeType.StartTime,
            DataFunctionStatusType.Custom,
            new[] {
                AdapterProperty.Create(
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_ValueGood,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_Description
                )
            }
        );


        /// <summary>
        /// Variance of good-quality samples in a time period.
        /// </summary>
        public static DataFunctionDescriptor Variance { get; } = DataFunctionDescriptor.Create(
            Constants.FunctionIdVariance,
            DataCoreAdapterAbstractionsResources.DataFunction_Variance_Name,
            DataCoreAdapterAbstractionsResources.DataFunction_Variance_Description,
            DataFunctionSampleTimeType.StartTime,
            DataFunctionStatusType.Custom,
            new[] {
                AdapterProperty.Create(
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_ValueGoodUnlessNonGoodSkipped,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_Description
                )
            }
        );


        /// <summary>
        /// Standard deviation of good-quality samples in a time period.
        /// </summary>
        public static DataFunctionDescriptor StandardDeviation { get; } = DataFunctionDescriptor.Create(
            Constants.FunctionIdStandardDeviation,
            DataCoreAdapterAbstractionsResources.DataFunction_StandardDeviation_Name,
            DataCoreAdapterAbstractionsResources.DataFunction_StandardDeviation_Description,
            DataFunctionSampleTimeType.StartTime,
            DataFunctionStatusType.Custom,
            new[] {
                AdapterProperty.Create(
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_ValueGoodUnlessNonGoodSkipped,
                    DataCoreAdapterAbstractionsResources.DataFunction_Property_StatusCalculation_Description
                )
            }
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
            Delta,
            PercentGood,
            PercentBad,
            Variance,
            StandardDeviation
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
