using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// A request to invoke a custom adapter function.
    /// </summary>
    public class CustomFunctionInvocationRequest : AdapterRequest {

        /// <summary>
        /// The ID of the operation.
        /// </summary>
        /// <remarks>
        ///   The <see cref="Id"/> must be an absolute URI.
        /// </remarks>
        [Required]
        public Uri Id { get; set; } = default!;

        /// <summary>
        /// The request body.
        /// </summary>
        [Required]
        public JsonElement Body { get; set; }


        /// <inheritdoc/>
        protected override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            foreach (var item in base.Validate(validationContext)) {
                yield return item;
            }

            if (Id != null && Id.IsAbsoluteUri) {
                yield return new ValidationResult(SharedResources.Error_AbsoluteUriRequired, new[] { nameof(Id) });
            }
        }

    }
}
