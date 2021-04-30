using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        Description = nameof(Resources.AdapterMetadata_Description)
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
        private readonly ExtensionFeatureFactory<HttpAdapterProxy>? _extensionFeatureFactory;

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
            _extensionFeatureFactory = Options?.ExtensionFeatureFactory;
            _snapshotRefreshInterval = Options?.TagValuePushInterval ?? TimeSpan.FromMinutes(1);
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

            if (_snapshotRefreshInterval > TimeSpan.Zero && this.TryGetFeature<Adapter.RealTimeData.IReadSnapshotTagValues>(out var readSnapshot)) {
                // We are able to simulate tag value push functionality.
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

                        impl = ExtensionFeatureProxyGenerator.CreateExtensionFeatureProxy<HttpAdapterProxy, HttpAdapterProxyOptions, Extensions.AdapterExtensionFeatureImpl>(
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

            if (Options.HealthCheckPushInterval > TimeSpan.Zero && RemoteDescriptor.HasFeature<IHealthCheck>()) {
                // Remote adapter supports health checks. Although the HTTP client does not support 
                // push notifications, we can periodically signal that the status should be re-polled.
                BackgroundTaskService.QueueBackgroundWorkItem(async ct => {
                    do {
                        await Task.Delay(Options.HealthCheckPushInterval, ct).ConfigureAwait(false);
                        OnHealthStatusChanged();
                    } while (!ct.IsCancellationRequested);
                });
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

    }
}
