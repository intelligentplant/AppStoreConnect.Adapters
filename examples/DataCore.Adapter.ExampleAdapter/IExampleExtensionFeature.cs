using System;
using System.ComponentModel;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Example {

    /// <summary>
    /// Example adapter extension feature.
    /// </summary>
    [ExtensionFeature(
        "example/ping-pong",
        Name = "Ping Pong",
        Description = "Responds to every ping message with a pong message"
    )]
    public interface IExampleExtensionFeature : IAdapterExtensionFeature {

        [ExtensionFeatureOperation(typeof(ExampleAdapter.ExampleExtensionImpl), nameof(ExampleAdapter.ExampleExtensionImpl.GetPingDescriptor))]
        PongMessage Ping(
            IAdapterCallContext context,
            PingMessage message
        );

    }


    public class PingMessage {

        [Description("The correlation ID for the ping.")]
        public string CorrelationId { get; set; }

    }


    public class PongMessage {

        [Description("The correlation ID for the ping associated with this pong.")]
        public string CorrelationId { get; set; }

    }

}
