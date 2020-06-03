using System.ComponentModel.DataAnnotations;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request to add or remove a tag to/from a snapshot tag value subscription.
    /// </summary>
    public class UpdateSnapshotTagValueSubscriptionRequest {

        /// <summary>
        /// The tag ID or name to add or remove.
        /// </summary>
        [Required]
        public string Tag { get; set; }

        /// <summary>
        /// The type of the subscription modification.
        /// </summary>
        public SubscriptionUpdateAction Action { get; set; }

    }
}
