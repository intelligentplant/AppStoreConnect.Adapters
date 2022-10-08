using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Http.Client;
using DataCore.Adapter.Proxy;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Http.Proxy {

    /// <summary>
    /// Adapter proxy that communicates with a remote adapter via HTTP.
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
    [AdapterMetadata(
        "https://www.intelligentplant.com/app-store-connect/adapters/proxies/http",
        ResourceType = typeof(Resources),
        Name = nameof(Resources.AdapterMetadata_DisplayName),
        Description = nameof(Resources.AdapterMetadata_Description),
        HelpUrl = "https://github.com/intelligentplant/AppStoreConnect.Adapters/tree/main/src/DataCore.Adapter.Http.Proxy"
    )]
    public class HttpAdapterProxy : AdapterBase<HttpAdapterProxyOptions>, IAdapterProxy {

        /// <summary>
        /// The ID of the remote adapter.
        /// </summary>
        private readonly string _remoteAdapterId;

        /// <summary>
        /// Information about the remote host.
        /// </summary>
        private HostInfo _remoteHostInfo = default!;

        /// <summary>
        /// The descriptor for the remote adapter.
        /// </summary>
        private AdapterDescriptorExtended _remoteDescriptor = default!;

        /// <summary>
        /// Lock for accessing <see cref="_remoteDescriptor"/>.
        /// </summary>
        private readonly object _remoteInfoLock = new object();

        /// <summary>
        /// The interval to use between re-polling snapshot values for subscribed tags. Ignored if 
        /// the remote adapter does not support <see cref="Adapter.RealTimeData.ISnapshotTagValuePush"/>.
        /// </summary>
        private readonly TimeSpan _snapshotRefreshInterval;

        /// <summary>
        /// Specifies if the proxy can use SignalR connections.
        /// </summary>
        private bool _canUseSignalR;

        /// <summary>
        /// The active SignalR clients.
        /// </summary>
        private readonly ConcurrentDictionary<string, SignalRClientWrapper> _signalRClients = new ConcurrentDictionary<string, SignalRClientWrapper>(StringComparer.Ordinal);

        /// <summary>
        /// The proxy's logger.
        /// </summary>
        protected internal new ILogger Logger {
            get { return base.Logger; }
        }

        /// <summary>
        /// The <see cref="IObjectEncoder"/> instances to use when sending or receiving 
        /// extension objects.
        /// </summary>
        internal IEnumerable<IObjectEncoder> Encoders { get; }

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
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly ExtensionFeatureFactory<HttpAdapterProxy>? _extensionFeatureFactory;
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// The client used in standard adapter queries.
        /// </summary>
        private readonly AdapterHttpClient _client;


        /// <summary>
        /// Creates a new <see cref="HttpAdapterProxy"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID. Specify <see langword="null"/> or white space to generate an ID 
        ///   automatically.
        /// </param>
        /// <param name="client">
        ///   The Adapter HTTP client to use.
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
        public HttpAdapterProxy(
            string id,
            AdapterHttpClient client, 
            HttpAdapterProxyOptions options, 
            IBackgroundTaskService? taskScheduler,
            IEnumerable<IObjectEncoder> encoders,
            ILogger<HttpAdapterProxy>? logger
        ) : base(
            id,
            options, 
            taskScheduler, 
            logger
        ) {
            Encoders = encoders?.ToArray() ?? throw new ArgumentNullException(nameof(encoders));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _client.CompatibilityVersion = options?.CompatibilityVersion ?? CompatibilityVersion.Latest;
            _remoteAdapterId = Options?.RemoteId ?? throw new ArgumentException(Resources.Error_AdapterIdIsRequired, nameof(options));
#pragma warning disable CS0618 // Type or member is obsolete
            _extensionFeatureFactory = Options?.ExtensionFeatureFactory;
            _snapshotRefreshInterval = Options?.TagValuePushInterval ?? TimeSpan.FromMinutes(1);
#pragma warning restore CS0618 // Type or member is obsolete
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
        /// Gets or creates the <see cref="SignalRClientWrapper"/> for the specified 
        /// <see cref="IAdapterCallContext"/>.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="SignalRClientWrapper"/> for the <paramref name="context"/>.
        /// </returns>
        internal SignalRClientWrapper GetSignalRClient(IAdapterCallContext context) {
            var getKeys = Options.SignalROptions?.ConnectionIdentityFactory;
            if (getKeys == null) {
                getKeys = ctx => new[] { context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.User?.Identity?.Name ?? string.Empty };
            }
            
            var key = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(string.Join(",", getKeys.Invoke(context)))));
            
            return _signalRClients.GetOrAdd(key, k => {
                var client = new SignalRClientWrapper(
                    k,
                    new AspNetCore.SignalR.Client.AdapterSignalRClient(Options.SignalROptions!.ConnectionFactory.Invoke(new Uri(_client.HttpClient.BaseAddress, AspNetCore.SignalR.Client.AdapterSignalRClient.DefaultHubRoute), context)),
                    Options.SignalROptions.TimeToLive <= TimeSpan.Zero
                        ? TimeSpan.FromSeconds(30)
                        : Options.SignalROptions.TimeToLive
                );

                client.Disposed += OnSignalRClientDisposed;

                return client;
            });
        }


        /// <summary>
        /// Tries to get the SignalR client for the specified <see cref="IAdapterCallContext"/>.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/>.
        /// </param>
        /// <param name="client">
        ///   The SignalR client for the <paramref name="context"/>, or <see langword="null"/> if 
        ///   SignalR functionality is not available.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the SignalR functionalty is available, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool TryGetSignalRClient(IAdapterCallContext context, out AspNetCore.SignalR.Client.AdapterSignalRClient? client) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            
            if (!_canUseSignalR) {
                client = null;
                return false;
            }

            var wrapper = GetSignalRClient(context);
            client = wrapper.Client;
            return true;
        }


        /// <summary>
        /// Removes a disposed <see cref="SignalRClientWrapper"/> from the <see cref="_signalRClients"/> 
        /// dictionary.
        /// </summary>
        /// <param name="client">
        ///   The disposed <see cref="SignalRClientWrapper"/>.
        /// </param>
        private void OnSignalRClientDisposed(SignalRClientWrapper client) {
            _signalRClients.TryRemove(client.Key, out _);
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
        private async Task InitAsync(CancellationToken cancellationToken = default) {
            var client = GetClient();
            RemoteHostInfo = await client.HostInfo.GetHostInfoAsync(null, cancellationToken).ConfigureAwait(false);
            var descriptor = await client.Adapters.GetAdapterAsync(_remoteAdapterId, null, cancellationToken).ConfigureAwait(false);

            RemoteDescriptor = descriptor;

            _canUseSignalR = Options.CompatibilityVersion < CompatibilityVersion.Version_3_0
                ? Options.SignalROptions != null
                : Options.SignalROptions != null && (await client.HostInfo.GetAvailableApisAsync(null, cancellationToken).ConfigureAwait(false)).Any(x => x.Enabled && "SignalR".Equals(x.Name, StringComparison.OrdinalIgnoreCase));

            var v3OrLaterFeatures = new[] { 
                typeof(Adapter.Extensions.ICustomFunctions)
            };

            var requiresSignalRFeatures = new[] {
                typeof(IConfigurationChanges),
                typeof(Adapter.Events.IEventMessagePush),
                typeof(Adapter.Events.IEventMessagePushWithTopics),
                typeof(Adapter.RealTimeData.ISnapshotTagValuePush)
            };

            ProxyAdapterFeature.AddFeaturesToProxy(this, descriptor.Features, type => {
                if (requiresSignalRFeatures.Contains(type)) {
                    return _canUseSignalR;
                }

                if (Options.CompatibilityVersion >= CompatibilityVersion.Version_3_0) {
                    return true;
                }

                return !v3OrLaterFeatures.Contains(type);
            });

            if (!this.TryGetFeature<Adapter.RealTimeData.ISnapshotTagValuePush>(out _) && _snapshotRefreshInterval > TimeSpan.Zero && this.TryGetFeature<Adapter.RealTimeData.IReadSnapshotTagValues>(out var readSnapshot)) {
                // We are able to simulate tag value push functionality via polling.
                var simulatedPush = new Adapter.RealTimeData.PollingSnapshotTagValuePush(
                    readSnapshot!, 
                    new Adapter.RealTimeData.PollingSnapshotTagValuePushOptions() { 
                        PollingInterval = _snapshotRefreshInterval,
                        TagResolver = Adapter.RealTimeData.SnapshotTagValuePush.CreateTagResolverFromAdapter(this)
                    },
                    BackgroundTaskService,
                    Logger
                );
                AddFeature(typeof(Adapter.RealTimeData.ISnapshotTagValuePush), simulatedPush);
            }

            foreach (var extensionFeature in descriptor.Extensions) {
                try {
                    var impl = _extensionFeatureFactory?.Invoke(extensionFeature, this);
                    if (impl == null) {
                        if (!extensionFeature.TryCreateUriWithTrailingSlash(out var featureUri)) {
                            Logger.LogWarning(Resources.Log_NoExtensionImplementationAvailable, extensionFeature);
                            continue;
                        }

#pragma warning disable CS0618 // Type or member is obsolete
                        impl = ExtensionFeatureProxyGenerator.CreateExtensionFeatureProxy<HttpAdapterProxy, HttpAdapterProxyOptions, Extensions.AdapterExtensionFeatureImpl>(
                            this,
                            featureUri!
                        );
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                    AddFeatures(impl, addStandardFeatures: false);
                }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_ExtensionFeatureRegistrationError, extensionFeature);
                }
            }

            if (RemoteDescriptor.HasFeature<IHealthCheck>()) {
                if (_canUseSignalR) {
                    BackgroundTaskService.QueueBackgroundWorkItem(RunPushRemoteHealthSubscriptionAsync);
                }
                else {
                    BackgroundTaskService.QueueBackgroundWorkItem(RunPollingRemoteHealthSubscriptionAsync);
                }
            }
        }


        /// <inheritdoc/>
        protected override async Task StartAsync(CancellationToken cancellationToken) {
            await InitAsync(cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        /// <summary>
        /// Long-running task that tells the adapter to recompute the overall health status of the 
        /// adapter on a periodic basis.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token that will fire when the task should end.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will monitor for changes in the remote adapter health.
        /// </returns>
        private async Task RunPollingRemoteHealthSubscriptionAsync(CancellationToken cancellationToken) {
            var interval = Options.HealthCheckPushInterval;
            if (interval > TimeSpan.Zero) {
                do {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                    OnHealthStatusChanged();
                } while (!cancellationToken.IsCancellationRequested);
            }
        }


        /// <summary>
        /// Long-running task that tells the adapter to recompute the overall health status of the 
        /// adapter when a status change is received from the remote adapter via push notification.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token that will fire when the task should end.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will monitor for changes in the remote adapter health.
        /// </returns>
        private async Task RunPushRemoteHealthSubscriptionAsync(CancellationToken cancellationToken) {
            var client = GetSignalRClient(new DefaultAdapterCallContext());
            await foreach (var item in client.Client.Adapters.CreateAdapterHealthChannelAsync(RemoteDescriptor.Id, cancellationToken).ConfigureAwait(false)) {
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
        private async Task<HealthCheckResult> CheckRemoteHealthAsync(
            IAdapterCallContext context,
            CancellationToken cancellationToken
        ) {
            if (!RemoteDescriptor.HasFeature<IHealthCheck>()) {
                return HealthCheckResult.Healthy(
                    Resources.HealthCheck_DisplayName_RemoteAdapter,
                    Resources.HealthCheck_RemoteAdapterHealthNotSupported
                );
            }

            try {
                var client = GetClient();

                var result = await client.Adapters
                    .CheckAdapterHealthAsync(RemoteDescriptor.Id, context?.ToRequestMetadata(), cancellationToken)
                    .ConfigureAwait(false);

                return new HealthCheckResult(
                    Resources.HealthCheck_DisplayName_RemoteAdapter,
                    result.Status,
                    result.Description,
                    result.Error,
                    result.Data,
                    result.InnerResults
                );
            }
            catch (Exception e) {
                return HealthCheckResult.Unhealthy(
                    Resources.HealthCheck_DisplayName_RemoteAdapter,
                    error: e.Message
                );
            }
        }


        /// <inheritdoc/>
        protected override async Task<IEnumerable<HealthCheckResult>> CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            var results = new List<HealthCheckResult>(await base.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false));
            if (!IsRunning) {
                return results;
            }

            results.Add(
                HealthCheckResult.Composite(
                    Resources.HealthCheck_DisplayName_Connection,
                    new[] {
                        await CheckRemoteHealthAsync(context, cancellationToken).ConfigureAwait(false)
                    },
                    Resources.HealthChecks_RemoteHeathDescription
                )
            );

            return results;
        }


        /// <inheritdoc/>
        protected override async ValueTask DisposeAsyncCore() {
            await base.DisposeAsyncCore().ConfigureAwait(false);
            foreach (var item in _signalRClients.Values) {
                item.Dispose();
            }
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);

            if (disposing) {
                foreach (var item in _signalRClients.Values) {
                    item.Dispose();
                }
            }
        }

    }
}
