using System;
using System.Collections.Generic;

namespace DataCore.Adapter {

    /// <summary>
    /// Provides a means of obtaining the features implemented by an adapter.
    /// </summary>
    public interface IAdapterFeaturesCollection {

        /// <summary>
        /// Gets the available features for the adapter.
        /// </summary>
        IEnumerable<Uri> Keys { get; }

        /// <summary>
        /// Gets the specified adapter feature.
        /// </summary>
        /// <param name="key">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1043:Use Integral Or String Argument For Indexers", Justification = "Features are identified by type")]
        IAdapterFeature this[Uri key] { get; }

    }
}
