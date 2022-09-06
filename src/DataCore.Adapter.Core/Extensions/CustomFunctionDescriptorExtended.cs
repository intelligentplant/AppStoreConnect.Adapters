using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// An extended <see cref="CustomFunctionDescriptor"/> that includes details about the 
    /// function's request and response schemas.
    /// </summary>
    public class CustomFunctionDescriptorExtended : CustomFunctionDescriptor {

        /// <summary>
        /// The JSON schema for the function's request body.
        /// </summary>
        /// <remarks>
        ///   If the <see cref="RequestSchema"/> is <see langword="null"/>, the custom function 
        ///   does not accept an input parameter.
        /// </remarks>
        public JsonElement? RequestSchema { get; }

        /// <summary>
        /// The JSON schema for the function's response body.
        /// </summary>
        /// <remarks>
        ///   If the <see cref="ResponseSchema"/> is <see langword="null"/>, the custom function 
        ///   does not return a result.
        /// </remarks>
        public JsonElement? ResponseSchema { get; }


        /// <summary>
        /// Creates a new <see cref="CustomFunctionDescriptorExtended"/> instance.
        /// </summary>
        /// <param name="id">
        ///   The function ID.
        /// </param>
        /// <param name="name">
        ///   The function name.
        /// </param>
        /// <param name="description">
        ///   The function description.
        /// </param>
        /// <param name="requestSchema">
        ///   The request schema.
        /// </param>
        /// <param name="responseSchema">
        ///   The response schema.
        /// </param>
        [JsonConstructor]
        public CustomFunctionDescriptorExtended(Uri id, string name, string? description, JsonElement? requestSchema, JsonElement? responseSchema) :
            base(id, name, description) {
            RequestSchema = requestSchema;
            ResponseSchema = responseSchema;
        }


        /// <inheritdoc/>
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            foreach (var item in base.Validate(validationContext)) {
                yield return item;
            }

            // JSON schemas must be true, false, or an object

            if (RequestSchema != null && RequestSchema.Value.ValueKind != JsonValueKind.Object && RequestSchema.Value.ValueKind != JsonValueKind.True && RequestSchema.Value.ValueKind != JsonValueKind.False) {
                yield return new ValidationResult(SharedResources.Error_InvalidJsonSchema, new[] { nameof(RequestSchema) });
            }

            if (ResponseSchema != null && ResponseSchema.Value.ValueKind != JsonValueKind.Object && ResponseSchema.Value.ValueKind != JsonValueKind.True && ResponseSchema.Value.ValueKind != JsonValueKind.False) {
                yield return new ValidationResult(SharedResources.Error_InvalidJsonSchema, new[] { nameof(ResponseSchema) });
            }
        }

    }

}
