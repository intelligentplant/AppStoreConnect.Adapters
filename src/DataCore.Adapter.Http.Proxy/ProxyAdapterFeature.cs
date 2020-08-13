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
    public abstract class ProxyAdapterFeature {

        /// <summary>
        /// Maps from feature interface type to the implementation type used for that feature.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Type> _featureImplementations;

        /// <summary>
        /// The proxy that the feature instance belongs to.
        /// </summary>
        private readonly HttpAdapterProxy _proxy;

        /// <summary>
        /// Gets the proxy's logger.
        /// </summary>
        protected ILogger Logger {
            get { return _proxy.Logger; }
        }

        /// <summary>
        /// The adapter ID for the remote adapter.
        /// </summary>
        protected string AdapterId {
            get { return _proxy.RemoteDescriptor?.Id; }
        }

        /// <summary>
        /// Gets the <see cref="IBackgroundTaskService"/> for the proxy.
        /// </summary>
        protected IBackgroundTaskService TaskScheduler {
            get { return _proxy.TaskScheduler; }
        }


        /// <summary>
        /// Static constructor.
        /// </summary>
#pragma warning disable CA1810 // Initialize reference type static fields inline
        static ProxyAdapterFeature() {
#pragma warning restore CA1810 // Initialize reference type static fields inline
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
        internal static void AddFeaturesToProxy(HttpAdapterProxy proxy, IEnumerable<string> remoteAdapterFeatures) {
            // Tracks feature instances as we go, in case the same type implements multiple 
            // features.
            var featureInstances = new Dictionary<Type, object>();

            foreach (var item in remoteAdapterFeatures) {
                var implementation = _featureImplementations.FirstOrDefault(x => x.Key.Name.Equals(item, StringComparison.OrdinalIgnoreCase));

                // .Key = adapter feature interface
                // .Value = implementation type

                if (implementation.Key == null) {
                    continue;
                }

                if (!featureInstances.TryGetValue(implementation.Value, out var feature)) {
                    feature = Activator.CreateInstance(implementation.Value, proxy);
                    featureInstances[implementation.Value] = feature;
                }

                proxy.AddFeature(implementation.Key, feature);
            }
        }


        /// <summary>
        /// Creates a new <see cref="ProxyAdapterFeature"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the feature instance.
        /// </param>
        protected ProxyAdapterFeature(HttpAdapterProxy proxy) {
            _proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
        }


        /// <summary>
        /// Gets the <see cref="AdapterHttpClient"/> used to query standard adapter features.
        /// </summary>
        /// <returns>
        ///   A <see cref="AdapterHttpClient"/> object.
        /// </returns>
        protected internal AdapterHttpClient GetClient() {
            return _proxy.GetClient();
        }

    }
}
