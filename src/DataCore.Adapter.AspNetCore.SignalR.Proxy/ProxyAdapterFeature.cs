using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {

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
        private readonly SignalRAdapterProxy _proxy;

        /// <summary>
        /// The adapter ID for the remote adapter.
        /// </summary>
        protected string AdapterId {
            get { return _proxy.Descriptor.Id; }
        }


        /// <summary>
        /// Static constructor.
        /// </summary>
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
        /// <param name="features">
        ///   The proxy's features collection.
        /// </param>
        /// <param name="remoteAdapterFeatures">
        ///   The features supported by the remote adapter.
        /// </param>
        internal static void AddFeaturesToProxy(SignalRAdapterProxy proxy, AdapterFeaturesCollection features, IEnumerable<string> remoteAdapterFeatures) {
            foreach (var item in remoteAdapterFeatures) {
                var implementation = _featureImplementations.FirstOrDefault(x => x.Key.Name.Equals(item, StringComparison.OrdinalIgnoreCase));

                // .Key = adapter feature interface
                // .Value = implementation type

                if (implementation.Key == null) {
                    continue;
                }

                features.Add(implementation.Key, Activator.CreateInstance(implementation.Value, proxy));
            }
        }


        /// <summary>
        /// Creates a new <see cref="ProxyAdapterFeature"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the feature instance.
        /// </param>
        protected ProxyAdapterFeature(SignalRAdapterProxy proxy) {
            _proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
        }


        /// <summary>
        /// Gets a SignalR hub connection for the specified hub route.
        /// </summary>
        /// <param name="url">
        ///   The hub route.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A hub connection object. If an existing connection for the same hub already exists, the 
        ///   existing connection will be returned.
        /// </returns>
        protected internal async Task<HubConnection> GetHubConnection(string url, CancellationToken cancellationToken = default) {
            return await _proxy.GetOrCreateHubConnection(url, cancellationToken).ConfigureAwait(false);
        }

    }
}
