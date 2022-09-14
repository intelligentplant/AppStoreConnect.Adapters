using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Describes basic information about a custom function on an adapter.
    /// </summary>
    public class CustomFunctionDescriptor : IValidatableObject {

        /// <summary>
        /// The ID of the custom function.
        /// </summary>
        /// <remarks>
        ///   The <see cref="Id"/> must be an absolute URI. It is not required that the URI can be 
        ///   dereferenced.
        /// </remarks>
        [Required]
        public Uri Id { get; }

        /// <summary>
        /// The name of the custom function.
        /// </summary>
        /// <remarks>
        ///   Function names must start with a letter or an underscore (<c>_</c>). Subsequent 
        ///   characters can be letters, decimal numbers, or underscores.
        /// </remarks>
        [Required]
        [MaxLength(100)]
        [RegularExpression("^[a-zA-Z_][a-zA-Z0-9_]+$")]
        public string Name { get; }

        /// <summary>
        /// The custom function description.
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; }


        /// <summary>
        /// Creates a new <see cref="CustomFunctionDescriptor"/> instance.
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
        [JsonConstructor]
        public CustomFunctionDescriptor(Uri id, string name, string? description) {
            Id = id;
            Name = name;
            Description = description;
        }


        /// <inheritdoc/>
        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (Id != null && !Id.IsAbsoluteUri) {
                yield return new ValidationResult(SharedResources.Error_AbsoluteUriRequired, new[] { nameof(Id) });
            }
        }

    }
}
