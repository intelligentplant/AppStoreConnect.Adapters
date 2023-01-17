using System;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Wrapper for an adapter feature registered with an <see cref="AdapterCore"/> instance.
    /// </summary>
    /// <remarks>
    ///   The wrapper provides validation and telemetry capabilities for the wrapped feature.
    /// </remarks>
    /// <seealso cref="AdapterFeatureWrapper{TFeature}"/>
    public abstract class AdapterFeatureWrapper : IAdapterFeature {

        /// <summary>
        /// The <see cref="AdapterCore"/> that the wrapper is assigned to.
        /// </summary>
        internal AdapterCore Adapter { get; }

        /// <summary>
        /// The feature that is wrapped by the <see cref="AdapterFeatureWrapper"/>.
        /// </summary>
        internal IAdapterFeature InnerFeature { get; }

        /// <inheritdoc/>
        IBackgroundTaskService IBackgroundTaskServiceProvider.BackgroundTaskService { get { return InnerFeature.BackgroundTaskService; } }


        /// <summary>
        /// Creates a new <see cref="AdapterFeatureWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The <see cref="AdapterCore"/> for the feature.
        /// </param>
        /// <param name="innerFeature">
        ///   The inner feature to wrap.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="innerFeature"/> is <see langword="null"/>.
        /// </exception>
        internal AdapterFeatureWrapper(AdapterCore adapter, IAdapterFeature innerFeature) {
            Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            InnerFeature = innerFeature ?? throw new ArgumentNullException(nameof(innerFeature));
        }

    }
}
