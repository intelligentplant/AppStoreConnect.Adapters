using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter {

    /// <summary>
    /// Provides a means of obtaining the features implemented by an adapter.
    /// </summary>
    public interface IAdapterFeaturesCollection {

        /// <summary>
        /// Gets the available features for the adapter.
        /// </summary>
        IEnumerable<Type> Keys { get; }

        /// <summary>
        /// Gets the specified adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        TFeature Get<TFeature>() where TFeature : IAdapterFeature;

    }
}
