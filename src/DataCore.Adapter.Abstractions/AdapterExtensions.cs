using System;
using System.Linq;
using System.Reflection;

using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="IAdapter"/> and <see cref="AdapterDescriptorExtended"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "String parameter might not always be a URI")]
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        public static TFeature GetFeature<TFeature>(
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
        public static TFeature GetFeature<TFeature>(
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
        public static TFeature GetFeature<TFeature>(
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
        public static IAdapterFeature GetFeature(this IAdapter adapter, Uri uri) {
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
        public static IAdapterFeature GetFeature(this IAdapter adapter, string uriString) {
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
        public static IAdapterExtensionFeature GetExtensionFeature(this IAdapter adapter, Uri uri) {
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
        public static IAdapterExtensionFeature GetExtensionFeature(this IAdapter adapter, string uriString) {
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
            out TFeature feature
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
            out TFeature feature
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
            out TFeature feature
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
            out IAdapterFeature feature
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
            out IAdapterFeature feature
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
        public static bool TryGetExtensionFeature(
            this IAdapter adapter,
            Uri uri,
            out IAdapterExtensionFeature feature
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
        public static bool TryGetExtensionFeature(
            this IAdapter adapter,
            string uriString,
            out IAdapterExtensionFeature feature
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

            var standardFeatures = adapter
                .Features
                    ?.Keys
                    ?.Where(x => x.IsStandardFeatureUri())
                .ToArray() ?? Array.Empty<Uri>();

            var extensionFeatures = adapter
                .Features
                    ?.Keys
                    ?.Except(standardFeatures)
                .ToArray();

            return AdapterDescriptorExtended.Create(
                adapter.Descriptor.Id,
                adapter.Descriptor.Name,
                adapter.Descriptor.Description,
                standardFeatures.Where(x => x != null).Select(x => x.ToString()).OrderBy(x => x).ToArray(),
                extensionFeatures.Where(x => x != null).Select(x => x.ToString()).OrderBy(x => x).ToArray(),
                adapter.Properties,
                adapter.TypeDescriptor
            );
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

            return descriptor.Features.Any(f => string.Equals(f, uriString, StringComparison.Ordinal)) || descriptor.Extensions.Any(f => string.Equals(f, uriString, StringComparison.Ordinal));
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

            return descriptor.Features.Any(f => string.Equals(f, uriString, StringComparison.Ordinal)) || descriptor.Extensions.Any(f => string.Equals(f, uriString, StringComparison.Ordinal));
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
