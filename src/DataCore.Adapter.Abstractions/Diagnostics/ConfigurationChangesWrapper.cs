using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Wrapper for <see cref="IConfigurationChanges"/>.
    /// </summary>
    internal class ConfigurationChangesWrapper : AdapterFeatureWrapper<IConfigurationChanges>, IConfigurationChanges {

        /// <summary>
        /// Creates a new <see cref="ConfigurationChangesWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal ConfigurationChangesWrapper(AdapterCore adapter, IConfigurationChanges innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<ConfigurationChange> IConfigurationChanges.Subscribe(IAdapterCallContext context, ConfigurationChangesSubscriptionRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.Subscribe, cancellationToken);
        }

    }
}
