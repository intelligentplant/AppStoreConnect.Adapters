using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes a subscription to an adapter.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of item that is received by the subscription.
    /// </typeparam>
    public interface IAdapterSubscription<T> : IDisposable, IAsyncDisposable {

        /// <summary>
        /// A channel reader that will emit items published to the subscription.
        /// </summary>
        ChannelReader<T> Reader { get; }

    }
}
