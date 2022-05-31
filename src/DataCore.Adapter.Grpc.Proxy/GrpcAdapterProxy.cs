using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using GrpcCore = Grpc.Core;
using Grpc.Core.Interceptors;

#if NETFRAMEWORK == false
using GrpcNet = Grpc.Net;
#endif

using DataCore.Adapter.Grpc.Client.Authentication;
using DataCore.Adapter.Common;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Proxy;


namespace DataCore.Adapter.Grpc.Proxy {

    /// <summary>
    /// Adapter proxy that communicates with a remote adapter via gRPC.
    /// </summary>
    [AdapterMetadata(
        "https://www.intelligentplant.com/app-store-connect/adapters/proxies/grpc",
        ResourceType = typeof(Resources),
        Name = nameof(Resources.AdapterMetadata_DisplayName),
        Description = nameof(Resources.AdapterMetadata_Description)
    )]
    public class GrpcAdapterProxy : AdapterBase<GrpcAdapterProxyOptions>, IAdapterProxy {

        /// <summary>
        /// Gets the logger for the proxy.
        /// </summary>
        internal new ILogger Logger {
            get { return base.Logger; }
        }

        /// <summary>
        /// The <see cref="IObjectEncoder"/> instances to use when sending or receiving 
        /// extension objects.
        /// </summary>
        internal IEnumerable<IObjectEncoder> Encoders { get; }

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        private readonly string _remoteAdapterId;

        /// <summary>
        /// Information about the remote host.
        /// </summary>
        private Common.HostInfo _remoteHostInfo = default!;

        /// <summary>
        /// The descriptor for the remote adapter.
        /// </summary>
        private AdapterDescriptorExtended _remoteDescriptor = default!;

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
        private readonly GetGrpcCallCredentials? _getCallCredentials;

        /// <summary>
        /// gRPC channel.
        /// </summary>
        private readonly GrpcCore.ChannelBase _channel;

        /// <summary>
        /// When <see langword="true"/>, the <see cref="_channel"/> will be shut down when the 
        /// adapter is disposed.
        /// </summary>
        private readonly bool _closeChannelOnDispose;

