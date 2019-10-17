using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes a subscription to an adapter.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of item that is received by the subscription.
    /// </typeparam>
    public interface IAdapterSubscription<T> : IDisposable, IAsyncDisposable {

        /// <summary>
        /// Indicates if the subscription has been initialised.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// A channel reader that will emit items published to the subscription.
        /// </summary>
        ChannelReader<T> Reader { get; }

        /// <summary>
        /// Initialises the subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will initialise the subscription.
        /// </returns>
        ValueTask StartAsync(IAdapterCallContext context, CancellationToken cancellationToken);

    }
}
