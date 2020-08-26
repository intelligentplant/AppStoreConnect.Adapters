using System;
using System.Linq;
using System.Reflection;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="IAdapterFeaturesCollection"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "String parameter might not always be a URI")]
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
        /// Gets a feature implementation by URI or name instead of type.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="featureUriOrName">
        ///   The feature URI or name. This must match either the URI of the feature, the <see cref="MemberInfo.Name"/> 
        ///   or <see cref="Type.FullName"/> of the feature type for standard adapter features, or 
        ///   the <see cref="Type.FullName"/> of the feature type for extension features.
        /// </param>
        /// <returns>
        ///   The feature implementation, or <see langword="null"/> if no matching feature was found.
        /// </returns>
        internal static object Get(this IAdapterFeaturesCollection features, string featureUriOrName) {
            if (features.TryGet(featureUriOrName, out var feature, out var _)) {
                return feature;
            }

            return default;
        }


        /// <summary>
        /// Gets a feature implementation by URI.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="featureUri">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   The feature implementation, or <see langword="null"/> if no matching feature was found.
        /// </returns>
        internal static object Get(this IAdapterFeaturesCollection features, Uri featureUri) {
            if (features.TryGet(featureUri, out var feature, out var _)) {
                return feature;
            }

            return default;
        }


        /// <summary>
        /// Tries to get the specified feature implementation by URI or name.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="uri">
        ///   The feature URI.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <param name="featureType">
        ///   The feature type that <paramref name="uri"/> was resolved to.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        internal static bool TryGet(this IAdapterFeaturesCollection features, Uri uri, out object feature, out Type featureType) {
            if (features == null || uri == null) {
                feature = null;
                featureType = null;
                return false;
            }

            var key = features.Keys.FirstOrDefault(x => {
                // Resolve via feature URI.
                var featureUri = x.GetAdapterFeatureUri();
                return featureUri != null && featureUri.Equals(uri);
            });

            feature = key == null 
                ? null 
                : features[key];

            if (feature == null) {
                featureType = null;
                return false;
            }

            featureType = key;
            return true;
        }


        /// <summary>
        /// Tries to get the specified feature implementation by URI or name.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="featureUriOrName">
        ///   The feature name. This must match either the URI for the feature, the <see cref="MemberInfo.Name"/> 
        ///   or <see cref="Type.FullName"/> of the feature type for standard adapter features, or 
        ///   the <see cref="Type.FullName"/> of the feature type for extension features.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <param name="featureType">
        ///   The feature type that <paramref name="featureUriOrName"/> was resolved to.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        internal static bool TryGet(this IAdapterFeaturesCollection features, string featureUriOrName, out object feature, out Type featureType) {
            if (features == null || string.IsNullOrWhiteSpace(featureUriOrName)) {
                feature = null;
                featureType = null;
                return false;
            }

            if (AdapterFeatureAttribute.TryCreateFeatureUriWithTrailingSlash(featureUriOrName, out var uri)) {
                return features.TryGet(uri, out feature, out featureType);
            }

            var key = features.Keys.FirstOrDefault(x => (x.Name.Equals(featureUriOrName, StringComparison.Ordinal) && x.IsStandardAdapterFeature()) || x.FullName.Equals(featureUriOrName, StringComparison.Ordinal));
            
            feature = key == null 
                ? null 
                : features[key];

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
        /// <param name="featureUriOrName">
        ///   The feature URI or name. This must match either the URI of the feature, the <see cref="MemberInfo.Name"/> 
        ///   or <see cref="Type.FullName"/> of the feature type for standard adapter features, or 
        ///   the <see cref="Type.FullName"/> of the feature type for extension features.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is defined in the collection, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool Contains(this IAdapterFeaturesCollection features, string featureUriOrName) {
            return features.TryGet(featureUriOrName, out var _, out var _);
        }


        /// <summary>
        /// Checks if the specified adapter feature is defined in the features collection.
        /// </summary>
        /// <param name="features">
        ///   The features collection.
        /// </param>
        /// <param name="featureUri">
        ///   The feature URI or name.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is defined in the collection, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool Contains(this IAdapterFeaturesCollection features, Uri featureUri) {
            return features.TryGet(featureUri, out var _, out var _);
        }

    }
}
