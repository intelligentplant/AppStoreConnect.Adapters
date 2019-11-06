using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using GrpcCore = Grpc.Core;

namespace DataCore.Adapter.Grpc.Proxy {

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
        private readonly GrpcAdapterProxy _proxy;

        /// <summary>
        /// Gets the logger for the proxy.
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
        internal static void AddFeaturesToProxy(GrpcAdapterProxy proxy, IEnumerable<string> remoteAdapterFeatures) {
            foreach (var item in remoteAdapterFeatures) {
                var implementation = _featureImplementations.FirstOrDefault(x => x.Key.Name.Equals(item, StringComparison.OrdinalIgnoreCase));

                // .Key = adapter feature interface
                // .Value = implementation type

                if (implementation.Key == null) {
                    continue;
                }

                proxy.AddFeature(implementation.Key, Activator.CreateInstance(implementation.Value, proxy));
            }
        }


        /// <summary>
        /// Creates a new <see cref="ProxyAdapterFeature"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the feature instance.
        /// </param>
        protected ProxyAdapterFeature(GrpcAdapterProxy proxy) {
            _proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
        }


        /// <summary>
        /// Creates a client for a gRPC service.
        /// </summary>
        /// <typeparam name="TClient">
        ///   The gRPC client type.
        /// </typeparam>
        /// <returns>
        ///   A new gRPC client instance.
        /// </returns>
        protected internal TClient CreateClient<TClient>() where TClient : GrpcCore.ClientBase<TClient> {
            return _proxy.CreateClient<TClient>();
        }


        /// <summary>
        /// Gets the gRPC call options for the specified adapter call context and cancellation token.
        /// </summary>
        /// <param name="context">
        ///   The adapter call context. If per-call credential options are configured on the proxy, 
        ///   call credentials will be added to the call options.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token to register with the call options.
        /// </param>
        /// <returns>
        ///   A new <see cref="GrpcCore.CallOptions"/> object.
        /// </returns>
        protected internal GrpcCore.CallOptions GetCallOptions(IAdapterCallContext context, CancellationToken cancellationToken) {
            return new GrpcCore.CallOptions(cancellationToken: cancellationToken, credentials: _proxy.GetCallCredentials(context));
        }

    }
}
