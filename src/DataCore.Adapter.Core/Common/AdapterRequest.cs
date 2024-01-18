using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Base class that adapter request objects can inherit from.
    /// </summary>
    public abstract class AdapterRequest : IValidatableObject {

        /// <summary>
        /// The maximum length of a key in the <see cref="Properties"/> dictionary.
        /// </summary>
        public const int MaxPropertyKeyLength = 50;

        /// <summary>
        /// The maximum length of a value in the <see cref="Properties"/> dictionary.
        /// </summary>
        public const int MaxPropertyValueLength = 100;

        /// <summary>
        /// The maximum length of the <see cref="Properties"/> dictionary.
        /// </summary>
        public const int MaxPropertiesCount = 20;

        /// <summary>
        /// Additional request properties. These can be used to provide bespoke query parameters 
        /// supported by the adapter.
        /// </summary>
        public IDictionary<string, string>? Properties { get; set; }


        /// <summary>
        /// Validates the request.
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
        /// Validates the request.
        /// </summary>
        /// <param name="validationContext">
        ///   The validation context.
        /// </param>
        /// <returns>
        ///   A collection of validation errors.
        /// </returns>
        protected virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (Properties != null) {
                if (Properties.Any(x => x.Key.Length > MaxPropertyKeyLength)) {
                    yield return new ValidationResult(string.Format(CultureInfo.CurrentCulture, SharedResources.Error_KeyIsTooLong, MaxPropertyKeyLength), new[] { nameof(Properties) });
                }
                if (Properties.Any(x => x.Value != null && x.Value.Length > MaxPropertyValueLength)) {
                    yield return new ValidationResult(string.Format(CultureInfo.CurrentCulture, SharedResources.Error_ValueIsTooLong, MaxPropertyValueLength), new[] { nameof(Properties) });
                }
                if (Properties.Count > MaxPropertiesCount) {
                    yield return new ValidationResult(string.Format(CultureInfo.CurrentCulture, SharedResources.Error_TooManyEntries, MaxPropertiesCount), new[] { nameof(Properties) });
                }
            }
        }
    }
}
