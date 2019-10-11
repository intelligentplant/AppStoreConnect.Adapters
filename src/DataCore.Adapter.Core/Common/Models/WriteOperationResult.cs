using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataCore.Adapter.Common.Models {

    /// <summary>
    /// Base class for result objects returned from write operations.
    /// </summary>
    public abstract class WriteOperationResult {

        /// <summary>
        /// Indicates if the write was successful.
        /// </summary>
        public WriteStatus Status { get; set; }

        /// <summary>
        /// Notes associated with the write.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Additional properties related to the write.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }

    }
}
