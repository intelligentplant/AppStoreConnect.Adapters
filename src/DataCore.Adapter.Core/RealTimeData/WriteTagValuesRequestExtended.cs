using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request to write snapshot or historical tag values.
    /// </summary>
    public class WriteTagValuesRequestExtended : WriteTagValuesRequest {

        /// <summary>
        /// The values to write.
        /// </summary>
        [Required]
        [MinLength(1)]
        [MaxLength(10000)]
        public WriteTagValueItem[] Values { get; set; } = Array.Empty<WriteTagValueItem>();

    }
}
