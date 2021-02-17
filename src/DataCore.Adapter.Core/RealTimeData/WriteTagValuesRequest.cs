using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request to write snapshot or historical tag values.
    /// </summary>
    public class WriteTagValuesRequest {

        /// <summary>
        /// The values to write.
        /// </summary>
        [Required]
        [MinLength(1)]
        public WriteTagValueItem[] Values { get; set; } = Array.Empty<WriteTagValueItem>();

    }
}
