using System;
using System.Collections.Generic;
using System.Linq;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.Csv {

    /// <summary>
    /// Describes tags and tag values parsed from a CSV.
    /// </summary>
    public class CsvDataSet {

        /// <summary>
        /// The tag definitions, indexed by ID.
        /// </summary>
        public IDictionary<string, TagDefinition> Tags { get; internal set; } = default!;

        /// <summary>
        /// The tag definitions, indexed by name.
        /// </summary>
        internal ILookup<string, TagDefinition> TagsByName { get; set; } = default!;

        /// <summary>
        /// The tag count.
        /// </summary>
        public int TagCount => Tags?.Count ?? 0;

        /// <summary>
        /// The UTC sample times read from the CSV.
        /// </summary>
        public IEnumerable<DateTime> UtcSampleTimes { get; internal set; } = default!;

        /// <summary>
        /// The raw tag values read from the CSV, indexed by tag ID and then UTC sample time.
        /// </summary>
        public IDictionary<string, SortedList<DateTime, TagValueExtended>> Values { get; internal set; } = default!;

        /// <summary>
        /// The earliest UTC sample time read from the CSV.
        /// </summary>
        public DateTime UtcEarliestSampleTime { get; internal set; }

        /// <summary>
        /// The latest UTC sample time read from the CSV.
        /// </summary>
        public DateTime UtcLatestSampleTime { get; internal set; }

        /// <summary>
        /// The total duration of samples read from the CSV.
        /// </summary>
        public TimeSpan DataSetDuration { get; internal set; }

        /// <summary>
        /// The number of rows read from the CSV.
        /// </summary>
        public long RowsRead { get; internal set; }

        /// <summary>
        /// The number of rows that were skipped when reading.
        /// </summary>
        public long RowsSkipped { get; internal set; }

        /// <summary>
        /// Indicates if the adapter can loop over the data set to supply results for data queries. 
        /// When <see langword="false"/>, only the original time range covered by the CSV data can be 
        /// queried.
        /// </summary>
        public bool IsDataLoopingAllowed { get; internal set; }

    }
}
