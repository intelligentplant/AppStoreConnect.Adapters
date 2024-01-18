using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a value being written to a tag.
    /// </summary>
    public sealed class WriteTagValueItem {

        /// <summary>
        /// An optional correlation ID to assign to the write operation.
        /// </summary>
        [MaxLength(500)]
        public string? CorrelationId { get; set; }

        /// <summary>
        /// The tag ID.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string TagId { get; set; } = default!;

        /// <summary>
        /// The tag value.
        /// </summary>
        [Required]
        public TagValue Value { get; set; } = default!;

    }
}
