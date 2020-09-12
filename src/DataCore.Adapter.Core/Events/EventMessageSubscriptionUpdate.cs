
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Describes an update to an event message subscription.
    /// </summary>
    public class EventMessageSubscriptionUpdate {

        /// <summary>
        /// The subscription topics.
        /// </summary>
        [Required]
        [MinLength(1)]
        public IEnumerable<string> Topics { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The subscription action.
        /// </summary>
        public SubscriptionUpdateAction Action { get; set; }

    }

}
