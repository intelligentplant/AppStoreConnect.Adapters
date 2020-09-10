using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for subscribing to receive snapshot tag value changes from an adapter via a push 
    /// notification.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.RealTimeData.SnapshotTagValuePush,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_SnapshotTagValuePush),
        Description = nameof(AbstractionsResources.Description_SnapshotTagValuePush)
    )]
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
        /// <param name="channel">
        ///   A channel that will add tags to or remove tags from the subscription.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the subscription.
        /// </param>
        /// <returns>
        ///   A channel reader that will emit tag values as they occur.
        /// </returns>
        Task<ChannelReader<TagValueQueryResult>> Subscribe(
            IAdapterCallContext context,
            CreateSnapshotTagValueSubscriptionRequest request,
            ChannelReader<TagValueSubscriptionUpdate> channel,
            CancellationToken cancellationToken
        );

    }
}
