using System.Collections.Generic;
using System.Linq;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// A sample that has been selected from a time bucket in a plot data query.
    /// </summary>
    public readonly struct PlotValue {

        /// <summary>
        /// The selected sample.
        /// </summary>
        public readonly TagValueExtended Sample { get; }

        /// <summary>
        /// The criteria that were used to select the sample.
        /// </summary>
        public readonly string[]? Criteria { get; }


        /// <summary>
        /// Creates a new <see cref="PlotValue"/> instance.
        /// </summary>
        /// <param name="sample">
        ///   The selected sample.
        /// </param>
        /// <param name="criteria">
        ///   The criteria that were used to select the sample.
        /// </param>
        public PlotValue(TagValueExtended sample, IEnumerable<string>? criteria) {
            Sample = sample;
            Criteria = criteria?.ToArray();
        }


        /// <summary>
        /// Creates a new <see cref="PlotValue"/> instance.
        /// </summary>
        /// <param name="sample">
        ///   The selected sample.
        /// </param>
        /// <param name="criteria">
        ///   The criteria that were used to select the sample.
        /// </param>
        public PlotValue(TagValueExtended sample, params string[] criteria) {
            Sample = sample;
            Criteria = criteria;
        }

    }
}
