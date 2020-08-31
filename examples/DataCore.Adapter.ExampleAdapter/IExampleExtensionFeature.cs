using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Example {

    /// <summary>
    /// Example adapter extension feature.
    /// </summary>
    [AdapterFeature("asc:extension/example/ping-pong")]
    public interface IExampleExtensionFeature : IAdapterExtensionFeature {

        [Display(Name = "Ping", Description = "Performs a ping operation on the adapter.")]
        [InputParameterDescription("The ping message.")]
        [OutputParameterDescription("The pong message.")]
        PongMessage Ping(
            IAdapterCallContext context,
            PingMessage ping
        );

    }


    public class PingMessage {

        public Guid CorrelationId { get; set; } = Guid.NewGuid();

    }


    public class PongMessage {

        public Guid CorrelationId { get; set; } = Guid.NewGuid();

    }

}