        /// <summary>
        /// A factory delegate for creating extension feature implementations.
        /// </summary>
        private readonly ExtensionFeatureFactory<GrpcAdapterProxy>? _extensionFeatureFactory;


#if NETFRAMEWORK == false

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
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances to use when sending or receiving 
        ///   extension objects.
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
            IBackgroundTaskService? taskScheduler, 
            IEnumerable<IObjectEncoder> encoders,
            ILogger<GrpcAdapterProxy>? logger
        ) : base(
            id,
            options, 
            taskScheduler, 
            logger
        ) {
            Encoders = encoders?.ToArray() ?? throw new ArgumentNullException(nameof(encoders));
            _remoteAdapterId = Options?.RemoteId ?? throw new ArgumentException(Resources.Error_AdapterIdIsRequired, nameof(options));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _getCallCredentials = Options?.GetCallCredentials;
            _extensionFeatureFactory = Options?.ExtensionFeatureFactory;
            _closeChannelOnDispose = Options?.CloseChannelOnDispose ?? false;
        }

#else

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
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances to use when sending or receiving 
        ///   extension objects.
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
            IEnumerable<IObjectEncoder> encoders,
            ILogger<GrpcAdapterProxy> logger
        ) : base(
            id,
            options, 
            taskScheduler, 
            logger
        ) {
            Encoders = encoders?.ToArray() ?? throw new ArgumentNullException(nameof(encoders));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _remoteAdapterId = Options?.RemoteId ?? throw new ArgumentException(Resources.Error_AdapterIdIsRequired, nameof(options));
            _getCallCredentials = Options?.GetCallCredentials;
            _extensionFeatureFactory = Options?.ExtensionFeatureFactory;
            _closeChannelOnDispose = Options?.CloseChannelOnDispose ?? false;
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
            var interceptors = Options.GetClientInterceptors?.Invoke()?.ToArray();
            if (interceptors?.Length > 0) {
                return (TClient) Activator.CreateInstance(typeof(TClient), _channel.Intercept(interceptors))!;
            }

            return (TClient) Activator.CreateInstance(typeof(TClient), _channel)!;
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
                credentials: GetCallCredentials(new DefaultAdapterCallContext())
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

            foreach (var extensionFeature in getAdapterResponse.Adapter.Extensions) {
                if (string.IsNullOrWhiteSpace(extensionFeature)) {
                    continue;
                }

                try {
                    var impl = _extensionFeatureFactory?.Invoke(extensionFeature, this);
                    if (impl == null) {
                        if (!extensionFeature.TryCreateUriWithTrailingSlash(out var featureUri)) {
                            Logger.LogWarning(Resources.Log_NoExtensionImplementationAvailable, extensionFeature);
                            continue;
                        }

                        impl = ExtensionFeatureProxyGenerator.CreateExtensionFeatureProxy<GrpcAdapterProxy, GrpcAdapterProxyOptions, Extensions.AdapterExtensionFeatureImpl>(
                            this,
                            featureUri!
                        );
                    }
                    AddFeatures(impl, addStandardFeatures: false);
                }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_ExtensionFeatureRegistrationError, extensionFeature);
                }
            }

            if (RemoteDescriptor.HasFeature<IHealthCheck>()) {
                // Adapter supports health check subscriptions.
                BackgroundTaskService.QueueBackgroundWorkItem(RunRemoteHealthSubscription);
            }
        }


        /// <inheritdoc/>
        protected override async Task StartAsync(CancellationToken cancellationToken) {
            await Init(cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing && _closeChannelOnDispose) {
                Task.Run(async () => {
                    try {
                        await _channel.ShutdownAsync().ConfigureAwait(false);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                        Logger.LogError(e, Resources.Log_ChannelShutdownError);
                    }
                }).GetAwaiter().GetResult();
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask DisposeAsyncCore() {
            await base.DisposeAsyncCore().ConfigureAwait(false);
            if (_closeChannelOnDispose) {
                try {
                    await _channel.ShutdownAsync().ConfigureAwait(false);
                }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_ChannelShutdownError);
                }
            }
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
        internal GrpcCore.CallOptions GetCallOptions(IAdapterCallContext context, CancellationToken cancellationToken) {
            var headers = new GrpcCore.Metadata();
            if (!string.IsNullOrWhiteSpace(context?.CorrelationId)) {
                // We have a correlation ID for the context; use it on the outgoing call as 
                // well.
                headers.Add("Request-Id", context!.CorrelationId);
            }

            return new GrpcCore.CallOptions(
                cancellationToken: cancellationToken,
                credentials: GetCallCredentials(context!),
                headers: headers
            );
        }


        /// <summary>
        /// Long-running task that tells the adapter to recompute the overall health status of the 
        /// adapter when the remote adapter health status changes.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token that will fire when the task should end.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will monitor for changes in the remote adapter health.
        /// </returns>
        private async Task RunRemoteHealthSubscription(CancellationToken cancellationToken) {
            var client = CreateClient<AdaptersService.AdaptersServiceClient>();
            var callOptions = GetCallOptions(new DefaultAdapterCallContext(), cancellationToken);

            var healthCheckStream = client.CreateAdapterHealthPushChannel(new CreateAdapterHealthPushChannelRequest() { 
                AdapterId = _remoteAdapterId
            }, callOptions);

            while (await healthCheckStream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                OnHealthStatusChanged();
            }
        }


        /// <summary>
        /// Checks the health of the remote adapter.
        /// </summary>
        /// <param name="context">
        ///   The context for the caller.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the health check result.
        /// </returns>
        private async Task<Diagnostics.HealthCheckResult> CheckRemoteHealthAsync(
            IAdapterCallContext context, 
            CancellationToken cancellationToken
        ) {
            if (!RemoteDescriptor.HasFeature<Diagnostics.IHealthCheck>()) {
                return Diagnostics.HealthCheckResult.Healthy(
                    Resources.HealthCheck_DisplayName_RemoteAdapter,
                    Resources.HealthCheck_RemoteAdapterHealthNotSupported
                );
            }

            try {
                var adapterClient = CreateClient<AdaptersService.AdaptersServiceClient>();
                var callOptions = new GrpcCore.CallOptions(
                    cancellationToken: cancellationToken,
                    credentials: GetCallCredentials(context)
                );
                var remoteResponse = adapterClient.CheckAdapterHealthAsync(
                    new CheckAdapterHealthRequest() {
                        AdapterId = RemoteDescriptor.Id
                    },
                    callOptions
                );

                var result = (await remoteResponse.ResponseAsync.ConfigureAwait(false)).Result.ToAdapterHealthCheckResult();

                return new Diagnostics.HealthCheckResult(
                    Resources.HealthCheck_DisplayName_RemoteAdapter,
                    result.Status,
                    result.Description,
                    result.Error,
                    result.Data,
                    result.InnerResults
                );
            }
            catch (Exception e) {
                return Diagnostics.HealthCheckResult.Unhealthy(
                    Resources.HealthCheck_DisplayName_RemoteAdapter,
                    error: e.Message
                );
            }
        }


        /// <inheritdoc/>
        protected override async Task<IEnumerable<Diagnostics.HealthCheckResult>> CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            var results = new List<Diagnostics.HealthCheckResult>(await base.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false));
            if (!IsRunning) {
                return results;
            }

#if NET461 == false
            // Grpc.Net channel doesn't expose a way of getting the channel state.
            results.Add(
                Diagnostics.HealthCheckResult.Composite(
                    Resources.HealthCheck_DisplayName_Connection,
                    new[] {
                        await CheckRemoteHealthAsync(context, cancellationToken).ConfigureAwait(false)
                    },
                    Resources.HealthCheck_GrpcNetClientDescription
                )
            );
#else
            var coreChannel = _channel as GrpcCore.Channel;
            var state = coreChannel!.State;

            switch (state) {
                case GrpcCore.ChannelState.Ready:
                case GrpcCore.ChannelState.Idle:
                    results.Add(
                        Diagnostics.HealthCheckResult.Composite(
                            Resources.HealthCheck_DisplayName_Connection,
                            new [] {
                                await CheckRemoteHealthAsync(context, cancellationToken).ConfigureAwait(false)
                            },
                            // Use coreChannel.State instead of state, since, if the connection
                            // was idle in the switch statement, it will now be ready.
                            string.Format(context?.CultureInfo, Resources.HealthCheck_ChannelStateDescription, coreChannel.State.ToString())
                        )    
                    );
                    break;
                case GrpcCore.ChannelState.Shutdown:
                    results.Add(Diagnostics.HealthCheckResult.Unhealthy(
                        Resources.HealthCheck_DisplayName_Connection,
                        string.Format(context?.CultureInfo, Resources.HealthCheck_ChannelStateDescriptionNoInnerResults, state.ToString())
                    ));
                    break;
                default:
                    results.Add(Diagnostics.HealthCheckResult.Degraded(
                        Resources.HealthCheck_DisplayName_Connection,
                        string.Format(context?.CultureInfo, Resources.HealthCheck_ChannelStateDescriptionNoInnerResults, state.ToString())
                    ));
                    break;
            }
#endif

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
        public GrpcCore.CallCredentials? GetCallCredentials(IAdapterCallContext context) {
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
