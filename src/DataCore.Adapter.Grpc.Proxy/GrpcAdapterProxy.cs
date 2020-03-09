using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Threading;

#if NETSTANDARD2_1
using GrpcNet = Grpc.Net;
#endif

using GrpcCore = Grpc.Core;
using DataCore.Adapter.Grpc.Client.Authentication;
using DataCore.Adapter.Common;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Grpc.Proxy {

    /// <summary>
    /// Adapter proxy that communicates with a remote adapter via gRPC.
    /// </summary>
    public class GrpcAdapterProxy : AdapterBase<GrpcAdapterProxyOptions>, IAdapterProxy {

        /// <summary>
        /// Gets the logger for the proxy.
        /// </summary>
        internal new ILogger Logger {
            get { return base.Logger; }
        }

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        private readonly string _remoteAdapterId;

        /// <summary>
        /// Information about the remote host.
        /// </summary>
        private Common.HostInfo _remoteHostInfo;

        /// <summary>
        /// The descriptor for the remote adapter.
        /// </summary>
        private AdapterDescriptorExtended _remoteDescriptor;

        /// <summary>
        /// Lock for accessing <see cref="_remoteHostInfo"/> and <see cref="_remoteDescriptor"/>.
        /// </summary>
        private readonly object _remoteInfoLock = new object();

        /// <inheritdoc/>
        public Common.HostInfo RemoteHostInfo {
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
        /// Gets per-call credentials.
        /// </summary>
        private readonly GetGrpcCallCredentials _getCallCredentials;

        /// <summary>
        /// gRPC channel (when using Grpc.Core for HTTP/2 support).
        /// </summary>
        private readonly GrpcCore.ChannelBase _channel;

        /// <summary>
        /// A factory delegate for creating extension feature implementations.
        /// </summary>
        private readonly ExtensionFeatureFactory _extensionFeatureFactory;


#if NETSTANDARD2_1

        /// <summary>
        /// Creates a new <see cref="GrpcAdapterProxy"/> using the specified <see cref="GrpcNet.Client.GrpcChannel"/>.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID. Specify <see langword="null"/> or white space to generate an ID 
        ///   automatically.
        /// </param>
        /// <param name="channel">
        ///   The channel.
        /// </param>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="taskScheduler">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The logger for the proxy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="options"/> does not define an adapter ID.
        /// </exception>
        public GrpcAdapterProxy(
            string id,
            GrpcNet.Client.GrpcChannel channel, 
            GrpcAdapterProxyOptions options, 
            IBackgroundTaskService taskScheduler, 
            ILogger<GrpcAdapterProxy> logger
        ) : base(
            id,
            options, 
            taskScheduler, 
            logger
        ) {
            _remoteAdapterId = Options?.RemoteId ?? throw new ArgumentException(Resources.Error_AdapterIdIsRequired, nameof(options));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _getCallCredentials = Options?.GetCallCredentials;
        }

#endif


        /// <summary>
        /// Creates a new <see cref="GrpcAdapterProxy"/> using the specified <see cref="GrpcCore.Channel"/>.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID. Specify <see langword="null"/> or white space to generate an ID 
        ///   automatically.
        /// </param>
        /// <param name="channel">
        ///   The channel.
        /// </param>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="taskScheduler">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The logger for the proxy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="options"/> does not define an adapter ID.
        /// </exception>
        public GrpcAdapterProxy(
            string id,
            GrpcCore.Channel channel, 
            GrpcAdapterProxyOptions options, 
            IBackgroundTaskService taskScheduler, 
            ILogger<GrpcAdapterProxy> logger
        ) : base(
            id,
            options, 
            taskScheduler, 
            logger
        ) {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _remoteAdapterId = Options?.RemoteId ?? throw new ArgumentException(Resources.Error_AdapterIdIsRequired, nameof(options));
            _getCallCredentials = Options?.GetCallCredentials;
            _extensionFeatureFactory = Options?.ExtensionFeatureFactory;
        }


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
            var callOptions = new GrpcCore.CallOptions(
                cancellationToken: cancellationToken,
                credentials: GetCallCredentials(null)
            );

            var hostInfoClient = CreateClient<HostInfoService.HostInfoServiceClient>();

            var getHostInfoResponse = await hostInfoClient.GetHostInfoAsync(
                new GetHostInfoRequest(),
                callOptions
            ).ResponseAsync.ConfigureAwait(false);

            RemoteHostInfo = getHostInfoResponse.HostInfo.ToAdapterHostInfo();

            var adapterClient = CreateClient<AdaptersService.AdaptersServiceClient>();

            var getAdapterResponse = await adapterClient.GetAdapterAsync(
                new GetAdapterRequest() {
                    AdapterId = _remoteAdapterId
                }, 
                callOptions
            ).ResponseAsync.ConfigureAwait(false);

            RemoteDescriptor = getAdapterResponse.Adapter.ToExtendedAdapterDescriptor();

            ProxyAdapterFeature.AddFeaturesToProxy(this, getAdapterResponse.Adapter.Features);

            if (_extensionFeatureFactory != null) {
                foreach (var extensionFeature in getAdapterResponse.Adapter.Extensions) {
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
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
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
        protected override async Task StopAsync(CancellationToken cancellationToken) {
            if (_channel is GrpcCore.Channel channel) {
                await channel.ShutdownAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing && _channel is GrpcCore.Channel channel) {
                Task.Run(() => channel.ShutdownAsync()).GetAwaiter().GetResult();
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask DisposeAsync(bool disposing) {
            await base.DisposeAsync(disposing).ConfigureAwait(false);
            if (disposing && _channel is GrpcCore.Channel channel) {
                await channel.ShutdownAsync().ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        protected override async Task<IEnumerable<Diagnostics.HealthCheckResult>> CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            var results = new List<Diagnostics.HealthCheckResult>(await base.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false));
            if (!IsRunning) {
                return results;
            }

            if (_channel is GrpcCore.Channel coreChannel) {
                var state = coreChannel.State;
                var description = string.Format(CultureInfo.CurrentCulture, Resources.HealthChecks_ChannelStateDescription, state.ToString());

                switch (state) {
                    case GrpcCore.ChannelState.Ready:
                        results.Add(Diagnostics.HealthCheckResult.Healthy(description));
                        break;
                    case GrpcCore.ChannelState.Shutdown:
                        results.Add(Diagnostics.HealthCheckResult.Degraded(description));
                        break;
                    default:
                        results.Add(Diagnostics.HealthCheckResult.Unhealthy(description));
                        break;
                }
            }

            // Grpc.Net channel doesn't expose a way of getting the channel state.

            return results;
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
                credentials.CopyToMetadataCollection(metadata);
            }));
        }

    }
}
