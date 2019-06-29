using System;
using System.Threading.Tasks;
using System.Threading;

//#if NETCOREAPP3_0
//using System.Net.Http;
//using GrpcNet = Grpc.Net;
//#endif

using GrpcCore = Grpc.Core;


namespace DataCore.Adapter.Grpc.Proxy {

    /// <summary>
    /// Adapter proxy that communicates with a remote adapter via gRPC.
    /// </summary>
    public class GrpcAdapterProxy : IAdapterProxy, IDisposable
#if NETCOREAPP3_0
        , 
        IAsyncDisposable
#endif
        {

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        private readonly string _adapterId;

        /// <summary>
        /// gRPC channel (when using Grpc.Core for HTTP/2 support).
        /// </summary>
        private readonly GrpcCore.Channel _channel;

//#if NETCOREAPP3_0
//        /// <summary>
//        /// HTTP client (when using native HTTP/2 support in .NET Core 3.0+).
//        /// </summary>
//        private readonly HttpClient _httpClient;
//#endif

        /// <inheritdoc />
        public Adapter.Common.Models.AdapterDescriptor Descriptor { get; private set; }

        /// <summary>
        /// Adapter features.
        /// </summary>
        private readonly AdapterFeaturesCollection _features = new AdapterFeaturesCollection();

        /// <inheritdoc />
        public IAdapterFeaturesCollection Features { get { return _features; } }


        /// <summary>
        /// Creates a new <see cref="GrpcAdapterProxy"/> using the specified gRPC channel.
        /// </summary>
        /// <param name="channel">
        ///   The channel.
        /// </param>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="options"/> does not define an adapter ID.
        /// </exception>
        private GrpcAdapterProxy(GrpcCore.Channel channel, GrpcAdapterProxyOptions options) {
            _adapterId = options?.AdapterId ?? throw new ArgumentException("Adapter ID is required.", nameof(options));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="options"/> does not define an adapter ID.
        /// </exception>
        public static async Task<GrpcAdapterProxy> Create(GrpcCore.Channel channel, GrpcAdapterProxyOptions options, CancellationToken cancellationToken = default) {
            var result = new GrpcAdapterProxy(channel, options);
            await result.Init(cancellationToken).ConfigureAwait(false);
            return result;
        }

//#if NETCOREAPP3_0

//        /// <summary>
//        /// Creates a new <see cref="GrpcAdapterProxy"/> using the specified HTTP client.
//        /// </summary>
//        /// <param name="httpClient">
//        ///   The HTTP client.
//        /// </param>
//        /// <param name="options">
//        ///   The proxy options.
//        /// </param>
//        /// <exception cref="ArgumentNullException">
//        ///   <paramref name="httpClient"/> is <see langword="null"/>.
//        /// </exception>
//        /// <exception cref="ArgumentException">
//        ///   <paramref name="options"/> does not define an adapter ID.
//        /// </exception>
//        private GrpcAdapterProxy(HttpClient httpClient, GrpcAdapterProxyOptions options) {
//            _adapterId = options?.AdapterId ?? throw new ArgumentException("Adapter ID is required.", nameof(options));
//            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
//        }


//        /// <summary>
//        /// Creates a new <see cref="GrpcAdapterProxy"/> using the specified HTTP client.
//        /// </summary>
//        /// <param name="httpClient">
//        ///   The HTTP client.
//        /// </param>
//        /// <param name="options">
//        ///   The proxy options.
//        /// </param>
//        /// <param name="cancellationToken">
//        ///   The cancellation token for the operation.
//        /// </param>
//        /// <exception cref="ArgumentNullException">
//        ///   <paramref name="httpClient"/> is <see langword="null"/>.
//        /// </exception>
//        /// <exception cref="ArgumentException">
//        ///   <paramref name="options"/> does not define an adapter ID.
//        /// </exception>
//        public static async Task<GrpcAdapterProxy> Create(HttpClient httpClient, GrpcAdapterProxyOptions options, CancellationToken cancellationToken = default) {
//            var result = new GrpcAdapterProxy(httpClient, options);
//            await result.Init(cancellationToken).ConfigureAwait(false);
//            return result;
//        }

//#endif


        /// <summary>
        /// Creates a client for a gRPC service.
        /// </summary>
        /// <typeparam name="TClient">
        ///   The gRPC client type.
        /// </typeparam>
        /// <returns>
        ///   A new gRPC client instance.
        /// </returns>
        internal TClient CreateClient<TClient>() where TClient : GrpcCore.ClientBase<TClient> {

//#if NETCOREAPP3_0
//            if (_httpClient != null) {
//                return GrpcNet.Client.GrpcClient.Create<TClient>(_httpClient);
//            }
//#endif

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
        }


        public async ValueTask DisposeAsync() {
            await _channel.ShutdownAsync().ConfigureAwait(false);
        }


        public void Dispose() {
            _channel.ShutdownAsync().Wait();
        }
    }
}
