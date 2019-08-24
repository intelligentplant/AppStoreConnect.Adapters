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
        public WriteStatus Status { get; }

        /// <summary>
        /// Notes associated with the write.
        /// </summary>
        public string Notes { get; }

        /// <summary>
        /// Additional properties related to the write.
        /// </summary>
        public IDictionary<string, string> Properties { get; }


        /// <summary>
        /// Creates a new <see cref="WriteOperationResult"/> object.
        /// </summary>
        /// <param name="status">
        ///   A flag indicating if the write was successful.
        /// </param>
        /// <param name="notes">
        ///   Notes associated with the write.
        /// </param>
        /// <param name="properties">
        ///   Additional properties related to the write.
        /// </param>
        public WriteOperationResult(WriteStatus status, string notes, IDictionary<string, string> properties) {
            Status = status;
            Notes = notes?.Trim();
            Properties = new ReadOnlyDictionary<string, string>(properties ?? new Dictionary<string, string>());
        }

    }
}
