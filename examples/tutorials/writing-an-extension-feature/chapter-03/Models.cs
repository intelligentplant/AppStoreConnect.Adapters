using System;

using DataCore.Adapter.Extensions;

namespace MyAdapter {

    [ExtensionFeatureDataType(typeof(PingPongExtension), "ping-message")]
    public class PingMessage {
        public string CorrelationId { get; set; }
        public DateTime UtcTime { get; set; } = DateTime.UtcNow;
    }

    [ExtensionFeatureDataType(typeof(PingPongExtension), "ping-message")]
    public class PongMessage {
        public string CorrelationId { get; set; }
        public DateTime UtcTime { get; set; } = DateTime.UtcNow;
    }

}
