using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataCore.Adapter.Common {

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
        public IEnumerable<AdapterProperty> Properties { get; }


        /// <summary>
        /// Creates a new <see cref="WriteOperationResult"/>.
        /// </summary>
        /// <param name="status">
        ///   Indicates if the write was successful.
        /// </param>
        /// <param name="notes">
        ///   Notes associated with the write.
        /// </param>
        /// <param name="properties">
        ///   Additional properties related to the write.
        /// </param>
        protected WriteOperationResult(WriteStatus status, string notes, IEnumerable<AdapterProperty> properties) {
            Status = status;
            Notes = notes;
            Properties = properties?.ToArray();
        }

    }
}
