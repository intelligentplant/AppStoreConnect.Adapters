using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request to retrieve tag values at specific sample times.
    /// </summary>
    public sealed class ReadTagValuesAtTimesRequest: ReadTagDataRequest {

        /// <summary>
        /// The UTC sample times to retrieve values at.
        /// </summary>
        [Required]
        [MinLength(1)]
        public DateTime[] UtcSampleTimes { get; set; }

    }
}
