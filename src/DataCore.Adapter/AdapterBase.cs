using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter {

    /// <summary>
    /// Base class that adapter implementations can inherit from.
    /// </summary>
    /// <typeparam name="TAdapterOptions">
    ///   The options type for the adapter.
    /// </typeparam>
    public abstract class AdapterBase<TAdapterOptions> : IAdapter, IHealthCheck, IAsyncDisposable, IDisposable where TAdapterOptions : AdapterOptions, new() {

        /// <summary>
        /// Indicates if the adapter has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Indicates if the adapter is being disposed.
        /// </summary>
        private bool _isDisposing;

        /// <summary>
        /// The <typeparamref name="TAdapterOptions"/> monitor subscription.
        /// </summary>
        private readonly IDisposable _optionsMonitorSubscription;

        /// <summary>
        /// Logging.
        /// </summary>
        protected internal ILogger Logger { get; }

        /// <summary>
        /// Indicates if the adapter has been started.
        /// </summary>
        protected bool IsRunning { get; private set; }

        /// <summary>
        /// Indicates if the adapter is starting.
        /// </summary>
        private bool _isStarting;

        /// <summary>
        /// Ensures that only one startup attempt can occur at a time.
        /// </summary>
        private readonly SemaphoreSlim _startupLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Fires when <see cref="IAdapter.StopAsync(CancellationToken)"/> is called.
        /// </summary>
        private CancellationTokenSource _stopTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Gets a cancellation token that will fire when the adapter is stopped.
        /// </summary>
        public CancellationToken StopToken => _stopTokenSource.Token;

        /// <summary>
        /// Allows the adapter to register work items to be run in the background.
        /// </summary>
        public IBackgroundTaskService TaskScheduler { get; }

        /// <summary>
        /// The adapter descriptor.
        /// </summary>
        private AdapterDescriptor _descriptor;

        /// <summary>
        /// The adapter features.
        /// </summary>
        private readonly AdapterFeaturesCollection _features = new AdapterFeaturesCollection();

        /// <summary>
        /// Adapter properties.
        /// </summary>
        private ConcurrentDictionary<string, AdapterProperty> _properties = new ConcurrentDictionary<string, AdapterProperty>();

        /// <summary>
        /// The adapter options.
        /// </summary>
        protected TAdapterOptions Options { get; private set; }

        /// <inheritdoc/>
        public AdapterDescriptor Descriptor {
            get {
                CheckDisposed();
                lock (_descriptor) {
                    return _descriptor;
                }
            }
        }

        /// <inheritdoc/>
        public IAdapterFeaturesCollection Features {
            get {
                CheckDisposed();
                return _features;
            }
        }

        /// <inheritdoc/>
        bool IAdapter.IsRunning { get { return IsRunning; } }

        /// <inheritdoc/>
        public IEnumerable<AdapterProperty> Properties {
            get {
                CheckDisposed();
                return _properties.Values.Select(x => AdapterProperty.FromExisting(x)).ToArray();
            }
        }


        /// <summary>
        /// Creates a new <see cref="Adapter"/> object.
        /// </summary>
        /// <param name="options">
        ///   The adapter options.
        /// </param>
        /// <param name="taskScheduler">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="loggerFactory">
        ///   The logger factory for the adapter. Can be <see langword="null"/>. The category name 
        ///   for the adapter's logger will be <c>{adapter_type_name}.{adapter_name}</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   The <paramref name="options"/> are not valid.
        /// </exception>
        protected AdapterBase(TAdapterOptions options, IBackgroundTaskService taskScheduler, ILoggerFactory loggerFactory)
            : this(new AdapterOptionsMonitor<TAdapterOptions>(options), taskScheduler, loggerFactory) {
            AddFeatures(this);
        }


        /// <summary>
        /// Creates a new <see cref="Adapter"/> object that can monitor for changes in 
        /// configuration. Note that changes in the adapter's ID will be ignored once the adapter 
        /// has been created.
        /// </summary>
        /// <param name="optionsMonitor">
        ///   The monitor for the adapter's options type.
        /// </param>
        /// <param name="taskScheduler">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="loggerFactory">
        ///   The logger factory for the adapter. Can be <see langword="null"/>. The category name 
        ///   for the adapter's logger will be <c>{adapter_type_name}.{adapter_name}</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="optionsMonitor"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   The initial options retrieved from <paramref name="optionsMonitor"/> are not valid.
        /// </exception>
        protected AdapterBase(IAdapterOptionsMonitor<TAdapterOptions> optionsMonitor, IBackgroundTaskService taskScheduler, ILoggerFactory loggerFactory) {
            if (optionsMonitor == null) {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            AddFeatures(this);

            var options = optionsMonitor.CurrentValue;

            // Validate options.
            System.ComponentModel.DataAnnotations.Validator.ValidateObject(
                options,
                new System.ComponentModel.DataAnnotations.ValidationContext(options),
                true
            );

            _descriptor = new AdapterDescriptor(
                options.Id,
                string.IsNullOrWhiteSpace(options.Name)
                    ? options.Id
                    : options.Name,
                options.Description
            );

            Logger = loggerFactory?.CreateLogger(GetType().FullName + "." + _descriptor.Name) ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            Options = options;
            TaskScheduler = taskScheduler ?? BackgroundTaskService.Default;

            _optionsMonitorSubscription = optionsMonitor.OnChange((opts) => {
                // Validate updated options.
                try {
                    System.ComponentModel.DataAnnotations.Validator.ValidateObject(
                        opts,
                        new System.ComponentModel.DataAnnotations.ValidationContext(opts),
                        true
                    );
                }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_InvalidAdapterOptionsUpdate);
                    return;
                }

                Options = opts;
                OnOptionsChangeInternal(opts);
            });
        }


        /// <inheritdoc/>
        async Task IAdapter.StartAsync(CancellationToken cancellationToken) {
            CheckDisposed();
            await _startupLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                if (IsRunning) {
                    return;
                }
                if (StopToken.IsCancellationRequested) {
                    throw new InvalidOperationException(Resources.Error_AdapterIsStopping);
                }

                string descriptorId;
                lock (_descriptor) {
                    descriptorId = _descriptor.Id;
                }

                try {
                    Logger.LogInformation(Resources.Log_StartingAdapter, descriptorId);

                    _isStarting = true;
                    try {
                        using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopTokenSource.Token)) {
                            await StartAsync(ctSource.Token).ConfigureAwait(false);
                        }
                        IsRunning = true;
                    }
                    finally {
                        _isStarting = false;
                    }
                    Logger.LogInformation(Resources.Log_StartedAdapter, descriptorId);
                }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_AdapterStartupError, descriptorId);
                    throw;
                }
            }
            finally {
                _startupLock.Release();
            }
        }


        /// <inheritdoc/>
        async Task IAdapter.StopAsync(CancellationToken cancellationToken) {
            CheckDisposed();
            CheckStarted();

            string descriptorId;
            lock (_descriptor) {
                descriptorId = _descriptor.Id;
            }

            try {
                Logger.LogInformation(Resources.Log_StoppingAdapter, descriptorId);
                _stopTokenSource.Cancel();
                await StopAsync(false, cancellationToken).ConfigureAwait(false);
                Logger.LogInformation(Resources.Log_StoppedAdapter, descriptorId);
            }
            catch (Exception e) {
                Logger.LogError(e, Resources.Log_AdapterStopError, descriptorId);
                throw;
            }
            finally {
                _stopTokenSource = new CancellationTokenSource();
                IsRunning = false;
            }
        }


        /// <inheritdoc/>
        async Task<HealthCheckResult> IHealthCheck.CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            try {
                var results = await CheckHealthAsync(context, cancellationToken).ConfigureAwait(false);
                if (results == null || !results.Any()) {
                    return HealthCheckResult.Healthy(Resources.HealthChecks_CompositeResultDescription_Healthy);
                }

                var resultsArray = results.ToArray();

                var compositeStatus = HealthCheckResult.GetAggregateHealthStatus(resultsArray.Select(x => x.Status));
                string description;

                switch (compositeStatus) {
                    case HealthStatus.Unhealthy:
                        description = Resources.HealthChecks_CompositeResultDescription_Unhealthy;
                        break;
                    case HealthStatus.Degraded:
                        description = Resources.HealthChecks_CompositeResultDescription_Degraded;
                        break;
                    default:
                        description = Resources.HealthChecks_CompositeResultDescription_Healthy;
                        break;
                }

                return new HealthCheckResult(compositeStatus, description, null, null, resultsArray);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                return HealthCheckResult.Unhealthy(Resources.HealthChecks_CompositeResultDescription_Error, e.Message);
            }
        }



        /// <summary>
        /// Disposes of the adapter.
        /// </summary>
        /// <returns>
        ///   A <see cref="ValueTask"/> that represents the dispose operation.
        /// </returns>
        async ValueTask IAsyncDisposable.DisposeAsync() {
            if (_isDisposed || _isDisposing) {
                return;
            }
            try {
                _isDisposing = true;
                Logger.LogInformation(Resources.Log_DisposingAdapter, _descriptor.Id);
                _optionsMonitorSubscription?.Dispose();
                _stopTokenSource.Dispose();
                await StopAsync(true, default).ConfigureAwait(false);
            }
            finally {
                await _features.DisposeAsync().ConfigureAwait(false);
                _properties.Clear();
                _isDisposed = true;
                _isDisposing = false;
                IsRunning = false;
            }
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }


        /// <summary>
        /// Disposes of the adapter.
        /// </summary>
        void IDisposable.Dispose() {
            Task.Run(() => ((IAsyncDisposable) this).DisposeAsync()).GetAwaiter().GetResult();
        }


        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the adapter has not been started.
        /// </summary>
        /// <param name="allowStarting">
        ///   When <see langword="true"/>, an error will not be thrown if the adapter is currently 
        ///   starting.
        /// </param>
        protected void CheckStarted(bool allowStarting = false) {
            if (IsRunning || (allowStarting && _isStarting)) {
                return;
            }

            throw new InvalidOperationException(Resources.Error_AdapterIsNotStarted);
        }


        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the adapter has been disposed.
        /// </summary>
        protected void CheckDisposed() {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
        }


        /// <summary>
        /// Starts the adapter.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that represents the start operation.
        /// </returns>
        protected abstract Task StartAsync(CancellationToken cancellationToken);


        /// <summary>
        /// Stops the adapter.
        /// </summary>
        /// <param name="disposing">
        ///   A flag that indicates if the adapter is being stopped because it is being disposed, 
        ///   or if <see cref="IAdapter.StopAsync(CancellationToken)"/> was explicitly called.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that represents the stop operation.
        /// </returns>
        protected abstract Task StopAsync(bool disposing, CancellationToken cancellationToken);


        /// <summary>
        /// Performs an adapter health check.
        /// </summary>
        /// <param name="context">
        ///   The call context for the operation, to allow authorization to be applied to the 
        ///   operation if required.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will return the <see cref="HealthCheckResult"/> for the 
        ///   health check.
        /// </returns>
        /// <remarks>
        ///   Override this method to perform custom health checks for your adapter. The default 
        ///   implementation will return unhealthy status if <see cref="IsRunning"/> is 
        ///   <see langword="false"/>.
        /// </remarks>
        protected virtual Task<IEnumerable<HealthCheckResult>> CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            if (!IsRunning) {
                var result = HealthCheckResult.Unhealthy(Resources.HealthChecks_CompositeResultDescription_NotStarted);
                return Task.FromResult<IEnumerable<HealthCheckResult>>(new[] { result });
            }

            return Task.FromResult<IEnumerable<HealthCheckResult>>(Array.Empty<HealthCheckResult>());
        }


        /// <summary>
        /// Invoked when the adapter detects that its supplied <typeparamref name="TAdapterOptions"/> 
        /// have changed. This method will only be called if an <see cref="IAdapterOptionsMonitor{TAdapterOptions}"/> 
        /// was provided when the adapter was created.
        /// </summary>
        /// <param name="options">
        ///   The updated options.
        /// </param>
        private void OnOptionsChangeInternal(TAdapterOptions options) {
            if (options == null) {
                return;
            }

            // Check if we need to update the descriptor.

            AdapterDescriptor descriptor;
            lock (_descriptor) {
                descriptor = _descriptor;
            }

            if (!string.Equals(options.Name, descriptor.Name, StringComparison.Ordinal) || 
                !string.Equals(options.Description, descriptor.Description, StringComparison.Ordinal)) {
                lock (_descriptor) {
                    _descriptor = new AdapterDescriptor(
                        descriptor.Id, // ID cannot change once initially configured!
                        string.IsNullOrWhiteSpace(options.Name)
                            ? options.Id
                            : options.Name,
                        options.Description
                    );
                }
            }

            // Call the handler on the implementing class.

            OnOptionsChange(options);
        }


        /// <summary>
        /// Override this method in a subclass to receive notifications when the adapter's options 
        /// have changed.
        /// </summary>
        /// <param name="options">
        ///   The updated options.
        /// </param>
        protected virtual void OnOptionsChange(TAdapterOptions options) {
            // Do nothing.
        }


        /// <summary>
        /// Adds a feature to the adapter.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature. This must be an interface derived from <see cref="IAdapterFeature"/>.
        /// </typeparam>
        /// <typeparam name="TFeatureImpl">
        ///   The feature implementation type. This must be a concrete class that implements 
        ///   <typeparamref name="TFeature"/>.
        /// </typeparam>
        /// <param name="feature">
        ///   The implementation object.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFeature"/> is not an interface, or it does not interit from 
        ///   <see cref="IAdapterFeature"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An implementation of <typeparamref name="TFeature"/> has already been registered.
        /// </exception>
        public void AddFeature<TFeature, TFeatureImpl>(TFeatureImpl feature) where TFeature : IAdapterFeature where TFeatureImpl : class, TFeature {
            CheckDisposed();
            _features.Add<TFeature, TFeatureImpl>(feature ?? throw new ArgumentNullException(nameof(feature)));
        }


        /// <summary>
        /// Adds an adapter feature.
        /// </summary>
        /// <param name="featureType">
        ///   The feature type. This must be an interface derived from <see cref="IAdapterFeature"/>.
        /// </param>
        /// <param name="feature">
        ///   The feature implementation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="featureType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="featureType"/> is not an adapter feature type.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="feature"/> is not an instance of <paramref name="featureType"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An implementation of <paramref name="featureType"/> has already been registered.
        /// </exception>
        public void AddFeature(Type featureType, object feature) {
            CheckDisposed();
            _features.Add(featureType, feature);
        }


        /// <summary>
        /// Adds all adapter features implemented by the specified feature provider.
        /// </summary>
        /// <param name="provider">
        ///   The object that will provide the adapter feature implementations.
        /// </param>
        /// <param name="addStandardFeatures">
        ///   Specifies if standard adapter feature implementations should be added to the 
        ///   collection. Standard feature types can be obtained by calling 
        ///   <see cref="TypeExtensions.GetStandardAdapterFeatureTypes"/>.
        /// </param>
        /// <param name="addExtensionFeatures">
        ///   Specifies if extension adapter feature implementations should be added to the 
        ///   collection. Extension features must derive from <see cref="IAdapterExtensionFeature"/>.
        /// </param>
        /// <remarks>
        ///   All interfaces implemented by the <paramref name="provider"/> that extend 
        ///   <see cref="IAdapterFeature"/> will be registered with the <see cref="Adapter"/> 
        ///   (assuming that they meet the <paramref name="addStandardFeatures"/> and 
        ///   <paramref name="addExtensionFeatures"/> constraints).
        /// </remarks>
        public void AddFeatures(object provider, bool addStandardFeatures = true, bool addExtensionFeatures = true) {
            CheckDisposed();
            _features.AddFromProvider(provider ?? throw new ArgumentNullException(nameof(provider)), addStandardFeatures, addExtensionFeatures);
        }


        /// <summary>
        /// Removes a registered feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type to remove.
        /// </typeparam>
        /// <returns>
        ///   <see langword="true"/> if the feature was removed, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        public bool RemoveFeature<TFeature>() where TFeature : IAdapterFeature {
            CheckDisposed();
            return _features.Remove<TFeature>();
        }


        /// <summary>
        /// Removes all features.
        /// </summary>
        public void RemoveAllFeatures() {
            CheckDisposed();
            _features.Clear();
        }


        /// <summary>
        /// Adds a bespoke adapter property.
        /// </summary>
        /// <param name="key">
        ///   The property key.
        /// </param>
        /// <param name="value">
        ///   The property value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> i <see langword="null"/>.
        /// </exception>
        protected void AddProperty(string key, object value) {
            CheckDisposed();
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            _properties[key] = AdapterProperty.Create(key, value);
        }


        /// <summary>
        /// Removes a bespoke adapter property.
        /// </summary>
        /// <param name="key">
        ///   The property key.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the property was removed, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> i <see langword="null"/>.
        /// </exception>
        protected bool RemoveProperty(string key) {
            CheckDisposed();
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            return _properties.TryRemove(key, out var _);
        }


        /// <summary>
        /// Removes all bespoke adapter properties.
        /// </summary>
        protected void RemoveAllProperties() {
            _properties.Clear();
        }

    }
}
