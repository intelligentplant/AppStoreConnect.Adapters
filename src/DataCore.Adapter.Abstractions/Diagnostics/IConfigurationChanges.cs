using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Feature that allows subscribers to be notified when configuration changes on items such as 
    /// tags and asset model nodes occur.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.Diagnostics.ConfigurationChanges,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_ConfigurationChanges),
        Description = nameof(AbstractionsResources.Description_ConfigurationChanges)
    )]
    public interface IConfigurationChanges : IAdapterFeature {

        /// <summary>
        /// Creates a configuration changes subscription.
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
        Task<ChannelReader<ConfigurationChange>> Subscribe(
            IAdapterCallContext context, 
            ConfigurationChangesSubscriptionRequest request,
            CancellationToken cancellationToken
        );

    }
}
