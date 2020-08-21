using System;
using System.Linq;
using DataCore.Adapter.Common;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="IAdapter"/> and <see cref="AdapterDescriptorExtended"/>.
    /// </summary>
    public static class AdapterExtensions {

        /// <summary>
        /// Gets the specified adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        public static TFeature GetFeature<TFeature>(this IAdapter adapter) where TFeature : IAdapterFeature {
            if (adapter?.Features == null) {
                return default;
            }
            return adapter.Features.Get<TFeature>();
        }


        /// <summary>
        /// Gets the specified adapter feature.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="featureName">
        ///   The feature name.
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        internal static object GetFeature(this IAdapter adapter, string featureName) {
            if (adapter?.Features == null) {
                return default;
            }
            return adapter.Features.Get(featureName);
        }


        /// <summary>
        /// Tries to get the specified adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool TryGetFeature<TFeature>(
            this IAdapter adapter,
            out TFeature feature
        ) where TFeature : IAdapterFeature {
            if (adapter?.Features == null) {
                feature = default;
                return false;
            }
            return adapter.Features.TryGet(out feature);
        }


        /// <summary>
        /// Tries to get the specified adapter feature implementation by name.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
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
        internal static bool TryGetFeature(
            this IAdapter adapter,
            string featureName,
            out object feature,
            out Type featureType
        ) {
            if (adapter?.Features == null) {
                feature = default;
                featureType = default;
                return false;
            }
            return adapter.Features.TryGet(featureName, out feature, out featureType);
        }


        /// <summary>
        /// Creates an <see cref="AdapterDescriptorExtended"/> for the <see cref="IAdapter"/>.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorExtended"/> for the adapter.
        /// </returns>
        public static AdapterDescriptorExtended CreateExtendedAdapterDescriptor(this IAdapter adapter) {
            if (adapter == null) {
                return null;
            }

            var standardFeatures = adapter
                .Features
                    ?.Keys
                    ?.Where(x => x.IsStandardAdapterFeature())
                .ToArray() ?? Array.Empty<Type>();

            var extensionFeatures = adapter
                .Features
                    ?.Keys
                    ?.Except(standardFeatures)
                .ToArray();

            return AdapterDescriptorExtended.Create(
                adapter.Descriptor.Id,
                adapter.Descriptor.Name,
                adapter.Descriptor.Description,
                standardFeatures.OrderBy(x => x.Name).Select(x => x.Name).ToArray(),
                extensionFeatures.OrderBy(x => x.FullName).Select(x => x.FullName).ToArray(),
                adapter.Properties
            );
        }


        /// <summary>
        /// Tests if the adapter contains the specified feature in its <see cref="IAdapter.Features"/> 
        /// collection.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is in the list, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool HasFeature<TFeature>(this IAdapter adapter) where TFeature : IAdapterFeature {
            return adapter?.Features?.Contains<TFeature>() ?? false;
        }


        /// <summary>
        /// Tests if the adapter contains the specified feature in its <see cref="IAdapter.Features"/> 
        /// collection.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="featureName">
        ///   The feature name.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is in the list, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool HasFeature(this IAdapter adapter, string featureName) {
            return adapter?.Features?.Contains(featureName) ?? false;
        }


        /// <summary>
        /// Tests if the descriptor contains the specified feature in its <see cref="AdapterDescriptorExtended.Features"/> 
        /// list.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is in the list, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool HasFeature<TFeature>(this AdapterDescriptorExtended descriptor) where TFeature : IAdapterFeature {
            if (descriptor == null) {
                return false;
            }

            return typeof(TFeature).IsExtensionAdapterFeature()
                ? descriptor.HasFeature(typeof(TFeature).FullName)
                : descriptor.HasFeature(typeof(TFeature).Name);
        }


        /// <summary>
        /// Tests if the descriptor contains the specified feature in its <see cref="AdapterDescriptorExtended.Features"/> 
        /// list.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <param name="featureName">
        ///   The feature name.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is in the list, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool HasFeature(this AdapterDescriptorExtended descriptor, string featureName) {
            if (descriptor == null || string.IsNullOrWhiteSpace(featureName)) {
                return false;
            }

            return descriptor.Features.Any(f => string.Equals(f, featureName, StringComparison.Ordinal)) || descriptor.Extensions.Any(f => string.Equals(f, featureName, StringComparison.Ordinal));
        }

    }
}
