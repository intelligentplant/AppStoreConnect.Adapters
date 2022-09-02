using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.Extensions {
    public class CustomFunctionDescriptor : IValidatableObject {

        [Required]
        public Uri Id { get; set; } = default!;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = default!;

        [MaxLength(500)]
        public string? Description { get; set; }


        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (Id != null && !Id.IsAbsoluteUri) {
                yield return new ValidationResult(SharedResources.Error_AbsoluteUriRequired, new[] { nameof(Id) });
            }
        }
    }
}
