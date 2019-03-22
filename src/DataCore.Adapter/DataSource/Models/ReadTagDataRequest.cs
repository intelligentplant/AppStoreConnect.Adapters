using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DataCore.Adapter.DataSource.Models {

    /// <summary>
    /// Base class for requests that query tags for data.
    /// </summary>
    public abstract class ReadTagDataRequest: IValidatableObject {

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
        protected virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (Tags.Any(x => x == null)) {
                yield return new ValidationResult(Resources.Error_TagNameOrIdCannotBeNull, new[] { nameof(Tags) });
            }
        }

    }

}
