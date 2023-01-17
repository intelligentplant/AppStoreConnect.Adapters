using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataCore.Adapter.Http.Client;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Http.Proxy {
    /// <summary>
    /// Base class that adapter feature implementations inherit from.
    /// </summary>
    /// <remarks>
    ///   The <see cref="ProxyAdapterFeature"/> class defines a static constructor that scans the 
    ///   assembly for implementations of adapter features. This allows feature implementations to 
    ///   be dynamically instantiated based on what the remote adapter supports.
    /// </remarks>
    public abstract class ProxyAdapterFeature : IBackgroundTaskServiceProvider {

        /// <summary>
        /// Maps from feature interface type to the implementation type used for that feature.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Type> _featureImplementations;

        /// <summary>
        /// The proxy that the feature instance belongs to.
        /// </summary>
        protected HttpAdapterProxy Proxy { get; }

        /// <summary>
        /// Gets the proxy's logger.
        /// </summary>
        protected ILogger Logger {
            get { return Proxy.Logger; }
        }

        /// <summary>
        /// The adapter ID for the remote adapter.
        /// </summary>
        protected string AdapterId {
            get { return Proxy.RemoteDescriptor?.Id!; }
        }

        /// <summary>
        /// Gets the <see cref="IBackgroundTaskService"/> for the proxy.
        /// </summary>
        public IBackgroundTaskService BackgroundTaskService {
            get { return Proxy.BackgroundTaskService; }
        }



        /// <summary>
        /// Static constructor.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Initialisation is non-trivial")]
        static ProxyAdapterFeature() {
            _featureImplementations = new ConcurrentDictionary<Type, Type>();

            var featureTypes = TypeExtensions.GetStandardAdapterFeatureTypes();
            var possibleImplementationTypes = typeof(ProxyAdapterFeature)
                .Assembly
                .GetTypes()
                .Where(x => x != typeof(ProxyAdapterFeature))
                .Where(x => typeof(ProxyAdapterFeature).IsAssignableFrom(x));

            foreach (var type in possibleImplementationTypes) {
                var implementedFeatures = featureTypes.Where(x => x.IsAssignableFrom(type));
                foreach (var implementedFeature in implementedFeatures) {
                    _featureImplementations[implementedFeature] = type;
                }
            }
        }


        /// <summary>
        /// Adds feature implementations to a proxy based on the features implemented in the remote 
        /// adapter.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy.
        /// </param>
        /// <param name="remoteAdapterFeatures">
        ///   The features supported by the remote adapter.
        /// </param>
        /// <param name="predicate">
        ///   A predicate that allows features to be omitted on a case-by-case basis.
        /// </param>
        internal static void AddFeaturesToProxy(HttpAdapterProxy proxy, IEnumerable<string> remoteAdapterFeatures, Func<Type, bool>? predicate = null) {
            // Tracks feature instances as we go, in case the same type implements multiple 
            // features.
            var featureInstances = new Dictionary<Type, object>();

            foreach (var featureUriOrName in remoteAdapterFeatures) { 
                var implementation = featureUriOrName.TryCreateUriWithTrailingSlash(out var uri)
                    ? _featureImplementations.FirstOrDefault(x => x.Key.HasAdapterFeatureUri(uri!))
                    : _featureImplementations.FirstOrDefault(x => x.Key.Name.Equals(featureUriOrName, StringComparison.OrdinalIgnoreCase));

                // .Key = adapter feature interface
                // .Value = implementation type

                if (implementation.Key == null || !(predicate?.Invoke(implementation.Key) ?? true)) {
                    continue;
                }

                if (!featureInstances.TryGetValue(implementation.Value, out var feature)) {
                    feature = Activator.CreateInstance(implementation.Value, proxy);
                    featureInstances[implementation.Value] = feature!;
                }

                proxy.AddFeature(implementation.Key, (IAdapterFeature) feature!);
            }
        }


        /// <summary>
        /// Creates a new <see cref="ProxyAdapterFeature"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the feature instance.
        /// </param>
        protected ProxyAdapterFeature(HttpAdapterProxy proxy) {
            Proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
        }


        /// <summary>
        /// Gets the <see cref="AdapterHttpClient"/> used to query standard adapter features.
        /// </summary>
        /// <returns>
        ///   A <see cref="AdapterHttpClient"/> object.
        /// </returns>
        protected internal AdapterHttpClient GetClient() {
            return Proxy.GetClient();
        }


        /// <summary>
        /// Gets the <see cref="SignalRClientWrapper"/> to use for SignalR-specific functionality.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller
        /// </param>
        /// <returns>
        ///   The <see cref="SignalRClientWrapper"/> for the calling <paramref name="context"/>.
        /// </returns>
        internal SignalRClientWrapper GetSignalRClient(IAdapterCallContext context) => Proxy.GetSignalRClient(context);

    }
}
