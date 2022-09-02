using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// An extended <see cref="CustomFunctionDescriptor"/> that includes details about the 
    /// function's request and response schemas.
    /// </summary>
    public class CustomFunctionDescriptorExtended : CustomFunctionDescriptor {

        /// <summary>
        /// The JSON schema for the function's request body.
        /// </summary>
        public JsonElement RequestSchema { get; set; }

        /// <summary>
        /// The JSON schema for the function's response body.
        /// </summary>
        public JsonElement ResponseSchema { get; set; }


        /// <inheritdoc/>
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            foreach (var item in base.Validate(validationContext)) {
                yield return item;
            }

            if (RequestSchema.ValueKind != JsonValueKind.Undefined) {
                // TODO: validate schema
            }

            if (ResponseSchema.ValueKind != JsonValueKind.Undefined) {
                // TODO: validate schema
            }
        }

    }

}
