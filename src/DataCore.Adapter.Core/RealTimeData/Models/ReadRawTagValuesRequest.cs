using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes a request to retrieve raw tag values.
    /// </summary>
    public sealed class ReadRawTagValuesRequest: ReadHistoricalTagValuesRequest {

        /// <summary>
        /// The maximum number of samples to retrieve per tag. A value of less than one is interpreted 
        /// as meaning no limit. The adapter that handles the request can apply its own maximum limit 
        /// to the query.
        /// </summary>
        [Required]
        [DefaultValue(500)]
        public int SampleCount { get; set; }

        /// <summary>
        /// The boundary type to use for the query. This controls if only values inside the query time 
        /// range should be included in the result, or if the values immediately before and immediately 
        /// after the query time range should be included.
        /// </summary>
        [Required]
        [DefaultValue(RawDataBoundaryType.Inside)]
        public RawDataBoundaryType BoundaryType { get; set; }

    }
}
