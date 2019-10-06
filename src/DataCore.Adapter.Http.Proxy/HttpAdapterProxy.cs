using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;
using DataCore.Adapter.Http.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataCore.Adapter.Http.Proxy {

    /// <summary>
    /// Adapter proxy that communicates with a remote adapter via SignalR.
    /// </summary>
    /// <remarks>
    ///   In order to apply per-call authorization to adapter calls, use the 
    ///   <see cref="AdapterHttpClient.CreateRequestTransformHandler"/> method to create a 
    ///   delegating handler that can set the appropriate authorization on outgoing requests, and 
    ///   add the handler to the pipeline for the <see cref="HttpClient"/> passed to the 
    ///   <see cref="HttpAdapterProxy.HttpAdapterProxy"/> constructor. The proxy will pass the 
    ///   <see cref="IAdapterCallContext.User"/> property from the adapter call to the delegating 
    ///   handler prior to sending each HTTP request.
    /// </remarks>
    public class HttpAdapterProxy : AdapterBase<HttpAdapterProxyOptions>, IAdapterProxy {

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        private readonly string _remoteAdapterId;

        /// <summary>
        /// Information about the remote host.
        /// </summary>
        private HostInfo _remoteHostInfo;

        /// <summary>
        /// The descriptor for the remote adapter.
        /// </summary>
        private AdapterDescriptorExtended _remoteDescriptor;

        /// <summary>
        /// Lock for accessing <see cref="_remoteDescriptor"/>.
        /// </summary>
        private readonly object _remoteInfoLock = new object();

        /// <inheritdoc/>
        public HostInfo RemoteHostInfo {
            get {
                lock (_remoteInfoLock) {
                    return _remoteHostInfo;
                }
            }
            private set {
                lock (_remoteInfoLock) {
                    _remoteHostInfo = value;
                }
            }
        }

        /// <inheritdoc/>
        public AdapterDescriptorExtended RemoteDescriptor {
            get {
                lock (_remoteInfoLock) {
                    return _remoteDescriptor;
                }
            }
            private set {
                lock (_remoteInfoLock) {
                    _remoteDescriptor = value;
                }
            }
        }

        /// <summary>
        /// A factory delegate for creating extension feature implementations.
        /// </summary>
        private readonly ExtensionFeatureFactory _extensionFeatureFactory;

        /// <summary>
        /// The client used in standard adapter queries.
        /// </summary>
        private readonly AdapterHttpClient _client;


        /// <summary>
        /// Creates a new <see cref="HttpAdapterProxy"/> object.
        /// </summary>
        /// <param name="client">
        ///   The Adapter HTTP client to use.
        /// </param>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="loggerFactory">
        ///   The logger factory for the proxy.
        /// </param>
        public HttpAdapterProxy(AdapterHttpClient client, IOptions<HttpAdapterProxyOptions> options, ILoggerFactory loggerFactory)
            : base(options, loggerFactory) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _remoteAdapterId = options?.Value?.RemoteId ?? throw new ArgumentException(Resources.Error_AdapterIdIsRequired, nameof(options));
            _extensionFeatureFactory = options?.Value?.ExtensionFeatureFactory;
        }


        /// <summary>
        /// Gets the proxy's <see cref="AdapterHttpClient"/>.
        /// </summary>
        /// <returns>
        ///   An <see cref="AdapterHttpClient"/> instance.
        /// </returns>
        public AdapterHttpClient GetClient() {
            return _client;
        }


        /// <summary>
        /// Initialises the proxy.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will perform the initialisation.
        /// </returns>
        private async Task Init(CancellationToken cancellationToken = default) {
            var client = GetClient();
            RemoteHostInfo = await client.HostInfo.GetHostInfoAsync(null, cancellationToken).ConfigureAwait(false);
            var descriptor = await client.Adapters.GetAdapterAsync(_remoteAdapterId, null, cancellationToken).ConfigureAwait(false);

            RemoteDescriptor = descriptor;

            ProxyAdapterFeature.AddFeaturesToProxy(this, descriptor.Features);

            if (_extensionFeatureFactory != null) {
                foreach (var extensionFeature in descriptor.Extensions) {
                    if (string.IsNullOrWhiteSpace(extensionFeature)) {
                        continue;
                    }

                    try {
                        var impl = _extensionFeatureFactory.Invoke(extensionFeature, this);
                        if (impl == null) {
                            Logger.LogWarning(Resources.Log_NoExtensionImplementationAvailable, extensionFeature);
                            continue;
                        }
                        AddFeatures(impl, addStandardFeatures: false);
                    }
                    catch (Exception e) {
                        Logger.LogError(e, Resources.Log_ExtensionFeatureRegistrationError, extensionFeature);
                    }
                }
            }
        }


        /// <inheritdoc/>
        protected override async Task StartAsync(CancellationToken cancellationToken) {
            await Init(cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override Task StopAsync(bool disposing, CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
