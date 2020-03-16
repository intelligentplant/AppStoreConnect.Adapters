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
    public interface IAdapterSubscription<T> : IDisposable {

        /// <summary>
        /// Indicates if the subscription has been initialised.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// The <see cref="IAdapterCallContext"/> for the subscriber.
        /// </summary>
        IAdapterCallContext Context { get; }

        /// <summary>
        /// A cancellation token that will fire when the subscription completes.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// A task that will complete when the subscription completes.
        /// </summary>
        Task Completed { get; }

        /// <summary>
        /// A channel reader that will emit items published to the subscription.
        /// </summary>
        ChannelReader<T> Values { get; }

        /// <summary>
        /// Cancels the subscription.
        /// </summary>
        void Cancel();

    }
}
