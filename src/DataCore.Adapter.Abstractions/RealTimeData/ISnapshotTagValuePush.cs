using System.Threading;

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
        ///   A subscription that will publish the received value changes.
        /// </returns>
        ISnapshotTagValueSubscription Subscribe(IAdapterCallContext context);

    }
}
