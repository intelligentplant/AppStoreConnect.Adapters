using System;
using System.Linq;

using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="IAdapter"/> and <see cref="AdapterDescriptorExtended"/>.
    /// </summary>
    public static class AdapterExtensions {

        /// <summary>
        /// Gets the ID of the adapter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   The adapter ID.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        public static string GetId(this IAdapter adapter) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }

            return adapter.Descriptor.Id;
        }


        /// <summary>
        /// Gets the display name for the adapter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   The adapter display name.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        public static string GetName(this IAdapter adapter) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }

            return adapter.Descriptor.Name;
        }


        /// <summary>
        /// Gets the description for the adapter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   The adapter description.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        public static string? GetDescription(this IAdapter adapter) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }

            return adapter.Descriptor.Description;
        }


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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        public static TFeature? GetFeature<TFeature>(
            this IAdapter adapter
        ) where TFeature : IAdapterFeature {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            return adapter.Features.Get<TFeature>();
        }


        /// <summary>
        /// Gets the specified adapter feature cast to the specified type.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The type to cast the feature to.
        /// </typeparam>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uri">
        ///   The feature URI
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        public static TFeature? GetFeature<TFeature>(
            this IAdapter adapter,
            Uri uri
        ) where TFeature : IAdapterFeature {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }
            return adapter.Features.Get<TFeature>(uri);
        }


        /// <summary>
        /// Gets the specified adapter feature cast to the specified type.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The type to cast the feature to.
        /// </typeparam>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI string.
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        public static TFeature? GetFeature<TFeature>(
            this IAdapter adapter,
            string uriString
        ) where TFeature : IAdapterFeature {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }
            return adapter.Features.Get<TFeature>(uriString);
        }


        /// <summary>
        /// Gets the specified adapter feature.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uri">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterFeature? GetFeature(this IAdapter adapter, Uri uri) {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }

            return adapter.Features[uri];
        }


        /// <summary>
        /// Gets the specified adapter feature.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI string.
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        public static IAdapterFeature? GetFeature(this IAdapter adapter, string uriString) {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }
            return adapter.Features.Get(uriString);
        }


        /// <summary>
        /// Gets the specified extension adapter feature.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uri">
        ///   The feature URI. Can be relative to <see cref="WellKnownFeatures.Extensions.BaseUri"/>.
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
        public static IAdapterExtensionFeature? GetExtensionFeature(this IAdapter adapter, Uri uri) {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }

            return adapter.Features.GetExtension(uri);
        }


        /// <summary>
        /// Gets the specified extension adapter feature.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI string. Can be relative to <see cref="WellKnownFeatures.Extensions.BaseUri"/>.
        /// </param>
        /// <returns>
        ///   The implemented feature, or <see langword="null"/> if the adapter does not implement the 
        ///   feature.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
        public static IAdapterExtensionFeature? GetExtensionFeature(this IAdapter adapter, string uriString) {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }
            return adapter.Features.GetExtension(uriString);
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryGetFeature<TFeature>(
            this IAdapter adapter,
            out TFeature? feature
        ) where TFeature : IAdapterFeature {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            return adapter.Features.TryGet(out feature);
        }


        /// <summary>
        /// Tries to get the specified adapter feature cast to the specified type.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The type to cast the feature to.
        /// </typeparam>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uri">
        ///   The feature URI.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryGetFeature<TFeature>(
            this IAdapter adapter,
            Uri uri,
            out TFeature? feature
        ) where TFeature : IAdapterFeature {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }
            return adapter.Features.TryGet(uri, out feature);
        }


        /// <summary>
        /// Tries to get the specified adapter feature cast to the specified type.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The type to cast the feature to.
        /// </typeparam>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        public static bool TryGetFeature<TFeature>(
            this IAdapter adapter,
            string uriString,
            out TFeature? feature
        ) where TFeature : IAdapterFeature {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }
            return adapter.Features.TryGet(uriString, out feature);
        }


        /// <summary>
        /// Tries to get the specified adapter feature.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uri">
        ///   The feature URI string.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryGetFeature(
            this IAdapter adapter,
            Uri uri,
            out IAdapterFeature? feature
        ) {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }
            return adapter.Features.TryGet(uri, out feature);
        }


        /// <summary>
        /// Tries to get the specified adapter feature.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI string.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        public static bool TryGetFeature(
            this IAdapter adapter,
            string uriString,
            out IAdapterFeature? feature
        ) {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }
            return adapter.Features.TryGet(uriString, out feature);
        }


        /// <summary>
        /// Tries to get the specified extension adapter feature.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uri">
        ///   The feature URI string. Can be relative to <see cref="WellKnownFeatures.Extensions.BaseUri"/>.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
        public static bool TryGetExtensionFeature(
            this IAdapter adapter,
            Uri uri,
            out IAdapterExtensionFeature? feature
        ) {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }
            return adapter.Features.TryGetExtension(uri, out feature);
        }


        /// <summary>
        /// Tries to get the specified extension adapter feature.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI string. Can be relative to <see cref="WellKnownFeatures.Extensions.BaseUri"/>.
        /// </param>
        /// <param name="feature">
        ///   The implemented feature.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
        public static bool TryGetExtensionFeature(
            this IAdapter adapter,
            string uriString,
            out IAdapterExtensionFeature? feature
        ) {
            if (adapter?.Features == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }
            return adapter.Features.TryGetExtension(uriString, out feature);
        }


        /// <summary>
        /// Creates an <see cref="AdapterTypeDescriptor"/> for the <see cref="IAdapter"/>.
        /// </summary>
        /// <returns>
        ///   The <see cref="AdapterTypeDescriptor"/> for the adapter.
        /// </returns>
        public static AdapterTypeDescriptor CreateTypeDescriptor(this IAdapter adapter) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }

            var type = adapter.GetType();
            return type.CreateAdapterTypeDescriptor()!;
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
        /// <exception cref="AdapterDescriptorExtended">
        ///  <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        public static AdapterDescriptorExtended CreateExtendedAdapterDescriptor(this IAdapter adapter) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }

            var builder = new AdapterDescriptorBuilder(adapter.Descriptor)
                .WithTypeDescriptor(adapter.TypeDescriptor)
                .WithFeatures(adapter.Features.Keys)
                .WithProperties(adapter.Properties);

            return builder.Build();
        }


        /// <summary>
        /// Tests if the adapter contains the specified feature in its <see cref="IAdapter.Features"/> 
        /// collection.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uri">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is in the list, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool HasFeature(this IAdapter adapter, Uri uri) {
            if (adapter == null || uri == null) {
                return false;
            }
            return adapter?.Features?.Contains(uri) ?? false;
        }


        /// <summary>
        /// Tests if the descriptor contains the specified feature in its <see cref="AdapterDescriptorExtended.Features"/> 
        /// list.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <param name="uri">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is in the list, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool HasFeature(this AdapterDescriptorExtended descriptor, Uri uri) {
            if (descriptor == null || uri == null) {
                return false;
            }

            var uriString = uri.ToString();

