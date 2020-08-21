using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for subscribing to receive snapshot tag value changes from an adapter via a push 
    /// notification.
    /// </summary>
    [AdapterFeature(WellKnownFeatures.RealTimeData.SnapshotTagValuePush)]
    public interface ISnapshotTagValuePush : IAdapterFeature {

        /// <summary>
        /// Creates a snapshot value change subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   A request describing the subscription settings.
        /// </param>
        /// <returns>
        ///   A task that will create and start a subscription object that can be disposed once 
        ///   the subscription is no longer required.
        /// </returns>
        Task<ISnapshotTagValueSubscription> Subscribe(
            IAdapterCallContext context,
            CreateSnapshotTagValueSubscriptionRequest request
        );

    }
}
