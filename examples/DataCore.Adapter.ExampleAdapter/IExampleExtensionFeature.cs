using System;

using DataCore.Adapter.Common;
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
            PingMessage ping
        );

    }


    [ExtensionFeatureDataType(typeof(IExampleExtensionFeature), "ping-message")]
    public class PingMessage {

        public Guid CorrelationId { get; set; } = Guid.NewGuid();

    }


    [ExtensionFeatureDataType(typeof(IExampleExtensionFeature), "pong-message")]
    public class PongMessage {

        public Guid CorrelationId { get; set; } = Guid.NewGuid();

    }

}
