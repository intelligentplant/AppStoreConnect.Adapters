using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Describes a request to retrieve historical event messages using a time range.
    /// </summary>
    public class ReadEventMessagesForTimeRangeRequest : ReadHistoricalEventMessagesRequest {

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
                yield return new ValidationResult(Resources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, new[] { nameof(UtcStartTime) });
            }
        }

    }
}
