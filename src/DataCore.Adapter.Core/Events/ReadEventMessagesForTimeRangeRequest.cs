using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Describes a request to retrieve historical event messages using a time range.
    /// </summary>
    public class ReadEventMessagesForTimeRangeRequest : ReadHistoricalEventMessagesRequest, IPageableAdapterRequest {

        /// <summary>
        /// The topics to read messages for. This property will be ignored if the adapter does not 
        /// support a topic-based event model.
        /// </summary>
        [MaxLength(100)]
        public IEnumerable<string> Topics { get; set; }

        /// <summary>
        /// The UTC start time for the request.
        /// </summary>
        [Required]
        public DateTime UtcStartTime { get; set; }

        /// <summary>
        /// The UTC end time for the request.
        /// </summary>
        [Required]
        public DateTime UtcEndTime { get; set; }

        /// <summary>
        /// The page size for the query.
        /// </summary>
        [Range(1, int.MaxValue)]
        [DefaultValue(10)]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// The page number for the query.
        /// </summary>
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int Page { get; set; } = 1;


        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="validationContext">
        ///   The validation context.
        /// </param>
        /// <returns>
        ///   A collection of validation errors.
        /// </returns>
        protected override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            foreach (var item in base.Validate(validationContext)) {
                yield return item;
            }

            if (UtcStartTime >= UtcEndTime) {
                yield return new ValidationResult(SharedResources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, new[] { nameof(UtcStartTime) });
            }
        }

    }
}
