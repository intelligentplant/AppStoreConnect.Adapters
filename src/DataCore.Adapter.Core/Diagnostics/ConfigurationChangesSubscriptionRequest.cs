using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// A request to create a configuration changes subscription.
    /// </summary>
    public class ConfigurationChangesSubscriptionRequest : AdapterRequest {

        /// <summary>
        /// The configuration item types to subscribe to.
        /// </summary>
        [MaxLength(100)]
        public IEnumerable<string> ItemTypes { get; set; } = Array.Empty<string>();


        /// <inheritdoc/>
        protected override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            foreach (var item in base.Validate(validationContext)) {
                yield return item;
            }

            if (ItemTypes != null && ItemTypes.Any(x => string.IsNullOrWhiteSpace(x))) {
                yield return new ValidationResult(SharedResources.Error_ItemTypeCannotBeNull, new[] { nameof(ItemTypes) });
            }

            if (ItemTypes != null && ItemTypes.Any(x => x?.Length > 50)) {
                yield return new ValidationResult(string.Format(CultureInfo.CurrentCulture, SharedResources.Error_CollectionItemIsTooLong, 50), new[] { nameof(ItemTypes) });
            }
        }

    }

}
