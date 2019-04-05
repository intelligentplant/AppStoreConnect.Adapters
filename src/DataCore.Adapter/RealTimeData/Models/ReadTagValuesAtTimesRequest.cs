using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

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
