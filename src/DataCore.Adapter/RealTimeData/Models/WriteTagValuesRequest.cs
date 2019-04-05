using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes a request to write tag values.
    /// </summary>
    public class WriteTagValuesRequest : AdapterRequest {

        /// <summary>
        /// The values to write.
        /// </summary>
        [Required]
        [MinLength(1)]
        public TagValueWriteCollection[] Values { get; set; }


        /// <summary>
        /// Validates the request.
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

            if (Values.Where(x => x != null).ToLookup(x => x.TagId).Any(grp => grp.Count() > 1)) {
                yield return new ValidationResult(Resources.Error_DuplicateTagWriteCollectionsAreNotAllowed, new[] { nameof(Values) });
            }
        }

    }
}
