using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Base class for requests that query tags for data.
    /// </summary>
    public abstract class ReadTagDataRequest: AdapterRequest {

        /// <summary>
        /// The tag names or IDs to query.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string[] Tags { get; set; }


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
            if (Tags.Any(x => x == null)) {
                yield return new ValidationResult(SharedResources.Error_TagNameOrIdCannotBeNull, new[] { nameof(Tags) });
            }
        }

    }

}
