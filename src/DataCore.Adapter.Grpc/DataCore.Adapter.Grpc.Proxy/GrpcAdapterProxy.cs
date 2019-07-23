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
    public class GrpcAdapterProxy : IAdapterProxy, IDisposable
#if NETSTANDARD2_1
        , 
        IAsyncDisposable
#endif
        {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        private readonly string _adapterId;

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

        /// <inheritdoc />
        public Adapter.Common.Models.AdapterDescriptor Descriptor { get; private set; }

        /// <summary>
        /// Adapter features.
        /// </summary>
        private readonly AdapterFeaturesCollection _features = new AdapterFeaturesCollection();

        /// <inheritdoc />
        public IAdapterFeaturesCollection Features { get { return _features; } }

        /// <summary>
        /// A factory delegate for creating extension feature implementations.
        /// </summary>
        private readonly ExtensionFeatureFactory _extensionFeatureFactory;


        /// <summary>
        /// Creates a new <see cref="GrpcAdapterProxy"/> using the specified gRPC channel.
        /// </summary>
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
        private GrpcAdapterProxy(GrpcCore.Channel channel, GrpcAdapterProxyOptions options, ILogger<GrpcAdapterProxy> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _adapterId = options?.AdapterId ?? throw new ArgumentException("Adapter ID is required.", nameof(options));
            _getCallCredentials = options?.GetCallCredentials;
            _extensionFeatureFactory = options?.ExtensionFeatureFactory;
        }


        /// <summary>
        /// Creates a new <see cref="GrpcAdapterProxy"/> using the specified gRPC channel.
        /// </summary>
        /// <param name="channel">
        ///   The channel.
        /// </param>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="logger">
        ///   The logger for the proxy.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
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
        public static async Task<GrpcAdapterProxy> Create(GrpcCore.Channel channel, GrpcAdapterProxyOptions options, ILogger<GrpcAdapterProxy> logger, CancellationToken cancellationToken = default) {
            var result = new GrpcAdapterProxy(channel, options, logger);
            await result.StartAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }

#if NETSTANDARD2_1

        /// <summary>
        /// Creates a new <see cref="GrpcAdapterProxy"/> using the specified HTTP client.
        /// </summary>
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
        private GrpcAdapterProxy(HttpClient httpClient, GrpcAdapterProxyOptions options, ILogger<GrpcAdapterProxy> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _adapterId = options?.AdapterId ?? throw new ArgumentException("Adapter ID is required.", nameof(options));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _getCallCredentials = options?.GetCallCredentials;
        }


        /// <summary>
        /// Creates a new <see cref="GrpcAdapterProxy"/> using the specified HTTP client.
        /// </summary>
        /// <param name="httpClient">
        ///   The HTTP client.
        /// </param>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="logger">
        ///   The logger for the proxy.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
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
        public static async Task<GrpcAdapterProxy> Create(HttpClient httpClient, GrpcAdapterProxyOptions options, ILogger<GrpcAdapterProxy> logger, CancellationToken cancellationToken = default) {
            var result = new GrpcAdapterProxy(httpClient, options, logger);
            await result.Init(cancellationToken).ConfigureAwait(false);
            return result;
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
        private async Task Init(CancellationToken cancellationToken = default) {
            var client = CreateClient<AdaptersService.AdaptersServiceClient>();
            var response = await client.GetAdapterAsync(
                new GetAdapterRequest() {
                    AdapterId = _adapterId
                }, 
                cancellationToken: cancellationToken
            ).ResponseAsync.ConfigureAwait(false);

            Descriptor = new Adapter.Common.Models.AdapterDescriptor(
                response.Adapter.AdapterDescriptor.Id, 
                response.Adapter.AdapterDescriptor.Name, 
                response.Adapter.AdapterDescriptor.Description
            );
            ProxyAdapterFeature.AddFeaturesToProxy(this, _features, response.Adapter.Features);

            if (_extensionFeatureFactory != null) {
                foreach (var extensionFeature in response.Adapter.Extensions) {
                    if (string.IsNullOrWhiteSpace(extensionFeature)) {
                        continue;
                    }

                    try {
                        var impl = _extensionFeatureFactory.Invoke(extensionFeature, this);
                        if (impl == null) {
                            _logger.LogWarning(Resources.Log_NoExtensionImplementationAvailable, extensionFeature);
                            continue;
                        }

                        _features.AddFromProvider(impl, addStandardFeatures: false);
                    }
                    catch (Exception e) {
                        _logger.LogError(e, Resources.Log_ExtensionFeatureRegistrationError, extensionFeature);
                    }
                }
            }
        }


        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken = default) {
            await Init(cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken = default) {
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


        /// <summary>
        /// Disposes of the proxy.
        /// </summary>
        /// <returns>
        ///   A task that will shut down the proxy's channel.
        /// </returns>
        public async ValueTask DisposeAsync() {
            await StopAsync().ConfigureAwait(false);
            await _features.DisposeAsync().ConfigureAwait(false);
        }


        /// <summary>
        /// Disposes of the proxy.
        /// </summary>
        public void Dispose() {
            StopAsync().GetAwaiter().GetResult();
            _features.Dispose();
        }
    }
}
