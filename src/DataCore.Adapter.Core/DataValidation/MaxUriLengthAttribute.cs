using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.DataValidation {

    /// <summary>
    /// Specifies the maximum length allowed on a <see cref="Uri"/> property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class MaxUriLengthAttribute : ValidationAttribute {

        /// <summary>
        /// The maximum length of the <see cref="Uri"/>.
        /// </summary>
        public int Length { get; }


        /// <summary>
        /// Creates a new <see cref="MaxUriLengthAttribute"/> instance.
        /// </summary>
        /// <param name="length">
        ///   The maximum length of the URI.
        /// </param>
        public MaxUriLengthAttribute(int length) { 
            Length = length; 
        }


        /// <inheritdoc/>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext) {
            if (Length > 0 && value is Uri uri && uri.ToString().Length > Length) {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }

            return null!;
        }


        /// <inheritdoc/>
        public override string FormatErrorMessage(string name) => new MaxLengthAttribute(Length).FormatErrorMessage(name);

    }
}
