
using System;

namespace MyAdapter {

    public class PingMessage {
        public string CorrelationId { get; set; }
        public DateTime UtcTime { get; set; } = DateTime.UtcNow;
    }

    public class PongMessage {
        public string CorrelationId { get; set; }
        public DateTime UtcTime { get; set; } = DateTime.UtcNow;
    }

}
