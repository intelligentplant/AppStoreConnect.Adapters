﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes the result of a tag value write operation.
    /// </summary>
    public sealed class WriteTagValueResult : WriteOperationResult {

        /// <summary>
        /// The optional correlation ID for the operation.
        /// </summary>
        public string? CorrelationId { get; }

        /// <summary>
        /// The ID of the tag.
        /// </summary>
        [Required]
        public string TagId { get; }


        /// <summary>
        /// Creates a new <see cref="WriteTagValueResult"/> object.
        /// </summary>
        /// <param name="correlationId">
        ///   The optional correlation ID for the operation. Can be <see langword="null"/>.
        /// </param>
        /// <param name="tagId">
        ///   The ID of the tag that was written to.
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        [JsonConstructor]
        public WriteTagValueResult(
            string? correlationId, 
            string tagId, 
            WriteStatus status, 
            string? notes, 
            IEnumerable<AdapterProperty>? properties
        ) : base(status, notes, properties) {
            CorrelationId = correlationId;
            TagId = tagId ?? throw new ArgumentNullException(nameof(tagId));
        }


        /// <summary>
        /// Creates a new <see cref="WriteTagValueResult"/> object.
        /// </summary>
        /// <param name="correlationId">
        ///   The optional correlation ID for the operation. Can be <see langword="null"/>.
        /// </param>
        /// <param name="tagId">
        ///   The ID of the tag that was written to.
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        public static WriteTagValueResult Create(string? correlationId, string tagId, WriteStatus status, string? notes, IEnumerable<AdapterProperty>? properties) {
            return new WriteTagValueResult(correlationId, tagId, status, notes, properties);
        }

    }

}
