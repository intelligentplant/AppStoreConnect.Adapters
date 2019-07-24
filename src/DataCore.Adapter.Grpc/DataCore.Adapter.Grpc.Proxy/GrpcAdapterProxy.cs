using System;
using System.Threading.Tasks;
using System.Threading;

#if NETSTANDARD2_1
using System.Net.Http;
using GrpcNet = Grpc.Net;
#endif

using GrpcCore = Grpc.Core;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Grpc.Proxy {

    /// <summary>
    /// Adapter proxy that communicates with a remote adapter via gRPC.
    /// </summary>
    public class GrpcAdapterProxy : AdapterBase, IAdapterProxy {

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        private readonly string _remoteAdapterId;

        /// <summary>
        /// The descriptor for the remote adapter.
        /// </summary>
        private Adapter.Common.Models.AdapterDescriptor _remoteDescriptor;

        /// <summary>
        /// Lock for accessing <see cref="_remoteDescriptor"/>.
        /// </summary>
        private readonly object _remoteDescriptorLock = new object();

        /// <inheritdoc/>
        public Adapter.Common.Models.AdapterDescriptor RemoteDescriptor {
            get {
                lock (_remoteDescriptorLock) {
                    return _remoteDescriptor;
                }
            }
            private set {
                lock (_remoteDescriptorLock) {
                    _remoteDescriptor = value;
                }
            }
        }

        /// <summary>
        /// Gets per-call credentials.
        /// </summary>
        private readonly GetGrpcCallCredentials _getCallCredentials;

        /// <summary>
        /// gRPC channel (when using Grpc.Core for HTTP/2 support).
        /// </summary>
        private readonly GrpcCore.Channel _channel;

#if NETSTANDARD2_1
        /// <summary>
        /// HTTP client (when using native HTTP/2 support in .NET Core 3.0+).
        /// </summary>
        private readonly HttpClient _httpClient;
#endif

        /// <summary>
        /// A factory delegate for creating extension feature implementations.
        /// </summary>
        private readonly ExtensionFeatureFactory _extensionFeatureFactory;


        /// <summary>
        /// Creates a new <see cref="GrpcAdapterProxy"/> using the specified gRPC channel.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor for the local proxy.
        /// <param name="channel">
        ///   The channel.
        /// </param>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="logger">
        ///   The logger for the proxy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="options"/> does not define an adapter ID.
        /// </exception>
        public GrpcAdapterProxy(Adapter.Common.Models.AdapterDescriptor descriptor, GrpcCore.Channel channel, GrpcAdapterProxyOptions options, ILogger<GrpcAdapterProxy> logger)
            : base(descriptor, logger) {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _remoteAdapterId = options?.AdapterId ?? throw new ArgumentException("Adapter ID is required.", nameof(options));
            _getCallCredentials = options?.GetCallCredentials;
            _extensionFeatureFactory = options?.ExtensionFeatureFactory;
        }

#if NETSTANDARD2_1

        /// <summary>
        /// Creates a new <see cref="GrpcAdapterProxy"/> using the specified HTTP client.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor for the local proxy.
        /// </param>
        /// <param name="httpClient">
        ///   The HTTP client.
        /// </param>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="logger">
        ///   The logger for the proxy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="httpClient"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="options"/> does not define an adapter ID.
        /// </exception>
        public GrpcAdapterProxy(Adapter.Common.Models.AdapterDescriptor descriptor, HttpClient httpClient, GrpcAdapterProxyOptions options, ILogger<GrpcAdapterProxy> logger) 
            : base(descriptor, logger) {
            _remoteAdapterId = options?.AdapterId ?? throw new ArgumentException("Adapter ID is required.", nameof(options));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _getCallCredentials = options?.GetCallCredentials;
        }

#endif


        /// <summary>
        /// Creates a client for a gRPC service using the proxy's gRPC channel.
        /// </summary>
        /// <typeparam name="TClient">
        ///   The gRPC client type.
        /// </typeparam>
        /// <returns>
        ///   A new gRPC client instance.
        /// </returns>
        public TClient CreateClient<TClient>() where TClient : GrpcCore.ClientBase<TClient> {

#if NETSTANDARD2_1
            if (_httpClient != null) {
                return GrpcNet.Client.GrpcClient.Create<TClient>(_httpClient);
            }
#endif

            return (TClient) Activator.CreateInstance(typeof(TClient), _channel);
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
        private async Task Init(CancellationToken cancellationToken) {
            var client = CreateClient<AdaptersService.AdaptersServiceClient>();
            var response = await client.GetAdapterAsync(
                new GetAdapterRequest() {
                    AdapterId = _remoteAdapterId
                }, 
                cancellationToken: cancellationToken
            ).ResponseAsync.ConfigureAwait(false);

            RemoteDescriptor = new Adapter.Common.Models.AdapterDescriptor(
                response.Adapter.AdapterDescriptor.Id,
                response.Adapter.AdapterDescriptor.Name,
                response.Adapter.AdapterDescriptor.Description
            );

            ProxyAdapterFeature.AddFeaturesToProxy(this, response.Adapter.Features);

            if (_extensionFeatureFactory != null) {
                foreach (var extensionFeature in response.Adapter.Extensions) {
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
        protected override async Task StopAsync(bool disposing, CancellationToken cancellationToken) {
            if (_channel != null) {
                await _channel.ShutdownAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Gets per-call gRPC credentials for the specified adapter call context.
        /// </summary>
        /// <param name="context">
        ///   The adapter call context.
        /// </param>
        /// <returns>
        ///   The call credentials to use. If <paramref name="context"/> is <see langword="null"/> 
        ///   or no <see cref="GrpcAdapterProxyOptions.GetCallCredentials"/> delegate was supplied 
        ///   when creating the proxy, the result will be <see langword="null"/>.
        /// </returns>
        public GrpcCore.CallCredentials GetCallCredentials(IAdapterCallContext context) {
            if (_getCallCredentials == null) {
                return null;
            }

            return GrpcCore.CallCredentials.FromInterceptor(new GrpcCore.AsyncAuthInterceptor(async (authContext, metadata) => {
                var credentials = await _getCallCredentials(context).ConfigureAwait(false);
                if (credentials == null) {
                    return;
                }

                metadata.Add(credentials.ToMetadataEntry());
            }));
        }

    }
}
