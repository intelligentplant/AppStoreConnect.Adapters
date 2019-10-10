using System;
using System.Linq;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="IAdapterFeaturesCollection"/>.
    /// </summary>
    public static class AdapterFeaturesCollectionExtensions {

        /// <summary>
        /// Gets the specified adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        public static TFeature Get<TFeature>(this IAdapterFeaturesCollection features) where TFeature : IAdapterFeature {
            if (features == null) {
                return default;
            }

            return (TFeature) features[typeof(TFeature)];
        }


        /// <summary>
        /// Checks if the specified adapter feature is defined in the features collection.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The features type.
        /// </typeparam>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is defined in the collection, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool Contains<TFeature>(this IAdapterFeaturesCollection features) where TFeature : IAdapterFeature {
            if (features == null) {
                return false;
            }

            return features[typeof(TFeature)] != null;
        }


        /// <summary>
        /// Gets a feature implementation by name instead of type.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="featureName">
        ///   The feature name. This must match the <see cref="System.Reflection.MemberInfo.Name"/> 
        ///   or <see cref="Type.FullName"/> of the feature type.
        /// </param>
        /// <returns>
        ///   The feature implementation, or <see langword="null"/> if no matching feature was found.
        /// </returns>
        public static object GetByName(this IAdapterFeaturesCollection features, string featureName) {
            if (features == null || string.IsNullOrWhiteSpace(featureName)) {
                return null;
            }

            var key = features.Keys.FirstOrDefault(x => x.Name.Equals(featureName, StringComparison.Ordinal) || x.FullName.Equals(featureName, StringComparison.Ordinal));
            return key == null
                ? null
                : features[key];
        }

    }
}
