
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {
    /// <summary>
    /// Describes an update to a tag value subscription.
    /// </summary>
    public class TagValueSubscriptionUpdate {

        /// <summary>
        /// The subscription topics.
        /// </summary>
        [Required]
        [MinLength(1)]
        public IEnumerable<string> Tags { get; set; }

        /// <summary>
        /// The subscription action.
        /// </summary>
        public SubscriptionUpdateAction Action { get; set; }

    }
}
