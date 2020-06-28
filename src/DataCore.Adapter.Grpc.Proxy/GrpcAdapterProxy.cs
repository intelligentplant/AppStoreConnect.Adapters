using System;
using System.Collections.Generic;
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
using DataCore.Adapter.Diagnostics;

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
        /// gRPC channel.
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

            if (RemoteDescriptor.HasFeature<IHealthCheck>()) {
                // Adapter supports health check subscriptions.
                TaskScheduler.QueueBackgroundWorkItem(RunRemoteHealthSubscription);
            }

            // Send periodic heartbeat message - this ensures that topic-based subscriptions 
            // (where separate actions are required to create the subscription and the individual 
            // topic subscription streams) are kept alive even if there aren't currently topics 
            // being actively subscribed to.
            TaskScheduler.QueueBackgroundWorkItem(RunRemoteHeartbeatLoop);
        }


        /// <inheritdoc/>
        protected override async Task StartAsync(CancellationToken cancellationToken) {
            await Init(cancellationToken).ConfigureAwait(false);
        }


#if NETSTANDARD2_1
        /// <inheritdoc/>
        protected override Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
#else
        /// <inheritdoc/>
        protected override async Task StopAsync(CancellationToken cancellationToken) {
            if (_channel is GrpcCore.Channel channel) {
                try {
                    await channel.ShutdownAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                    Logger.LogError(e, Resources.Log_ChannelShutdownError);
                }
            }
        }
#endif


#if NETSTANDARD2_0
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
#endif


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
                headers.Add("Request-Id", context.CorrelationId);
            }

            return new GrpcCore.CallOptions(
                cancellationToken: cancellationToken,
                credentials: GetCallCredentials(context),
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
            var callOptions = GetCallOptions(null, cancellationToken);

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

#if NETSTANDARD2_1
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
            var state = coreChannel.State;

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


        private async Task RunRemoteHeartbeatLoop(CancellationToken cancellationToken) {
            var client = CreateClient<HeartbeatService.HeartbeatServiceClient>();
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
