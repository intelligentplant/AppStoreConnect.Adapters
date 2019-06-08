using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Text;

namespace DataCore.Adapter.Csv {
    public class CsvAdapterOptions {

        /// <summary>
        /// Configuration options for a <see cref="CsvAdapter"/>.
        /// </summary>
        public const int DefaultSnapshotPushUpdateInterval = 30000;

        /// <summary>
        /// A callback that will return a stream to read CSV data from. The stream will be disposed 
        /// once the CSV data has been read.
        /// </summary>
        [Required]
        public Func<Stream> GetCsvStream { get; set; }

        /// <summary>
        /// The index of the time stamp field in the CSV file.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int TimeStampFieldIndex { get; set; }

        /// <summary>
        /// The time stamp format used in the CSV file. A value is only required if the time stamps 
        /// are specified in a non-standard format.
        /// </summary>
        public string TimeStampFormat { get; set; }

        /// <summary>
        /// The time zone that the time stamps are in.
        /// </summary>
        public string TimeZone { get; set; }

        /// <summary>
        /// The <see cref="System.Globalization.CultureInfo"/> to use when parsing numbers and time 
        /// stamps.
        /// </summary>
        public CultureInfo CultureInfo { get; set; }

        /// <summary>
        /// The interval, in milliseconds, between snapshot push updates. An internal polling request 
        /// will be made at this interval to determine if any subscribed tags have changes in value.
        /// </summary>
        public int SnapshotPushUpdateInterval { get; set; } = DefaultSnapshotPushUpdateInterval;

        /// <summary>
        /// Indicates if the adapter can loop over the data set to supply results for data queries. 
        /// When <see langword="false"/>, only the original time range covered by the CSV data can be 
        /// queried.
        /// </summary>
        public bool IsDataLoopingAllowed { get; set; }

    }
}
