using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using DataCore.Adapter.Events.Models;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes the result of a tag value write operation.
    /// </summary>
    public sealed class WriteTagValueResult {

        /// <summary>
        /// The ID of the tag.
        /// </summary>
        public string TagId { get; }

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
        /// Creates a new <see cref="WriteTagValueResult"/> object.
        /// </summary>
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
        public WriteTagValueResult(string tagId, WriteStatus status, string notes, IDictionary<string, string> properties) {
            TagId = tagId ?? throw new ArgumentNullException(nameof(tagId));
            Status = status;
            Notes = notes?.Trim();
            Properties = new ReadOnlyDictionary<string, string>(properties ?? new Dictionary<string, string>());
        }

    }
}
