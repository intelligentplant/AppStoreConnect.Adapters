using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes a request to add or remove a topic to/from a subscription that supports topics.
    /// </summary>
    public class UpdateSubscriptionTopicsRequest {

        /// <summary>
        /// The topic or name to add or remove.
        /// </summary>
        [Required]
        public string Topic { get; set; } = default!;

        /// <summary>
        /// The type of the subscription modification.
        /// </summary>
        public SubscriptionUpdateAction Action { get; set; }

    }
}
