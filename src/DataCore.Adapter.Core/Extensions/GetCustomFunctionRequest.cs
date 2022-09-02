using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// A request to get the extended descriptor for a custom function.
    /// </summary>
    public class GetCustomFunctionRequest : AdapterRequest {

        /// <summary>
        /// The function ID.
        /// </summary>
        /// <remarks>
        ///   The <see cref="Id"/> must be an absolute URI.
        /// </remarks>
        [Required]
        public Uri Id { get; set; } = default!;


        /// <inheritdoc/>
        protected override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            foreach (var item in base.Validate(validationContext)) {
                yield return item;
            }

            if (Id != null && !Id.IsAbsoluteUri) {
                yield return new ValidationResult(SharedResources.Error_AbsoluteUriRequired, new[] { nameof(Id) });
            }
        }

    }
}
