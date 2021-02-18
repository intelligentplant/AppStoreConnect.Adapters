using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Describes a request to get tag definitions by tag ID or name.
    /// </summary>
    public sealed class GetTagsRequest : AdapterRequest {

        /// <summary>
        /// The IDs or names of the tags to retrieve.
        /// </summary>
        [Required]
        [MinLength(1)]
        public IEnumerable<string> Tags { get; set; } = Array.Empty<string>();


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
                yield return new ValidationResult(SharedResources.Error_NameOrIdCannotBeNull, new[] { nameof(Tags) });
            }
        }

    }
}
