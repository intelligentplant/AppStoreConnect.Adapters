using System;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// A request to create a snapshot tag value subscription.
    /// </summary>
    public class CreateSnapshotTagValueSubscriptionRequest : AdapterRequest { 
    
        /// <summary>
        /// Specifies how frequently new values should be emitted from the subscription. 
        /// Specifying a positive value can result in data loss, as only the most recently-received 
        /// value will be emitted for each subscribed tag at each publish interval.
        /// </summary>
        public TimeSpan PublishInterval { get; set; }

    }

}
