﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.SignalR.Client;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Proxy;

using IntelligentPlant.BackgroundTasks;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {

    /// <summary>
    /// Adapter proxy that communicates with a remote adapter via ASP.NET Core SignalR.
    /// </summary>
    [AdapterMetadata(
        "https://www.intelligentplant.com/app-store-connect/adapters/proxies/signalr",
        ResourceType = typeof(Resources),
        Name = nameof(Resources.AdapterMetadata_DisplayName),
        Description = nameof(Resources.AdapterMetadata_Description),
        HelpUrl = "https://github.com/intelligentplant/AppStoreConnect.Adapters/tree/main/src/DataCore.Adapter.AspNetCore.SignalR.Proxy"
    )]
    public class SignalRAdapterProxy : AdapterBase<SignalRAdapterProxyOptions>, IAdapterProxy {

        /// <summary>
        /// Gets the logger factory for the proxy.
        /// </summary>
        internal new ILoggerFactory LoggerFactory {
            get { return base.LoggerFactory; }
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
        private HostInfo _remoteHostInfo = default!;

        /// <summary>
        /// The descriptor for the remote adapter.
        /// </summary>
        private AdapterDescriptorExtended _remoteDescriptor = default!;

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
        /// A factory delegate that can create hub connections.
        /// </summary>
        private readonly ConnectionFactory _connectionFactory;

        /// <summary>
        /// A factory delegate for creating extension feature implementations.
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly ExtensionFeatureFactory<SignalRAdapterProxy>? _extensionFeatureFactory;
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// The client used in standard adapter queries.
        /// </summary>
        private readonly Lazy<AdapterSignalRClient> _client;

        /// <summary>
        /// Additional hub connections created for extension features.
        /// </summary>
        private readonly ConcurrentDictionary<string, Lazy<Task<HubConnection>>> _extensionConnections = new ConcurrentDictionary<string, Lazy<Task<HubConnection>>>();

        /// <summary>
        /// The last health check result that was received from the remote adapter.
        /// </summary>
        private HealthCheckResult? _lastRemoteHealthCheckResult;

        /// <summary>
        /// Lock for reading/writing <see cref="_lastRemoteHealthCheckResult"/>.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncReaderWriterLock _lastRemoteHealthCheckResultLock = new Nito.AsyncEx.AsyncReaderWriterLock();


        /// <summary>
        /// Creates a new <see cref="SignalRAdapterProxy"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID. Specify <see langword="null"/> or white space to generate an ID 
        ///   automatically.
        /// </param>
        /// <param name="options">
        ///   The proxy options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances to use when sending or receiving 
        ///   extension objects.
        /// </param>
        /// <param name="loggerFactory">
        ///   The logger factory for the proxy.
        /// </param>
        public SignalRAdapterProxy(
            string id,
            SignalRAdapterProxyOptions options, 
            IBackgroundTaskService? backgroundTaskService,
            IEnumerable<IObjectEncoder> encoders,
            ILoggerFactory? loggerFactory
        ) : base(
            id,
            options, 
            backgroundTaskService, 
            loggerFactory
        ) {
#pragma warning disable CS0618 // Type or member is obsolete
            Encoders = encoders?.ToArray() ?? throw new ArgumentNullException(nameof(encoders));
            _remoteAdapterId = Options?.RemoteId ?? throw new ArgumentException(Resources.Error_AdapterIdIsRequired, nameof(options));
            _connectionFactory = Options?.ConnectionFactory ?? throw new ArgumentException(Resources.Error_ConnectionFactoryIsRequired, nameof(options));
            _extensionFeatureFactory = Options?.ExtensionFeatureFactory;
            _client = new Lazy<AdapterSignalRClient>(() => {
                var conn = _connectionFactory.Invoke(null);
                AddHubEventHandlers(conn);
                return new AdapterSignalRClient(conn, true, Options!.CompatibilityLevel);
            }, LazyThreadSafetyMode.ExecutionAndPublication);
#pragma warning restore CS0618 // Type or member is obsolete

            // Remove inherited custom functions feature. A proxy for this feature will be re-added
            // if supported by the remote adapter.
            RemoveFeature<Adapter.Extensions.ICustomFunctions>();
        }


        /// <summary>
        /// Gets the strongly-typed SignalR client for the proxy. This client can be used to query 
        /// standard adapter features (if supported by the remote adapter).
        /// </summary>
        /// <returns>
        ///   An <see cref="AdapterSignalRClient"/> object.
        /// </returns>
        public AdapterSignalRClient GetClient() {
            return _client.Value;
        }


        /// <summary>
        /// Gets or creates an active hub connection for use with an adapter extension feature.
        /// </summary>
        /// <param name="key">
        ///   The key for the extension hub. This cannot be <see langword="null"/> and will be 
        ///   vendor-specific.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The hub connection.
        /// </returns>
        /// <remarks>
        ///   The connection lifetime is managed by the proxy.
        /// </remarks>
        public Task<HubConnection> GetOrCreateExtensionHubConnection(string key, CancellationToken cancellationToken = default) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            return _extensionConnections.GetOrAdd(key, k => new Lazy<Task<HubConnection>>(() => Task.Run(async () => {
                var conn = _connectionFactory.Invoke(k);
                AddHubEventHandlers(conn);
                await conn.StartAsync(StopToken).ConfigureAwait(false);
                return conn;
            }), LazyThreadSafetyMode.ExecutionAndPublication)).Value.WithCancellation(cancellationToken);
        }


        /// <summary>
        /// Adds event handlers to a hub connection.
        /// </summary>
        /// <param name="connection">
        ///   The hub connection.
        /// </param>
        private void AddHubEventHandlers(HubConnection connection) {
            connection.Closed += err => {
                OnHealthStatusChanged();
                return Task.CompletedTask;
            };
            connection.Reconnected += id => {
                OnHealthStatusChanged();
                if (RemoteDescriptor.HasFeature<IHealthCheck>()) {
                    // Adapter supports health check subscriptions.
                    BackgroundTaskService.QueueBackgroundWorkItem(RunRemoteHealthSubscriptionAsync);
                }
                return Task.CompletedTask;
            };
            connection.Reconnecting += err => {
                OnHealthStatusChanged();
                return Task.CompletedTask;
            };
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
            RemoteHostInfo = await client.HostInfo.GetHostInfoAsync(cancellationToken).ConfigureAwait(false);
            var descriptor = await client.Adapters.GetAdapterAsync(_remoteAdapterId, cancellationToken).ConfigureAwait(false);

            RemoteDescriptor = descriptor;

            ProxyAdapterFeature.AddFeaturesToProxy(this, descriptor.Features);

#pragma warning disable CS0618 // Type or member is obsolete
            foreach (var extensionFeature in descriptor.Extensions) {
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

                        impl = ExtensionFeatureProxyGenerator.CreateExtensionFeatureProxy<SignalRAdapterProxy, SignalRAdapterProxyOptions, Extensions.AdapterExtensionFeatureImpl>(
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
#pragma warning restore CS0618 // Type or member is obsolete

            if (RemoteDescriptor.HasFeature<IHealthCheck>()) {
                // Adapter supports health check subscriptions.
                BackgroundTaskService.QueueBackgroundWorkItem(RunRemoteHealthSubscriptionAsync);
            }
        }


        /// <inheritdoc/>
        protected override async Task StartAsync(CancellationToken cancellationToken) {
            await InitAsync(cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override async Task StopAsync(CancellationToken cancellationToken) {
            if (_client.IsValueCreated) {
                await _client.Value.StopAsync(cancellationToken).ConfigureAwait(false);
            }
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
        private async Task RunRemoteHealthSubscriptionAsync(CancellationToken cancellationToken) {
            try {
                await foreach (var item in _client.Value.Adapters.CreateAdapterHealthChannelAsync(_remoteAdapterId, cancellationToken).ConfigureAwait(false)) {
                    using (await _lastRemoteHealthCheckResultLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                        _lastRemoteHealthCheckResult = CreateRemoteAdapterHealthCheckResult(item);
                    }
                    OnHealthStatusChanged();
                }
            }
            catch {
                if (!cancellationToken.IsCancellationRequested) {
                    using (await _lastRemoteHealthCheckResultLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                        _lastRemoteHealthCheckResult = null;
                    }
                    OnHealthStatusChanged();
                }
            }
        }


        /// <summary>
        /// Checks the health of the remote adapter.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the health check result.
        /// </returns>
        private async Task<HealthCheckResult> CheckRemoteHealthAsync(
            CancellationToken cancellationToken
        ) {
            if (!RemoteDescriptor.HasFeature<IHealthCheck>()) {
                return HealthCheckResult.Healthy(
                    Resources.HealthCheck_DisplayName_RemoteAdapter, 
                    Resources.HealthCheck_RemoteAdapterHealthNotSupported
                );
            }

            try {
                var result = await _client
                    .Value
                    .Adapters
                    .CheckAdapterHealthAsync(RemoteDescriptor.Id, cancellationToken)
                    .ConfigureAwait(false);

                return CreateRemoteAdapterHealthCheckResult(result);
            }
            catch (Exception e) {
                return HealthCheckResult.Unhealthy(
                    Resources.HealthCheck_DisplayName_RemoteAdapter,
                    error: e.Message
                );
            }
        }


        /// <summary>
        /// Converts a <see cref="HealthCheckResult"/> received from a remote adapter into a local 
        /// <see cref="HealthCheckResult"/> for the proxy.
        /// </summary>
        /// <param name="result">
        ///   The <see cref="HealthCheckResult"/> received from the remote adapter.
        /// </param>
        /// <returns>
        ///   A new <see cref="HealthCheckResult"/> for use in the local proxy.
        /// </returns>
        private static HealthCheckResult CreateRemoteAdapterHealthCheckResult(HealthCheckResult result) {
            return new HealthCheckResult(
                    Resources.HealthCheck_DisplayName_RemoteAdapter,
                    result.Status,
                    result.Description,
                    result.Error,
                    result.Data,
                    result.InnerResults
                );
        }


        /// <inheritdoc/>
        protected override async Task<IEnumerable<HealthCheckResult>> CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            var results = new List<HealthCheckResult>(await base.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false));
            if (!IsRunning) {
                return results;
            }

            if (_client.IsValueCreated) {
                var state = _client.Value.ConnectionState;
               
                switch (state) {
                    case HubConnectionState.Connected:
                        using (await _lastRemoteHealthCheckResultLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false)) {
                            if (_lastRemoteHealthCheckResult != null) {
                                results.Add(HealthCheckResult.Composite(
                                    Resources.HealthCheck_DisplayName_Connection,
                                    new[] { _lastRemoteHealthCheckResult.Value },
                                    string.Format(context?.CultureInfo, Resources.HealthCheck_HubConnectionStatusDescription, state.ToString())
                                ));
                                break;
                            }
                        }

                        results.Add(
                            HealthCheckResult.Composite(
                                Resources.HealthCheck_DisplayName_Connection,
                                new[] {
                                    await CheckRemoteHealthAsync(cancellationToken).ConfigureAwait(false)
                                },
                                string.Format(context?.CultureInfo, Resources.HealthCheck_HubConnectionStatusDescriptionNoInnerResults, state.ToString())
                            )
                        );
                        break;
                    default:
                        results.Add(HealthCheckResult.Degraded(
                            Resources.HealthCheck_DisplayName_Connection,
                            string.Format(context?.CultureInfo, Resources.HealthCheck_HubConnectionStatusDescriptionNoInnerResults, state.ToString())
                        ));
                        break;
                }
            }

            return results;
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing && _client.IsValueCreated) {
                _client.Value.Dispose();
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask DisposeAsyncCore() {
            await base.DisposeAsyncCore().ConfigureAwait(false);
            if (_client.IsValueCreated) {
                await _client.Value.DisposeAsync().ConfigureAwait(false);
            }
        }

    }
}
