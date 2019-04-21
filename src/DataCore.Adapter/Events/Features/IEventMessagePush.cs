using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.Events.Models;

namespace DataCore.Adapter.Events.Features {

    /// <summary>
    /// Feature for subscribing to receive event messages from an adapter via a push notification.
    /// </summary>
    public interface IEventMessagePush : IAdapterFeature {

        /// <summary>
        /// Creates a push subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="channel">
        ///   The channel to write event messages to.
        /// </param>
        /// <returns>
        ///   A subscription object that can be disposed once the subscription is no longer required.
        /// </returns>
        IEventMessageSubscription Subscribe(IAdapterCallContext context, ChannelWriter<EventMessage> channel);

    }

}
