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
        /// Tries to get the specified feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool TryGet<TFeature>(
            this IAdapterFeaturesCollection features, 
            out TFeature feature
        ) where TFeature : IAdapterFeature {
            feature = features.Get<TFeature>();
            return feature != null;
        }


        /// <summary>
        /// Gets a feature implementation by name instead of type.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="featureName">
        ///   The feature name. This must match the <see cref="System.Reflection.MemberInfo.Name"/> 
        ///   or <see cref="Type.FullName"/> of the feature type for standard adapter features, or 
        ///   the <see cref="Type.FullName"/> of the feature type for extension features.
        /// </param>
        /// <returns>
        ///   The feature implementation, or <see langword="null"/> if no matching feature was found.
        /// </returns>
        internal static object Get(this IAdapterFeaturesCollection features, string featureName) {
            if (features.TryGet(featureName, out var feature, out var _)) {
                return feature;
            }

            return default;
        }


        /// <summary>
        /// Tries to get the specified feature implementation by name.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="featureName">
        ///   The feature name. This must match the <see cref="System.Reflection.MemberInfo.Name"/> 
        ///   or <see cref="Type.FullName"/> of the feature type for standard adapter features, or 
        ///   the <see cref="Type.FullName"/> of the feature type for extension features.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <param name="featureType">
        ///   The feature type that <paramref name="featureName"/> was resolved to.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        internal static bool TryGet(this IAdapterFeaturesCollection features, string featureName, out object feature, out Type featureType) {
            if (features == null || string.IsNullOrWhiteSpace(featureName)) {
                feature = null;
                featureType = null;
                return false;
            }

            var key = features.Keys.FirstOrDefault(x => (x.Name.Equals(featureName, StringComparison.Ordinal) && x.IsStandardAdapterFeature()) || x.FullName.Equals(featureName, StringComparison.Ordinal));
            feature = features[key];
            if (feature == null) {
                featureType = null;
                return false;
            }

            featureType = key;
            return true;
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
            return features.TryGet<TFeature>(out var _);
        }


        /// <summary>
        /// Checks if the specified adapter feature is defined in the features collection.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="featureName">
        ///   The feature name. This must match the <see cref="System.Reflection.MemberInfo.Name"/> 
        ///   or <see cref="Type.FullName"/> of the feature type for standard adapter features, or 
        ///   the <see cref="Type.FullName"/> of the feature type for extension features.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is defined in the collection, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool Contains(this IAdapterFeaturesCollection features, string featureName) {
            return features.TryGet(featureName, out var _, out var _);
        }

    }
}
