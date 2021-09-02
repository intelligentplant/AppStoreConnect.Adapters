using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MyAdapter {

    public class PingMessage {

        [Required]
        [Description("The correlation ID for the ping.")]
        public string CorrelationId { get; set; }

        [Description("The UTC time that the ping was sent at.")]
        public DateTime UtcTime { get; set; } = DateTime.UtcNow;

    }

    public class PongMessage {

        [Required]
        [Description("The correlation ID for the ping associated with this pong.")]
        public string CorrelationId { get; set; }

        [Description("The UTC time that the pong was sent at.")]
        public DateTime UtcTime { get; set; } = DateTime.UtcNow;
    }

}
