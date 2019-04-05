using System;
using System.Collections.Generic;
using System.Text;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes default/commonly-supported data functions for aggregation.
    /// </summary>
    public static class DefaultDataFunctions {

        /// <summary>
        /// Average value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Average => new DataFunctionDescriptor("AVG", Resources.DataFunction_Avg_Description);

        /// <summary>
        /// Minimum value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Minimum => new DataFunctionDescriptor("MIN", Resources.DataFunction_Min_Description);

        /// <summary>
        /// Maximum value over a time period.
        /// </summary>
        public static DataFunctionDescriptor Maximum => new DataFunctionDescriptor("MAX", Resources.DataFunction_Max_Description);

    }
}
