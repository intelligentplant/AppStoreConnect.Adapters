using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.DataSource.Models {

    /// <summary>
    /// Describes a request to get tag definitions by tag ID or name.
    /// </summary>
    public sealed class GetTagsRequest: IValidatableObject {

        /// <summary>
        /// The IDs or names of the tags to retrieve.
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
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext) {
            return Validate(validationContext);
        }


        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="validationContext">
        ///   The validation context.
        /// </param>
        /// <returns>
        ///   A collection of validation errors.
        /// </returns>
        private IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (Tags.Any(x => x == null)) {
                yield return new ValidationResult(Resources.Error_TagNameOrIdCannotBeNull, new[] { nameof(Tags) });
            }
        }

    }
}
