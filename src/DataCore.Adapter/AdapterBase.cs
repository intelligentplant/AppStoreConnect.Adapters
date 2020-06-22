using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter {

    /// <summary>
    /// Base class for adapter implementations.
    /// </summary>
    /// <seealso cref="AdapterBase{TAdapterOptions}"/>
    public abstract class AdapterBase : IAdapter, IHealthCheckPush {

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
        /// The current halth check subscriptions.
        /// </summary>
        private readonly HashSet<HealthCheckSubscription> _healthCheckSubscriptions = new HashSet<HealthCheckSubscription>();

        /// <summary>
        /// The most recent health check that was performed.
        /// </summary>
        private HealthCheckResult? _latestHealthCheck;

        /// <summary>
        /// For protecting access to <see cref="_healthCheckSubscriptions"/> and <see cref="_latestHealthCheck"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _healthCheckSubscriptionsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Ensures that only one call the <see cref="CheckHealthInternalAsync"/> can occur at a time.
        /// </summary>
        private readonly SemaphoreSlim _healthCheckUpdateLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Channel that is used to tell the adapter when it should recompute its health status.
        /// </summary>
        private readonly Channel<bool> _recomputeHealthChannel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { 
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite
        });

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
                return _properties.Values.Select(x => AdapterProperty.FromExisting(x)).ToArray();
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
        /// <param name="scheduler">
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
            string name = null, 
            string description = null, 
            IBackgroundTaskService scheduler = null, 
            ILogger logger = null
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
            if (name.Length > MaxNameLength) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterNameIsTooLong, MaxNameLength), nameof(name));
            }
            if (description != null && description.Length > MaxDescriptionLength) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterDescriptionIsTooLong, MaxDescriptionLength), nameof(description));
            }

            _descriptor = new AdapterDescriptor(id, name, description);
            TaskScheduler = scheduler ?? BackgroundTaskService.Default;
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            _loggerScope = Logger.BeginScope(_descriptor.Id);

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
        protected void UpdateDescriptor(string name = null, string description = null) {
            if (!string.IsNullOrWhiteSpace(name)) {
                lock (_descriptor) {
                    _descriptor = new AdapterDescriptor(
                        _descriptor.Id, 
                        name, 
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
        /// Performs health checks.
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
        private async Task<HealthCheckResult> CheckHealthInternalAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            await _healthCheckUpdateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                var results = await CheckHealthAsync(context, cancellationToken).ConfigureAwait(false);
                if (results == null || !results.Any()) {
                    return HealthCheckResult.Healthy(Resources.HealthChecks_DisplayName_OverallAdapterHealth, Resources.HealthChecks_CompositeResultDescription_Healthy);
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

                var overallResult = new HealthCheckResult(Resources.HealthChecks_DisplayName_OverallAdapterHealth, compositeStatus, description, null, null, resultsArray);
                _healthCheckSubscriptionsLock.EnterWriteLock();
                try {
                    _latestHealthCheck = overallResult;
                }
                finally {
                    _healthCheckSubscriptionsLock.ExitWriteLock();
                }

                return overallResult;
            }
            catch (OperationCanceledException) {
                throw;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                return HealthCheckResult.Unhealthy(Resources.HealthChecks_DisplayName_OverallAdapterHealth, Resources.HealthChecks_CompositeResultDescription_Error, e.Message);
            }
            finally {
                _healthCheckUpdateLock.Release();
            }
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
        protected async Task<IEnumerable<HealthCheckResult>> CheckFeatureHealthAsync(
            IAdapterCallContext context,
            CancellationToken cancellationToken
        ) {
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

                var healthCheckName = string.Format(context?.CultureInfo, Resources.HealthChecks_DisplayName_FeatureHealth, key.Name);
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
        /// Notifies that a subscription was disposed.
        /// </summary>
        /// <param name="subscription">
        ///   The disposed subscription.
        /// </param>
        private void OnHealthCheckSubscriptionCancelled(HealthCheckSubscription subscription) {
            if (_isDisposed) {
                return;
            }

            _healthCheckSubscriptionsLock.EnterWriteLock();
            try {
                _healthCheckSubscriptions.Remove(subscription);
            }
            finally {
                _healthCheckSubscriptionsLock.ExitWriteLock();
            }
        }


        /// <summary>
        /// Long-running task that monitors the <see cref="_recomputeHealthChannel"/>, 
        /// recalculates the adapter health when messages are published to the channel, and then 
        /// publishes he health status to subscribers.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that can be used to stop processing of the queue.
        /// </param>
        /// <returns>
        ///   A task that will complete when the cancellation token fires
        /// </returns>
        private async Task PublishToHealthCheckSubscribers(CancellationToken cancellationToken) {
            while (await _recomputeHealthChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!_recomputeHealthChannel.Reader.TryRead(out var val) || !val) {
                    continue;
                }

                HealthCheckSubscription[] subscribers;

                _healthCheckSubscriptionsLock.EnterReadLock();
                try {
                    subscribers = _healthCheckSubscriptions.ToArray();
                }
                finally {
                    _healthCheckSubscriptionsLock.ExitReadLock();
                }

                var update = await CheckHealthInternalAsync(
                    new DefaultAdapterCallContext(),
                    cancellationToken
                ).ConfigureAwait(false);

                if (subscribers.Length == 0) {
                    // No subscribers; no point recomputing health status.
                    continue;
                }

                foreach (var subscriber in subscribers) {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }

                    try {
                        var success = await subscriber.ValueReceived(update, cancellationToken).ConfigureAwait(false);
                        if (!success) {
                            Logger.LogTrace(Resources.Log_PublishToSubscriberWasUnsuccessful, subscriber.Context?.ConnectionId);
                        }
                    }
                    catch (OperationCanceledException) { }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                        Logger.LogError(e, Resources.Log_PublishToSubscriberThrewException, subscriber.Context?.ConnectionId);
                    }
                }
            }
        }


        /// <summary>
        /// Informs the adapter that its overall health status needs to be recomputed (for example, 
        /// due to a disconnection from an external system). Subscribers to health status updates 
        /// will receive the updated health status.
        /// </summary>
        protected internal void OnHealthStatusChanged() {
            _recomputeHealthChannel.Writer.TryWrite(true);
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
        protected virtual async Task<IEnumerable<HealthCheckResult>> CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
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
                            TaskScheduler.QueueBackgroundWorkItem(PublishToHealthCheckSubscribers, StopToken);
                        }
                        IsRunning = true;
                    }
                    finally {
                        if (!cancellationToken.IsCancellationRequested) {
                            _ = await CheckHealthInternalAsync(new DefaultAdapterCallContext(), cancellationToken).ConfigureAwait(false);
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
            CheckStarted();

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
        async Task<HealthCheckResult> IHealthCheck.CheckHealthAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
            if (!this.HasFeature<IHealthCheck>()) {
                // Implementer has removed the feature.
                throw new InvalidOperationException(Resources.Error_FeatureUnavailable);
            }

            var implementation = this.GetFeature<IHealthCheck>();
            if (implementation != this) {
                // Implementer has provided their own feature implementation.
                return await implementation.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false);
            }

            return await CheckHealthInternalAsync(context, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        async Task<IHealthCheckSubscription> IHealthCheckPush.Subscribe(IAdapterCallContext context) {
            if (!this.HasFeature<IHealthCheckPush>()) {
                // Implementer has removed the feature.
                throw new InvalidOperationException(Resources.Error_FeatureUnavailable);
            }

            var implementation = this.GetFeature<IHealthCheckPush>();
            if (implementation != this) {
                // Implementer has provided their own feature implementation.
                return await implementation.Subscribe(context).ConfigureAwait(false);
            }

            var subscription = new HealthCheckSubscription(context, this);

            bool added;
            HealthCheckResult? latestResult;

            _healthCheckSubscriptionsLock.EnterWriteLock();
            try {
                added = _healthCheckSubscriptions.Add(subscription);
                latestResult = _latestHealthCheck;
            }
            finally {
                _healthCheckSubscriptionsLock.ExitWriteLock();
            }

            if (added) {
                await subscription.Start().ConfigureAwait(false);
                if (latestResult != null) {
                    await subscription.ValueReceived(latestResult.Value).ConfigureAwait(false);
                }
            }

            return subscription;
        }


        /// <inheritdoc/>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <inheritdoc/>
        public async ValueTask DisposeAsync() {
            await DisposeAsync(true).ConfigureAwait(false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~AdapterBase() {
            Dispose(false);
        }


        /// <summary>
        /// Releases adapter resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the adapter is being disposed, or <see langword="false"/> 
        ///   if it is being finalized.
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (_isDisposed) {
                return;
            }

            if (disposing) {
                _stopTokenSource?.Cancel();
                _stopTokenSource?.Dispose();
                _features.Dispose();
                _properties.Clear();
                _loggerScope.Dispose();
                _startupLock.Dispose();
                _recomputeHealthChannel.Writer.TryComplete();
                _healthCheckSubscriptionsLock.EnterWriteLock();
                try {
                    foreach (var item in _healthCheckSubscriptions.ToArray()) {
                        item.Dispose();
                    }
                }
                finally {
                    _healthCheckSubscriptionsLock.ExitWriteLock();
                }
                _healthCheckSubscriptionsLock.Dispose();
                _healthCheckSubscriptions.Clear();
                _healthCheckUpdateLock.Dispose();
                _isDisposed = true;
            }
        }


        /// <summary>
        /// Asynchronously releases adapter resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the adapter is being disposed, or <see langword="false"/> 
        ///   if it is being finalized.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will perform the operation.
        /// </returns>
        /// <remarks>
        ///   The default implementation of this method calls <see cref="Dispose(bool)"/>. Override 
        ///   both this method and <see cref="Dispose(bool)"/> if your adapter requires a separate 
        ///   asynchronous resource cleanup implementation.
        /// </remarks>
        protected virtual ValueTask DisposeAsync(bool disposing) {
            Dispose(disposing);
            return default;
        }


        #region [ Inner Types ]

        /// <summary>
        /// <see cref="IHealthCheckSubscription"/> implementation.
        /// </summary>
        private class HealthCheckSubscription : AdapterSubscription<HealthCheckResult>, IHealthCheckSubscription {

            /// <summary>
            /// The subscribed adapter.
            /// </summary>
            private readonly AdapterBase _adapter;


            /// <summary>
            /// Creates a new <see cref="HealthCheckSubscription"/> object.
            /// </summary>
            /// <param name="context">
            ///   The <see cref="IAdapterCallContext"/> for the subscriber.
            /// </param>
            /// <param name="adapter">
            ///   The adapter that the caller is subscribing to.
            /// </param>
            internal HealthCheckSubscription(
                IAdapterCallContext context, 
                AdapterBase adapter
            ) : base(context, ((IAdapter) adapter).Descriptor.Id) {
                _adapter = adapter;
            }


            /// <inheritdoc/>
            protected override void OnCancelled() {
                _adapter.OnHealthCheckSubscriptionCancelled(this);
            }

        }

        #endregion

    }
}
