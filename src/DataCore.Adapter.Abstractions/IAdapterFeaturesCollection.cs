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
        IEnumerable<Type> Keys { get; }

        /// <summary>
        /// Gets the specified adapter feature.
        /// </summary>
        /// <param name="key">
        ///   The feature type.
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        object this[Type key] { get; }
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers

    }
}
