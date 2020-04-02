using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for subscribing to receive snapshot tag value changes from an adapter via a push 
    /// notification.
    /// </summary>
    public interface ISnapshotTagValuePush : IAdapterFeature {

        /// <summary>
        /// Creates a snapshot value change subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <returns>
        ///   A task that will create and start a subscription object that can be disposed once 
        ///   the subscription is no longer required.
        /// </returns>
        /// <remarks>
        ///   When the <see cref="ISnapshotTagValueSubscription"/> is created, it must immediately 
        ///   publish a value to the subscriber to indicate that the subscription is operational. 
        ///   It is the responsibility of the subscriber to read and discard this value.
        /// </remarks>
        Task<ISnapshotTagValueSubscription> Subscribe(IAdapterCallContext context);

    }
}
