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

        PongMessage Ping(
            PingMessage ping
        );

    }


    [DataTypeId(WellKnownFeatures.Extensions.BaseUri + "example/ping-pong/types/ping")]
    public class PingMessage {

        public Guid CorrelationId { get; set; } = Guid.NewGuid();

    }


    [DataTypeId(WellKnownFeatures.Extensions.BaseUri + "example/ping-pong/types/pong")]
    public class PongMessage {

        public Guid CorrelationId { get; set; } = Guid.NewGuid();

    }

}
