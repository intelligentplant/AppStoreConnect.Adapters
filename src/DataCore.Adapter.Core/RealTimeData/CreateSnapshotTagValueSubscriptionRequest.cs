using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// A request to create a snapshot tag value subscription.
    /// </summary>
    public class CreateSnapshotTagValueSubscriptionRequest : AdapterRequest {

        /// <summary>
        /// The tag names or IDs to subscribe to.
        /// </summary>
        [MaxLength(100)]
        public IEnumerable<string> Tags { get; set; }

        /// <summary>
        /// Specifies how frequently new values should be emitted from the subscription. 
        /// Specifying a positive value can result in data loss, as only the most recently-received 
        /// value will be emitted for the subscribed tag at each publish interval.
        /// </summary>
        public TimeSpan PublishInterval { get; set; }


        /// <inheritdoc/>
        protected override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            foreach (var err in base.Validate(validationContext)) {
                yield return err;
            }

            if (Tags != null) {
                if (Tags.Any(string.IsNullOrWhiteSpace)) {
                    yield return new ValidationResult(SharedResources.Error_SubscriptionTopicsCannotBeNullOrWhiteSpace, new[] { nameof(Tags) });
                }
                if (Tags.GroupBy(x => x).Any(x => x.Count() > 1)) {
                    yield return new ValidationResult(SharedResources.Error_DuplicateSubscriptionTopicsAreNotAllowed, new[] { nameof(Tags) });
                }
            }
        }

    }

}
