using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request for interpolated data on a set of tags.
    /// </summary>
    public sealed class ReadInterpolatedTagValuesRequest: ReadHistoricalTagValuesRequest {

        /// <summary>
        /// The sample interval for the interpolation.
        /// </summary>
        [Required]
        public TimeSpan SampleInterval { get; set; }


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

            if (SampleInterval <= TimeSpan.Zero) {
                yield return new ValidationResult(SharedResources.Error_SampleIntervalMustBeGreaterThanZero, new[] { nameof(SampleInterval) });
            }
        }

    }
}
