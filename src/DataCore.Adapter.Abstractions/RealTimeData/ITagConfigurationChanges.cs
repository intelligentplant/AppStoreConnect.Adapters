using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature that allows subscribers to be notified when tag configuration changes occur.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.RealTimeData.TagConfigurationChanges,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_TagConfigurationChanges),
        Description = nameof(AbstractionsResources.Description_TagConfigurationChanges)
    )]
    public interface ITagConfigurationChanges : IAdapterFeature {

        /// <summary>
        /// Creates a tag configuration changes subscription.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   A request describing the subscription settings.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the subscription.
        /// </param>
        /// <returns>
        ///   A channel reader that will emit configuration changes as they occur.
        /// </returns>
        Task<ChannelReader<TagConfigurationChange>> Subscribe(
            IAdapterCallContext context, 
            TagConfigurationChangesSubscriptionRequest request,
            CancellationToken cancellationToken
        );

    }
}