#pragma warning disable CS0618 // Type or member is obsolete
            return descriptor.Features.Any(f => string.Equals(f, uriString, StringComparison.Ordinal)) || descriptor.Extensions.Any(f => string.Equals(f, uriString, StringComparison.Ordinal));
#pragma warning restore CS0618 // Type or member is obsolete
        }


        /// <summary>
        /// Tests if the adapter contains the specified feature in its <see cref="IAdapter.Features"/> 
        /// collection.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is in the list, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool HasFeature(this IAdapter adapter, string uriString) {
            if (adapter?.Features == null || string.IsNullOrEmpty(uriString)) {
                return false;
            }
            return adapter.Features.Contains(uriString);
        }


        /// <summary>
        /// Tests if the descriptor contains the specified feature in its <see cref="AdapterDescriptorExtended.Features"/> 
        /// list.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is in the list, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool HasFeature(this AdapterDescriptorExtended descriptor, string uriString) {
            if (descriptor == null || string.IsNullOrEmpty(uriString)) {
                return false;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            return descriptor.Features.Any(f => string.Equals(f, uriString, StringComparison.Ordinal)) || descriptor.Extensions.Any(f => string.Equals(f, uriString, StringComparison.Ordinal));
#pragma warning restore CS0618 // Type or member is obsolete
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
        public static bool HasFeature<TFeature>(this IAdapter adapter) {
            if (adapter?.Features == null) {
                return false;
            }

            var uri = typeof(TFeature).GetAdapterFeatureUri();
            if (uri == null) {
                return false;
            }

            return adapter.HasFeature(uri);
        }


        /// <summary>
        /// Tests if the descriptor contains the specified feature in its <see cref="AdapterDescriptorExtended.Features"/> 
        /// list.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="adapter">
        ///   The descriptor.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is in the list, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool HasFeature<TFeature>(this AdapterDescriptorExtended adapter) {
            if (adapter?.Features == null) {
                return false;
            }

            var uri = typeof(TFeature).GetAdapterFeatureUri();
            if (uri == null) {
                return false;
            }

            return adapter.HasFeature(uri);
        }

    }
}
