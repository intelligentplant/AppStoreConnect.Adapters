using System;
using System.Collections.Generic;
using System.Linq;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Describes the result of an event message write operation.
    /// </summary>
    public sealed class WriteEventMessageResult : WriteOperationResult {

        /// <summary>
        /// The optional correlation ID for the operation.
        /// </summary>
        public string CorrelationId { get; }


        /// <summary>
        /// Creates a new <see cref="WriteEventMessageResult"/> object.
        /// </summary>
        /// <param name="correlationId">
        ///   The optional correlation ID for the operation.
        /// </param>
        /// <param name="status">
        ///   A flag indicating if the write was successful.
        /// </param>
        /// <param name="notes">
        ///   Notes associated with the write.
        /// </param>
        /// <param name="properties">
        ///   Additional properties related to the write.
        /// </param>,
        /// 
        public WriteEventMessageResult(
            string correlationId,
            WriteStatus status, 
            string notes, 
            IEnumerable<AdapterProperty> properties
        ) : base(status, notes, properties) {
            CorrelationId = correlationId;
        }


        /// <summary>
        /// Creates a new <see cref="WriteEventMessageResult"/> object.
        /// </summary>
        /// <param name="correlationId">
        ///   The optional correlation ID for the operation.
        /// </param>
        /// <param name="status">
        ///   A flag indicating if the write was successful.
        /// </param>
        /// <param name="notes">
        ///   Notes associated with the write.
        /// </param>
        /// <param name="properties">
        ///   Additional properties related to the write.
        /// </param>
        public static WriteEventMessageResult Create(string correlationId, WriteStatus status, string notes, IEnumerable<AdapterProperty> properties)  {
            return new WriteEventMessageResult(correlationId, status, notes, properties);
        }

    }
}
