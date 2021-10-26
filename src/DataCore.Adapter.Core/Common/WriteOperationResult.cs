﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Base class for result objects returned from write operations.
    /// </summary>
    public abstract class WriteOperationResult {

        /// <summary>
        /// The status code for the operation.
        /// </summary>
        public StatusCode StatusCode { get; }

        /// <summary>
        /// Notes associated with the operation.
        /// </summary>
        public string? Notes { get; }

        /// <summary>
        /// Additional properties related to the operation.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; }


        /// <summary>
        /// Creates a new <see cref="WriteOperationResult"/>.
        /// </summary>
        /// <param name="statusCode">
        ///   The status code for the operation.
        /// </param>
        /// <param name="notes">
        ///   Notes associated with the operation.
        /// </param>
        /// <param name="properties">
        ///   Additional properties related to the operation.
        /// </param>
        protected WriteOperationResult(StatusCode statusCode, string? notes, IEnumerable<AdapterProperty>? properties) {
            StatusCode = statusCode;
            Notes = notes;
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }

    }
}
