using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// A request to create an event subscription for a specific event topic.
    /// </summary>
    public class CreateEventMessageTopicSubscriptionRequest : CreateEventMessageSubscriptionRequest {

        /// <summary>
        /// The topic names.
        /// </summary>
        [MaxLength(100)]
        public IEnumerable<string> Topics { get; set; } = Array.Empty<string>();


        /// <inheritdoc/>
        protected override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            foreach (var err in base.Validate(validationContext)) {
                yield return err;
            }

            if (Topics != null) {
                if (Topics.Any(string.IsNullOrWhiteSpace)) {
                    yield return new ValidationResult(SharedResources.Error_SubscriptionTopicsCannotBeNullOrWhiteSpace, new[] { nameof(Topics) });
                }
                if (Topics.Any(x => x?.Length > 500)) {
                    yield return new ValidationResult(string.Format(CultureInfo.CurrentCulture, SharedResources.Error_CollectionItemIsTooLong, 500), new[] { nameof(Topics) });
                }
                if (Topics.GroupBy(x => x).Any(x => x.Count() > 1)) {
                    yield return new ValidationResult(SharedResources.Error_DuplicateSubscriptionTopicsAreNotAllowed, new[] { nameof(Topics) });
                }
            }
        }

    }
}
