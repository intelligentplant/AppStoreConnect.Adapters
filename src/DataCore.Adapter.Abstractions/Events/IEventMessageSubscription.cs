using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using DataCore.Adapter.Events.Models;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Defines a subscription for receiving event messages as push notifications.
    /// </summary>
    public interface IEventMessageSubscription : IDisposable {

        /// <summary>
        /// A channel reader that emitted event messages can be read from.
        /// </summary>
        ChannelReader<EventMessage> Reader { get; }

    }

}
