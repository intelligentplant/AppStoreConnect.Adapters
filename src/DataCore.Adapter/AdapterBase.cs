using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Extensions;

using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter {

    /// <summary>
    /// Base class for adapter implementations.
    /// </summary>
    /// <seealso cref="AdapterBase{TAdapterOptions}"/>
    public abstract class AdapterBase : IAdapter {

        #region [ Fields / Properties ]

        /// <summary>
        /// Maximum length for an adapter ID.
        /// </summary>
        public const int MaxIdLength = 100;

        /// <summary>
        /// Maximum length for an adapter name.
        /// </summary>
        public const int MaxNameLength = 200;

        /// <summary>
        /// Maximum length for an adapter description.
        /// </summary>
        public const int MaxDescriptionLength = 500;

        /// <summary>
        /// Indicates if the adapter is disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Indicates if the resources disposed by <see cref="DisposeCommon"/> have been disposed.
        /// </summary>
        private bool _isDisposedCommon;

        /// <summary>
        /// Logging.
        /// </summary>
        protected internal ILogger Logger { get; }

        /// <summary>
        /// Scope for the <see cref="Logger"/> that gets set when the adapter is created.
        /// </summary>
        private readonly IDisposable _loggerScope;

        /// <summary>
        /// Indicates if the adapter is enabled. When <see langword="false"/>, calls to 
        /// <see cref="IAdapter.StartAsync(CancellationToken)"/> will throw an 
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        protected virtual bool IsEnabled { get { return true; } }

        /// <summary>
        /// Indicates if the adapter has been started.
        /// </summary>
        protected bool IsRunning { get; private set; }

        /// <summary>
        /// Indicates if the adapter is starting.
        /// </summary>
        protected bool IsStarting { get; private set; }

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
        public CancellationToken StopToken { get; }

        /// <summary>
        /// Allows the adapter to register work items to be run in the background. The adapter's 
        /// <see cref="StopToken"/> is always added to the list of <see cref="CancellationToken"/> 
        /// instances that the background task observes.
        /// </summary>
        public IBackgroundTaskService BackgroundTaskService { get; }

        /// <summary>
        /// The adapter descriptor.
        /// </summary>
        private AdapterDescriptor _descriptor;

        /// <summary>
        /// The adapter features.
        /// </summary>
        private readonly AdapterFeaturesCollection _features = new AdapterFeaturesCollection(true);

        /// <summary>
        /// The <see cref="HealthCheckManager"/> that provides the <see cref="IHealthCheck"/> feature.
        /// </summary>
        private readonly HealthCheckManager _healthCheckManager;

        /// <summary>
        /// Adapter properties.
        /// </summary>
        private ConcurrentDictionary<string, AdapterProperty> _properties = new ConcurrentDictionary<string, AdapterProperty>();


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
        bool IAdapter.IsEnabled { get { return IsEnabled; } }

        /// <inheritdoc/>
        bool IAdapter.IsRunning { get { return IsRunning; } }

        /// <inheritdoc/>
        public IEnumerable<AdapterProperty> Properties {
            get {
                CheckDisposed();
                return _properties
                    .Values
                    .Select(x => AdapterProperty.FromExisting(x))
                    .OrderBy(x => x.Name)
                    .ToArray();
            }
        }

        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a new <see cref="AdapterBase"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID. If <see langword="null"/> or white space, a unique identifier will 
        ///   be generated.
        /// </param>
        /// <param name="name">
        ///   The adapter display name. If <see langword="null"/> or white space, the adapter ID 
        ///   will also be used as the display name.
        /// </param>
        /// <param name="description">
        ///   The adapter description.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running background operations. 
        ///   Specify <see langword="null"/> to use <see cref="BackgroundTaskService.Default"/>.
        /// </param>
        /// <param name="logger">
        ///   The logger to use. Specify <see langword="null"/> to use 
        ///   <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is longer than <see cref="MaxIdLength"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is longer than <see cref="MaxNameLength"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="description"/> is longer than <see cref="MaxDescriptionLength"/>.
        /// </exception>
        protected AdapterBase(
            string id, 
            string? name = null, 
            string? description = null, 
            IBackgroundTaskService? backgroundTaskService = null, 
            ILogger? logger = null
        ) {
            if (string.IsNullOrWhiteSpace(id)) {
                id = Guid.NewGuid().ToString();
            }
            if (string.IsNullOrWhiteSpace(name)) {
                name = id;
            }

            if (id.Length > MaxIdLength) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterIdIsTooLong, MaxIdLength), nameof(id));
            }
            if (name!.Length > MaxNameLength) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterNameIsTooLong, MaxNameLength), nameof(name));
            }
            if (description != null && description.Length > MaxDescriptionLength) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterDescriptionIsTooLong, MaxDescriptionLength), nameof(description));
            }

            StopToken = _stopTokenSource.Token;
            _descriptor = new AdapterDescriptor(id, name, description);
            BackgroundTaskService = new BackgroundTaskServiceWrapper(this, backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default);
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            _loggerScope = Logger.BeginScope(_descriptor.Id);

            _healthCheckManager = new HealthCheckManager(this);
            AddFeatures(_healthCheckManager);
            AddFeatures(this);
        }

        #endregion

        #region [ Helper Methods ]

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the adapter has been disposed.
        /// </summary>
        protected void CheckDisposed() {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().Name);
            }
        }


        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the adapter has not been started.
        /// </summary>
        /// <param name="allowStarting">
        ///   When <see langword="true"/>, an error will not be thrown if the adapter is currently 
        ///   starting.
        /// </param>
        protected void CheckStarted(bool allowStarting = false) {
            if (IsRunning || (allowStarting && IsStarting)) {
                return;
            }

            throw new InvalidOperationException(Resources.Error_AdapterIsNotStarted);
        }


        /// <summary>
        /// Updates the name and/or description for the adapter.
        /// </summary>
        /// <param name="name">
        ///   The new adapter name. Specify a <see langword="null"/> or white space value to leave 
        ///   the name unchanged.
        /// </param>
        /// <param name="description">
        ///   The new adapter description. Specify <see langword="null"/> to leave the description 
        ///   unchanged.
        /// </param>
        protected void UpdateDescriptor(string? name = null, string? description = null) {
            if (!string.IsNullOrWhiteSpace(name)) {
                lock (_descriptor) {
                    _descriptor = new AdapterDescriptor(
                        _descriptor.Id, 
                        name!, 
                        description ?? _descriptor.Description
                    );
                }
            }
            else if (description != null) {
                lock (_descriptor) {
                    _descriptor = new AdapterDescriptor(
                        _descriptor.Id,
                        _descriptor.Name,
                        description
                    );
                }
            }
        }


        /// <summary>
        /// Validates the <see cref="IAdapterCallContext"/> passed to an adapter feature method.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        protected virtual void ValidateContext(IAdapterCallContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
        }


        /// <summary>
        /// Validates a request object passed to an adapter feature method.
        /// </summary>
        /// <typeparam name="TRequest">
        ///   The request type.
        /// </typeparam>
        /// <param name="request">
        ///   The request object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        protected virtual void ValidateRequest<TRequest>(TRequest request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            Validator.ValidateObject(request, new ValidationContext(request), true);
        }


        /// <summary>
        /// Checks the health of all adapter features that implement <see cref="IFeatureHealthCheck"/>.
        /// </summary>
        /// <param name="context">
        ///   The call context for the operation, to allow authorization to be applied to the 
        ///   operation if required.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return the <see cref="HealthCheckResult"/> for the 
        ///   health check.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        protected async Task<IEnumerable<HealthCheckResult>> CheckFeatureHealthAsync(
            IAdapterCallContext context,
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (!IsRunning) {
                return Array.Empty<HealthCheckResult>();
            }

            var result = new List<HealthCheckResult>();
            var processedFeatures = new HashSet<object>();

            foreach (var key in _features.Keys.ToArray()) {
                if (cancellationToken.IsCancellationRequested) {
                    return Array.Empty<HealthCheckResult>();
                }

                var feature = _features[key];

                if (feature == null || feature == this || !processedFeatures.Add(feature) || !(feature is IFeatureHealthCheck healthCheck)) {
                    continue;
                }

                var healthCheckName = string.Format(context.CultureInfo, Resources.HealthChecks_DisplayName_FeatureHealth, key);
                var featureHealth = await healthCheck.CheckFeatureHealthAsync(context, cancellationToken).ConfigureAwait(false);
                
                // Create new result that uses normalised name.
                result.Add(new HealthCheckResult(
                    healthCheckName, 
                    featureHealth.Status, 
                    featureHealth.Description, 
                    featureHealth.Error, 
                    featureHealth.Data, 
                    featureHealth.InnerResults
                ));
            }

            return result;
        }


        /// <summary>
        /// Informs the adapter that its overall health status needs to be recomputed (for example, 
        /// due to a disconnection from an external system). Subscribers to health status updates 
        /// will receive the updated health status.
        /// </summary>
        protected internal void OnHealthStatusChanged() {
            _healthCheckManager.RecalculateHealthStatus();
        }

        #endregion

        #region [ Feature Management ]

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
        public void AddFeature(Type featureType, IAdapterFeature feature) {
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
        ///   collection. Extension features must derive from <see cref="IAdapterExtensionFeature"/> 
        ///   and must match the criteria specified in <see cref="TypeExtensions.IsExtensionAdapterFeature"/>.
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
        /// Adds all standard adapter features implemented by the specified feature provider. 
        /// Standard feature types can be obtained by calling <see cref="TypeExtensions.GetStandardAdapterFeatureTypes"/>
        /// </summary>
        /// <param name="provider">
        ///   The object that will provide the adapter feature implementations.
        /// </param>
        public void AddStandardFeatures(object provider) {
            AddFeatures(provider, true, false);
        }


        /// <summary>
        /// Adds all extension adapter features implemented by the specified feature provider. See 
        /// the remarks for details on how extension feature types are identified.
        /// </summary>
        /// <param name="provider"></param>
        /// <remarks>
        /// 
        /// <para>
        ///   Extension feature implementations supplied by the <paramref name="provider"/> are 
        ///   identified in one of two ways:
        /// </para>
        /// 
        /// <list type="number">
        ///   <item>
        ///     <description>
        ///       If the <paramref name="provider"/> implements <see cref="IAdapterExtensionFeature"/> 
        ///       and is directly annotated with <see cref="ExtensionFeatureAttribute"/>, the 
        ///       <paramref name="provider"/> will be directly registered using its own type as the 
        ///       index in the adapter's features dictionary.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       If the <paramref name="provider"/> implements any interfaces that extend 
        ///       <see cref="IAdapterExtensionFeature"/> that are annotated with 
        ///       <see cref="ExtensionFeatureAttribute"/>, the <paramref name="provider"/> 
        ///       will be registered using each of the implemented extension feature interfaces.
        ///     </description>
        ///   </item>
        /// </list>
        /// 
        /// </remarks>
        public void AddExtensionFeatures(object provider) {
            AddFeatures(provider, false, true);
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

        #endregion

        #region [ Property Management ]

        /// <summary>
        /// Adds a bespoke adapter property.
        /// </summary>
        /// <param name="key">
        ///   The property key.
        /// </param>
        /// <param name="value">
        ///   The property value.
        /// </param>
        /// <param name="description">
        ///   The property description.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> i <see langword="null"/>.
        /// </exception>
        protected void AddProperty(string key, object value, string? description = null) {
            CheckDisposed();
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            _properties[key] = AdapterProperty.Create(key, value, description);
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

        #endregion

        #region [ Abstract / Virtual Methods ]

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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that represents the stop operation.
        /// </returns>
        protected abstract Task StopAsync(CancellationToken cancellationToken);


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
        ///   <see langword="false"/>, or a collection of health check results for all features 
        ///   implementing <see cref="IFeatureHealthCheck"/> otherwise.
        /// </remarks>
        protected internal virtual async Task<IEnumerable<HealthCheckResult>> CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            if (!IsRunning) {
                var result = HealthCheckResult.Unhealthy(Resources.HealthChecks_DisplayName_OverallAdapterHealth, Resources.HealthChecks_CompositeResultDescription_NotStarted);
                return new[] { result };
            }

            return await CheckFeatureHealthAsync(context, cancellationToken).ConfigureAwait(false);
        }

        #endregion


        /// <inheritdoc/>
        async Task IAdapter.StartAsync(CancellationToken cancellationToken) {
            if (!IsEnabled) {
                throw new InvalidOperationException(Resources.Error_AdapterIsDisabled);
            }

            CheckDisposed();
            await _startupLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                if (IsRunning) {
                    return;
                }
                if (StopToken.IsCancellationRequested) {
                    throw new InvalidOperationException(Resources.Error_AdapterIsStopping);
                }

                var descriptorId = Descriptor.Id;

                try {
                    Logger.LogInformation(Resources.Log_StartingAdapter, descriptorId);

                    IsStarting = true;
                    try {
                        using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopTokenSource.Token)) {
                            await StartAsync(ctSource.Token).ConfigureAwait(false);
                        }
                        IsRunning = true;
                    }
                    finally {
                        if (!cancellationToken.IsCancellationRequested) {
                            await _healthCheckManager.Init(cancellationToken).ConfigureAwait(false);
                        }
                        IsStarting = false;
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
            if (!IsStarting && !IsRunning) {
                return;
            }

            var descriptorId = Descriptor.Id;

            try {
                Logger.LogInformation(Resources.Log_StoppingAdapter, descriptorId);
                _stopTokenSource.Cancel();
                await StopAsync(cancellationToken).ConfigureAwait(false);
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
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }



        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Called from DisposeAsync")]
        public async ValueTask DisposeAsync() {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~AdapterBase() {
            Dispose(false);
        }


        /// <summary>
        /// Disposes of items common to both <see cref="Dispose(bool)"/> and 
        /// <see cref="DisposeAsyncCore"/>.
        /// </summary>
        private void DisposeCommon() {
            if (_isDisposedCommon || _isDisposed) {
                return;
            }

            _stopTokenSource?.Cancel();
            _stopTokenSource?.Dispose();
            _healthCheckManager.Dispose();
            _properties.Clear();
            _loggerScope.Dispose();
            _startupLock.Dispose();

            _isDisposedCommon = true;
        }


        /// <summary>
        /// Releases adapter resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the adapter is being disposed synchronously, or <see langword="false"/> 
        ///   if it is being disposed asynchronously, or finalized.
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (_isDisposed) {
                return;
            }

            if (disposing) {
                DisposeCommon();
                _features.Dispose();
            }

            _isDisposed = true;
        }


        /// <summary>
        /// Asynchronously releases adapter resources.
        /// </summary>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will perform the operation.
        /// </returns>
        /// <remarks>
        ///   Override both this method and <see cref="Dispose(bool)"/> if your adapter requires 
        ///   a separate asynchronous resource cleanup implementation. When calling <see cref="DisposeAsync"/>, 
        ///   both <see cref="DisposeAsyncCore"/> and <see cref="Dispose(bool)"/> will be called. 
        ///   The call to <see cref="Dispose(bool)"/> will be passed <see langword="false"/> when 
        ///   the object is being disposed asynchronously.
        /// </remarks>
        protected virtual async ValueTask DisposeAsyncCore() {
            DisposeCommon();
            await _features.DisposeAsync().ConfigureAwait(false);
        }


        /// <summary>
        /// <see cref="IBackgroundTaskService"/> implementation that always uses <see cref="StopToken"/> 
        /// as an additional cancellation token.
        /// </summary>
        private class BackgroundTaskServiceWrapper : IBackgroundTaskService, IDisposable {

            /// <summary>
            /// The owning adapter.
            /// </summary>
            private readonly AdapterBase _adapter;

            /// <summary>
            /// The inner <see cref="IBackgroundTaskService"/> to use.
            /// </summary>
            private readonly IBackgroundTaskService _inner;

            /// <inheritdoc/>
            public bool IsRunning => _inner.IsRunning;

            /// <inheritdoc/>
            public int QueuedItemCount => _inner.QueuedItemCount;


            /// <summary>
            /// Creates a new <see cref="BackgroundTaskServiceWrapper"/> object.
            /// </summary>
            /// <param name="adapter">
            ///   The owning adapter.
            /// </param>
            /// <param name="inner">
            ///   The inner <see cref="IBackgroundTaskService"/> to use.
            /// </param>
            internal BackgroundTaskServiceWrapper(AdapterBase adapter, IBackgroundTaskService inner) {
                _adapter = adapter;
                _inner = inner;
            }


            /// <inheritdoc/>
            public void QueueBackgroundWorkItem(BackgroundWorkItem workItem) {
                if (workItem.WorkItemAsync != null) {
                    _inner.QueueBackgroundWorkItem(workItem.WorkItemAsync, workItem.Description, _adapter.StopToken);
                }
                else {
                    _inner.QueueBackgroundWorkItem(workItem.WorkItem!, workItem.Description, _adapter.StopToken);
                }
            }


            /// <inheritdoc/>
            public void Dispose() {
                // No action required.
            }

        }

    }
}
