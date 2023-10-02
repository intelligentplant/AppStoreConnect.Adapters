using System.Threading.Channels;

namespace DataCore.Adapter.Subscriptions {
    public class SubscriptionManagerOptions {

        public int PublishChannelCapacity { get; set; } = 10000;

        public BoundedChannelFullMode PublishChannelFullMode { get; set; } = BoundedChannelFullMode.Wait;

        public char[]? TopicLevelSeparators { get; set; }

        public char? SingleLevelWildcard { get; set; }

        public char? MultiLevelWildcard { get; set; }

        public bool EnableWildcardSubscriptions { get; set; }

        public bool Retain { get; set; }

    }
}
