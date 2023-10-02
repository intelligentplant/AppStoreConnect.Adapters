using System;

namespace DataCore.Adapter.RealTimeData {
    public class PollingSnapshotSubscriptionManagerOptions : SnapshotSubscriptionManagerOptions {

        public TimeSpan PollingInterval { get; set; }

    }
}
