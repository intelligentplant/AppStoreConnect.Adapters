using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.DataSource {

    /// <summary>
    /// Describes default/commonly-supported data functions for aggregation.
    /// </summary>
    public static class DefaultDataFunctions {

        /// <summary>
        /// Average value over a time period.
        /// </summary>
        public const string Average = "AVG";

        /// <summary>
        /// Minimum value over a time period.
        /// </summary>
        public const string Minimum = "MIN";

        /// <summary>
        /// Maximum value over a time period.
        /// </summary>
        public const string Maximum = "MAX";

        /// <summary>
        /// Standard deviation over a time period.
        /// </summary>
        public const string StandardDeviation = "STDDEV";

    }
}
