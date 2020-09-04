﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Example {

    /// <summary>
    /// Example adapter extension feature.
    /// </summary>
    [AdapterExtensionFeature(
        "example/ping-pong",
        Name = "Ping Pong",
        Description = "Responds to every ping message with a pong message"
    )]
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
