using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes a generic request to retrieve historical data from a set of tags.
    /// </summary>
    public abstract class ReadHistoricalTagValuesRequest: ReadTagDataRequest {

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
                yield return new ValidationResult(SharedResources.Error_StartTimeCannotBeGreaterThanOrEqualToEndTime, new[] { nameof(UtcStartTime) });
            }
        }

    }
}
