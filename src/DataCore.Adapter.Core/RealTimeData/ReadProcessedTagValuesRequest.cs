using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request to retrieve processed (aggregated) tag values.
    /// </summary>
    public sealed class ReadProcessedTagValuesRequest: ReadHistoricalTagValuesRequest {

        /// <summary>
        /// The aggregate data functions to request.
        /// </summary>
        [Required]
        [MinLength(1)]
        public IEnumerable<string> DataFunctions { get; set; }

        /// <summary>
        /// The sample interval to use in the aggregation.
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

            if (DataFunctions.Any(x => x == null)) {
                yield return new ValidationResult(SharedResources.Error_DataFunctionCannotBeNull, new[] { nameof(DataFunctions) });
            }

            if (SampleInterval <= TimeSpan.Zero) {
                yield return new ValidationResult(SharedResources.Error_SampleIntervalMustBeGreaterThanZero, new[] { nameof(SampleInterval) });
            }
        }

    }
}
