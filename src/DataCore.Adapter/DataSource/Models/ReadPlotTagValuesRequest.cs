using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataCore.Adapter.DataSource.Models {

    /// <summary>
    /// Describes a request for visualization-friendly tag values.
    /// </summary>
    public sealed class ReadPlotTagValuesRequest: ReadHistoricalTagValuesRequest {

        /// <summary>
        /// The number of time intervals to use in the query. While the sample count may vary according 
        /// to the implementation of the adapter handling the query, callers should expect to receive 
        /// a number of samples equal to up to 4-5x the interval count.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int Intervals { get; set; }

    }
}
