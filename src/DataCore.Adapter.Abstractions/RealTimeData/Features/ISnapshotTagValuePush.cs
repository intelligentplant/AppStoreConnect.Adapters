using System.Threading;
using System.Threading.Tasks;

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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A subscription object that can be disposed once the subscription is no longer required.
        /// </returns>
        Task<ISnapshotTagValueSubscription> Subscribe(IAdapterCallContext context, CancellationToken cancellationToken);

    }
}
