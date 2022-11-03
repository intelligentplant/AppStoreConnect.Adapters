using System.Collections.Generic;
using System.Linq;

namespace DataCore.Adapter.RealTimeData.Utilities {
    public readonly struct PlotValue {

        public readonly TagValueExtended Sample { get; }

        public readonly string[]? Criteria { get; }


        public PlotValue(TagValueExtended sample, IEnumerable<string>? criteria) {
            Sample = sample;
            Criteria = criteria?.ToArray();
        }


        public PlotValue(TagValueExtended sample, params string[] criteria) {
            Sample = sample;
            Criteria = criteria;
        }

    }
}
