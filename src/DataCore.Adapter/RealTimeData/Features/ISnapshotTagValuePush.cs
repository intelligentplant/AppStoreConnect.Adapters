using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Features {

    /// <summary>
    /// Feature for subscribing to receive snapshot tag value changes from an adapter via a push 
    /// notification.
    /// </summary>
    public interface ISnapshotTagValuePush : IAdapterFeature {

        /// <summary>
        /// Creates a push subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="channel">
        ///   The channel to write snapshot values to.
        /// </param>
        /// <returns>
        ///   A subscription object that can be disposed once the subscription is no longer required.
        /// </returns>
        ISnapshotTagValueSubscription Subscribe(IAdapterCallContext context, ChannelWriter<SnapshotTagValue> channel);

    }
}
