using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Extensions;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataCore.Adapter {

    /// <summary>
    /// Base class for adapter implementations that use a strongly-typed options class.
    /// </summary>
    /// <typeparam name="TAdapterOptions">
    ///   The options type for the adapter.
    /// </typeparam>
    [AutomaticFeatureRegistration(true)]
    public abstract partial class AdapterBase<TAdapterOptions> : IAdapter where TAdapterOptions : AdapterOptions, new() {

        #region [ Fields / Properties ]

        /// <summary>
        /// Indicates if the adapter is disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Indicates if the resources disposed by <see cref="DisposeCommon"/> have been disposed.
        /// </summary>
        private bool _isDisposedCommon;

        /// <summary>
        /// Specifies if the adapter is currently running.
        /// </summary>
        private bool _isRunning;

        /// <summary>
        /// Logging.
        /// </summary>
        protected internal ILogger Logger { get; }

        /// <summary>
        /// Scope for the <see cref="Logger"/> that gets set when the adapter is created.
        /// </summary>
        private readonly IDisposable _loggerScope;

        /// <summary>
        /// The <see cref="AdapterEventSource"/> for the adapter.
        /// </summary>
        protected internal virtual AdapterEventSource EventSource => Telemetry.EventSource;

        /// <summary>
        /// The <typeparamref name="TAdapterOptions"/> monitor subscription.
        /// </summary>
        private readonly IDisposable? _optionsMonitorSubscription;

        /// <summary>
        /// The adapter options.
        /// </summary>
        protected TAdapterOptions Options { get; private set; }

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
        /// The <see cref="HealthCheckManager{TAdapterOptions}"/> that provides the <see cref="IHealthCheck"/> feature.
        /// </summary>
        private readonly HealthCheckManager<TAdapterOptions> _healthCheckManager;

        /// <summary>
        /// Adapter properties.
        /// </summary>
        private ConcurrentDictionary<string, AdapterProperty> _properties = new ConcurrentDictionary<string, AdapterProperty>();

        /// <inheritdoc/>
        public bool IsEnabled { get { return Options.IsEnabled; } }

        /// <inheritdoc/>
        public bool IsRunning => _isRunning && !_isDisposed;

        /// <inheritdoc/>
        public AdapterDescriptor Descriptor { get; private set; }

        /// <inheritdoc/>
        public AdapterTypeDescriptor TypeDescriptor { get; }

        /// <inheritdoc/>
        public IAdapterFeaturesCollection Features {
            get {
                CheckDisposed();
                return (IAdapterFeaturesCollection) this;
            }
        }

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

        /// <summary>
        /// The <see cref="System.Diagnostics.ActivitySource"/> that can be used to create 
        /// activities for the adapter.
        /// </summary>
        /// <remarks>
        ///   By default, the property returns <see cref="Telemetry.ActivitySource"/>. You don't 
        ///   need to override the property unless you want your adapter traces to be generated 
        ///   by a custom source.
        /// </remarks>
        protected virtual ActivitySource ActivitySource => Telemetry.ActivitySource;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="AdapterBase{TAdapterOptions}"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The logger for the adapter. Can be <see langword="null"/>.
        /// </param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private AdapterBase(string id, IBackgroundTaskService? backgroundTaskService, ILogger? logger) {
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            
            if (string.IsNullOrWhiteSpace(id)) {
                throw new ArgumentException(Resources.Error_AdapterIdIsRequired);
            }

            if (id.Length > AdapterConstants.MaxIdLength) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterIdIsTooLong, AdapterConstants.MaxIdLength), nameof(id));
            }

            StopToken = _stopTokenSource.Token;
            TypeDescriptor = this.CreateTypeDescriptor();
            BackgroundTaskService = new BackgroundTaskServiceWrapper(
                backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default,
                () => StopToken
            );
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            _loggerScope = Logger.BeginScope(id);

            _healthCheckManager = new HealthCheckManager<TAdapterOptions>(this);

            // Register default features.
            AddDefaultFeatures();

            // Automatically register features implemented directly on the adapter if required. 
            var autoRegisterFeatures = GetType().GetCustomAttribute<AutomaticFeatureRegistrationAttribute>(true);
            if (autoRegisterFeatures?.IsEnabled ?? true) {
                AddFeatures(this);
            }
        }


        /// <summary>
        /// Creates a new <see cref="AdapterBase{TAdapterOptions}"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="options">
        ///   The adapter options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The logger for the adapter. Can be <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is longer than <see cref="AdapterConstants.MaxIdLength"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   The <paramref name="options"/> are not valid.
        /// </exception>
        protected AdapterBase(
            string id,
            TAdapterOptions options,
            IBackgroundTaskService? backgroundTaskService = null,
            ILogger? logger = null
        ) : this(id, Microsoft.Extensions.Options.Options.Create(options ?? throw new ArgumentNullException(nameof(options))), backgroundTaskService, logger) { }


        /// <summary>
        /// Creates a new <see cref="AdapterBase{TAdapterOptions}"/> object that receives its 
        /// configuration from an <see cref="IOptions{TOptions}"/>.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="options">
        ///   The adapter options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The logger for the adapter. Can be <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is longer than <see cref="AdapterConstants.MaxIdLength"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The value of the <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   The value of the <paramref name="options"/> is not valid.
        /// </exception>
        protected AdapterBase(
            string id,
            IOptions<TAdapterOptions> options,
            IBackgroundTaskService? backgroundTaskService = null,
            ILogger? logger = null
        ) : this(id, backgroundTaskService, logger) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            var opts = options.Value;
            if (opts == null) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_NoOptionsFoundForAdapter, id));
            }
            Validator.ValidateObject(opts, new ValidationContext(opts), true);
            Options = opts;
            Descriptor = CreateDescriptor(id, opts);
        }


        /// <summary>
        /// Creates a new <see cref="AdapterBase{TAdapterOptions}"/> object that receives its 
        /// configuration from an <see cref="IOptionsMonitor{TOptions}"/> and can monitor for 
        /// configuration changes.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="optionsMonitor">
        ///   The monitor for the adapter's options type. The <see cref="IOptionsMonitor{TOptions}"/> 
        ///   key used is the supplied <paramref name="id"/>.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The logger factory for the adapter. Can be <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is longer than <see cref="AdapterConstants.MaxIdLength"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="optionsMonitor"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="optionsMonitor"/> does not contain an entry that can be used with this adapter.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   The initial options retrieved from <paramref name="optionsMonitor"/> are not valid.
        /// </exception>
        /// <remarks>
        ///   Note to implementers: override the <see cref="OnOptionsChange"/> method on your 
        ///   adapter implementation to receive notifications of options changes received from the 
        ///   <paramref name="optionsMonitor"/>.
        /// </remarks>
        protected AdapterBase(
            string id,
            IOptionsMonitor<TAdapterOptions> optionsMonitor, 
            IBackgroundTaskService? backgroundTaskService = null, 
            ILogger? logger = null
        ) : this(
            id, 
            backgroundTaskService,
            logger
        ) {
            if (optionsMonitor == null) {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            var options = optionsMonitor.Get(id);
            if (options == null) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_NoOptionsFoundForAdapter, id));
            }

            // Validate options.
            Validator.ValidateObject(
                options,
                new ValidationContext(options),
                true
            );

            Options = options;
            Descriptor = CreateDescriptor(id, options);

            _optionsMonitorSubscription = optionsMonitor.OnChange((opts, name) => {
                if (!string.Equals(name, id, StringComparison.Ordinal)) {
                    return;
                }

                // Validate updated options.
                try {
                    if (opts == null) {
                        throw new ArgumentNullException(nameof(opts));
                    }
                    Validator.ValidateObject(
                        opts,
                        new ValidationContext(opts),
                        true
                    );
                }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_InvalidAdapterOptionsUpdate);
                    return;
                }

                var previous = Options;
                Options = opts;
                OnOptionsChangeInternal(opts, previous);
            });
        }

        #endregion

        #region [ Helper Methods ]

        /// <summary>
        /// Creates a new adapter descriptor.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="options">
        ///   The adapter options.
        /// </param>
        /// <param name="nameOverride">
        ///   When specified, overrides the name supplied in the <paramref name="options"/>.
        /// </param>
        /// <param name="descriptionOverride">
        ///   When specified, overrides the description supplied in the <paramref name="options"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="AdapterDescriptor"/>.
        /// </returns>
        private static AdapterDescriptor CreateDescriptor(string id, TAdapterOptions? options, string? nameOverride = null, string? descriptionOverride = null) {
            return new AdapterDescriptor(
                id,
                string.IsNullOrWhiteSpace(nameOverride ?? options?.Name)
                    ? id
                    : nameOverride ?? options?.Name!,
                descriptionOverride ?? options?.Description
            );
        }


        /// <summary>
        /// Invoked when the adapter detects that its supplied <typeparamref name="TAdapterOptions"/> 
        /// have changed. This method will only be called if an <see cref="IOptionsMonitor{TOptions}"/> 
        /// was provided when the adapter was created.
        /// </summary>
        /// <param name="newOptions">
        ///   The updated options.
        /// </param>
        /// <param name="previousOptions">
        ///   The previous options.
        /// </param>
        private void OnOptionsChangeInternal(TAdapterOptions newOptions, TAdapterOptions previousOptions) {

            // Check if we need to update the descriptor.

            var currentDescriptor = Descriptor;

            if (!string.Equals(newOptions.Name, currentDescriptor.Name, StringComparison.Ordinal) ||
                !string.Equals(newOptions.Description, currentDescriptor.Description, StringComparison.Ordinal)
            ) {
                UpdateDescriptor(newOptions.Name, newOptions.Description);
            }

            if (newOptions.IsEnabled != previousOptions.IsEnabled) {
                if (!newOptions.IsEnabled && (IsStarting || IsRunning)) {
                    // The adapter is already running and has now been disabled.

                    var tcs = new TaskCompletionSource<bool>();

                    BackgroundTaskService.QueueBackgroundWorkItem(async ct => {
                        try {
                            await ((IAdapter) this).StopAsync(ct).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) {
                            tcs.TrySetCanceled(ct);
                        }
                        catch (Exception e) {
                            tcs.TrySetException(e);
                        }
                        finally {
                            tcs.TrySetResult(true);
                        }
                    });

                    tcs.Task.Wait();

                    // No need to call the handler on the implementing class, since we've just 
                    // stopped the adapter.
                    return;
                }
            }

            // Call the handler on the implementing class.

            OnOptionsChange(newOptions);
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
        /// Throws an <see cref="InvalidOperationException"/> if the adapter has not been started.
        /// </summary>
        /// <param name="allowStarting">
        ///   When <see langword="true"/>, an error will not be thrown if the adapter is currently 
        ///   starting.
        /// </param>
        protected void CheckStarted(bool allowStarting = false) {
            if (IsEnabled && (IsRunning || (allowStarting && IsStarting))) {
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
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is longer than <see cref="AdapterConstants.MaxNameLength"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="description"/> is longer than <see cref="AdapterConstants.MaxDescriptionLength"/>.
        /// </exception>
        private void UpdateDescriptor(string? name = null, string? description = null) {
            if (!string.IsNullOrWhiteSpace(name)) {
                if (name!.Length > AdapterConstants.MaxNameLength) {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterNameIsTooLong, AdapterConstants.MaxNameLength), nameof(name));
                }
                if (description != null && description.Length > AdapterConstants.MaxDescriptionLength) {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterDescriptionIsTooLong, AdapterConstants.MaxDescriptionLength), nameof(description));
                }

                Descriptor = CreateDescriptor(Descriptor.Id, null, name!, description ?? Descriptor.Description);
            }
            else if (description != null) {
                if (description.Length > AdapterConstants.MaxDescriptionLength) {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterDescriptionIsTooLong, AdapterConstants.MaxDescriptionLength), nameof(description));
                }

                Descriptor = CreateDescriptor(Descriptor.Id, null, Descriptor.Name, description);
            }
        }


        /// <summary>
        /// <strong>[INFRASTRUCTURE METHOD]</strong> 
        /// Validates the <see cref="IAdapterCallContext"/> passed to an adapter feature method.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   This method is not intended to be called directly from your adapter implementation; 
        ///   you should call <see cref="ValidateInvocation(IAdapterCallContext, object[])"/> 
        ///   instead when validating a call to your adapter.
        /// </para>
        /// 
        /// <para>
        ///   Override this method to customise the validation of <see cref="IAdapterCallContext"/> 
        ///   objects passed into your adapter. The default behaviour in <see cref="AdapterBase{TAdapterOptions}"/>
        ///   is to ensure that the <paramref name="context"/> is not <see langword="null"/>.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="ValidateInvocation(IAdapterCallContext, object[])"/>
        /// <seealso cref="ValidateInvocation{TFeature}(IAdapterCallContext, object[])"/>
        protected virtual void ValidateContext(IAdapterCallContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
        }


        /// <summary>
        /// <strong>[INFRASTRUCTURE METHOD]</strong>
        /// Ensures that the specified feature type is available on the adapter.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <exception cref="InvalidOperationException">
        ///   <typeparamref name="TFeature"/> is not available.
        /// </exception>
        /// <remarks>
        ///   In some scenarios, an adapter class might implement a given feature interface, but 
        ///   the feature itself might not be available at runtime. This method allows validation 
        ///   of a feature's availability when one if the feature's methods is called.
        /// </remarks>
        /// <seealso cref="ValidateInvocation(IAdapterCallContext, object[])"/>
        /// <seealso cref="ValidateInvocation{TFeature}(IAdapterCallContext, object[])"/>
        private void ValidateFeature<TFeature>() where TFeature : IAdapterFeature {
            if (!this.HasFeature<TFeature>()) {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, AbstractionsResources.Error_MissingAdapterFeature, typeof(TFeature).Name));
            }
        }


        /// <summary>
        /// <strong>[INFRASTRUCTURE METHOD]</strong> 
        /// Validates a parameter passed to an adapter feature method.
        /// </summary>
        /// <param name="parameter">
        ///   The request object for the invocation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="parameter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   <paramref name="parameter"/> fails validation.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   This method is not intended to be called directly from your adapter implementation; 
        ///   you should call <see cref="ValidateInvocation(IAdapterCallContext, object[])"/> 
        ///   instead when validating a call to your adapter.
        /// </para>
        /// 
        /// <para>
        ///   Override this method to customise the validation of parameters passed to an adapter 
        ///   feature invocation. The default behaviour in <see cref="AdapterBase{TAdapterOptions}"/>
        ///   is to ensure that the <paramref name="parameter"/> is not <see langword="null"/>, and 
        ///   to validate the <paramref name="parameter"/> by calling 
        ///   <see cref="Validator.ValidateObject(object, ValidationContext, bool)"/>.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="ValidateInvocation(IAdapterCallContext, object[])"/>
        protected virtual void ValidateInvocationParameter(object parameter) {
            if (parameter == null) {
                throw new ArgumentNullException(nameof(parameter));
            }
            Validator.ValidateObject(parameter, new ValidationContext(parameter), true);
        }


        /// <summary>
        /// Validates the invocation of an adapter feature method.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the invocation.
        /// </param>
        /// <param name="invocationParameters">
        ///   The invocation parameters to validate (such as request DTOs).
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Any item in <paramref name="invocationParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   Any item in <paramref name="invocationParameters"/> fails validation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The adapter has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The adapter is not running.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Call this method at the start of every adapter feature implementation method to 
        ///   validate the feature method's parameters.
        /// </para>
        /// 
        /// <para>
        ///   Override this method to perform any additional invocation checks required by your 
        ///   adapter.  
        /// </para>
        /// 
        /// <para>
        ///   The default behaviour in <see cref="AdapterBase{TAdapterOptions}"/> is to ensure that 
        ///   the adapter has not been disposed and that it is currently running, and to validate 
        ///   the <paramref name="context"/> and <paramref name="invocationParameters"/> by calling 
        ///   <see cref="ValidateContext(IAdapterCallContext)"/> and <see cref="ValidateInvocationParameter(object)"/> 
        ///   respectively.
        /// </para>
        /// 
        /// </remarks>
        public virtual void ValidateInvocation(IAdapterCallContext context, params object[] invocationParameters) {
            CheckDisposed();
            CheckStarted();
            ValidateContext(context);
            foreach (var item in invocationParameters) {
                ValidateInvocationParameter(item);
            }
        }


        /// <summary>
        /// Validates the invocation of an adapter feature method.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the invocation.
        /// </param>
        /// <param name="invocationParameters">
        ///   The invocation parameters to validate (such as request DTOs).
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   Any item in <paramref name="invocationParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   Any item in <paramref name="invocationParameters"/> fails validation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The adapter has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The adapter is not running.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Call this method at the start of every adapter feature implementation method to 
        ///   validate the availability of the feature, and the feature method's parameters.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="ValidateInvocation(IAdapterCallContext, object[])"/>
        public void ValidateInvocation<TFeature>(IAdapterCallContext context, params object[] invocationParameters) where TFeature : IAdapterFeature {
            ValidateFeature<TFeature>();
            ValidateInvocation(context, invocationParameters);
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

            foreach (var item in _featureLookup) {
                if (cancellationToken.IsCancellationRequested) {
                    return Array.Empty<HealthCheckResult>();
                }

                var key = item.Key;
                var feature = item.Value;

                if (feature == null || feature == this || !processedFeatures.Add(feature) || !(feature is IFeatureHealthCheck healthCheck)) {
                    continue;
                }

                var descriptor = feature.GetType().CreateFeatureDescriptor();

                var healthCheckName = string.Format(
                    context.CultureInfo,
                    Resources.HealthChecks_DisplayName_FeatureHealth,
                    descriptor?.DisplayName ?? key.ToString()
                );
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


        /// <summary>
        /// Creates a new <see cref="CancellationTokenSource"/> that will cancel when the 
        /// adapter's <see cref="StopToken"/> or any of the specified additional tokens 
        /// request cancellation.
        /// </summary>
        /// <param name="additionalTokens">
        ///   The additional cancellation tokens to monitor.
        /// </param>
        /// <returns>
        ///   A new <see cref="CancellationTokenSource"/> instance. Note that it is the caller's 
        ///   responsibility to dispose of the <see cref="CancellationTokenSource"/> when it is no 
        ///   longer required.
        /// </returns>
        public CancellationTokenSource CreateCancellationTokenSource(params CancellationToken[] additionalTokens) {
            return CancellationTokenSource.CreateLinkedTokenSource(new List<CancellationToken>(additionalTokens) { StopToken }.ToArray());
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
        /// <remarks>
        ///   The <see cref="StopAsync"/> method is intended to allow the same adapter to be 
        ///   started and stopped multiple times. Therefore, only resources that are created 
        ///   when <see cref="StartAsync"/> is called should be disposed when <see cref="StopAsync"/> 
        ///   is called. The <see cref="Dispose(bool)"/> and <see cref="DisposeAsyncCore"/> 
        ///   methods should be used to dispose of all resources, including those created by 
        ///   calls to <see cref="StartAsync"/>.
        /// </remarks>
        protected abstract Task StopAsync(CancellationToken cancellationToken);


        /// <summary>
        /// Called once the adapter has started.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will be run once the adapter has started.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   <see cref="OnStartedAsync(CancellationToken)"/> is run in the background using the 
        ///   adapter's <see cref="BackgroundTaskService"/>. The <paramref name="cancellationToken"/> 
        ///   will request cancellation when the adapter is stopped.
        /// </para>
        /// 
        /// <para>
        ///   The method can return a long-running task that runs until the adapter is stopped.
        /// </para>
        /// 
        /// </remarks>
        protected virtual Task OnStartedAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


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


        /// <summary>
        /// Override this method to receive notifications when the adapter's options have changed.
        /// </summary>
        /// <param name="options">
        ///   The updated options.
        /// </param>
        /// <remarks>
        ///   Note to implementers: this method is not called unless the adapter has been created 
        ///   using an <see cref="IOptionsMonitor{TOptions}"/> instance to supply the adapter 
        ///   options.
        /// </remarks>
        protected virtual void OnOptionsChange(TAdapterOptions options) {
            // Do nothing.
        }

        #endregion

        #region [ IAdapter Methods ]

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

                using (var activity = Telemetry.ActivitySource.StartActivity(ActivitySourceExtensions.GetActivityName(typeof(IAdapter), nameof(IAdapter.StartAsync)))) {
                    if (activity != null) {
                        activity.SetAdapterTag(this);
                    }

                    try {
                        Logger.LogInformation(Resources.Log_StartingAdapter, descriptorId);

                        IsStarting = true;
                        try {
                            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopTokenSource.Token)) {
                                await StartAsync(ctSource.Token).ConfigureAwait(false);
                                _isRunning = true;
                                await _healthCheckManager.Init(ctSource.Token).ConfigureAwait(false);
                            }
                        }
                        finally {
                            IsStarting = false;
                        }

                        EventSource.AdapterStarted(descriptorId);
                        Logger.LogInformation(Resources.Log_StartedAdapter, descriptorId);
                    }
                    catch (Exception e) {
                        Logger.LogError(e, Resources.Log_AdapterStartupError, descriptorId);
                        throw;
                    }
                }
            }
            finally {
                // If startup has been cancelled because the adapter is being disposed, releasing 
                // the lock will throw an ObjectDisposedException.
                if (!_isDisposed) {
                    _startupLock.Release();
                }
            }

            BackgroundTaskService.QueueBackgroundWorkItem(OnStartedAsync);
        }


        /// <inheritdoc/>
        async Task IAdapter.StopAsync(CancellationToken cancellationToken) {
            CheckDisposed();
            if (!IsStarting && !IsRunning) {
                return;
            }

            var descriptorId = Descriptor.Id;

            try {
                using (var activity = Telemetry.ActivitySource.StartActivity(ActivitySourceExtensions.GetActivityName(typeof(IAdapter), nameof(IAdapter.StopAsync)))) {
                    if (activity != null) {
                        activity.SetAdapterTag(this);
                    }
                    Logger.LogInformation(Resources.Log_StoppingAdapter, descriptorId);
                    _stopTokenSource.Cancel();
                    await StopAsync(cancellationToken).ConfigureAwait(false);
                    EventSource.AdapterStopped(descriptorId);
                    Logger.LogInformation(Resources.Log_StoppedAdapter, descriptorId);
                }
            }
            catch (Exception e) {
                Logger.LogError(e, Resources.Log_AdapterStopError, descriptorId);
                throw;
            }
            finally {
                _stopTokenSource = new CancellationTokenSource();
                _isRunning = false;
            }
        }

        #endregion

        #region [ Disposable Pattern ]

        /// <inheritdoc/>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
            EventSource.AdapterDisposed(Descriptor.Id);
        }



        /// <inheritdoc/>
        public async ValueTask DisposeAsync() {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
            EventSource.AdapterDisposed(Descriptor.Id);
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

            _optionsMonitorSubscription?.Dispose();
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
        /// <remarks>
        ///   Override both this method and <see cref="DisposeAsyncCore"/> if your adapter requires 
        ///   a separate asynchronous resource cleanup implementation. When calling <see cref="DisposeAsync"/>, 
        ///   both <see cref="DisposeAsyncCore"/> and <see cref="Dispose(bool)"/> will be called. 
        ///   The call to <see cref="Dispose(bool)"/> will be passed <see langword="false"/> when 
        ///   the object is being disposed asynchronously.
        /// </remarks>
        /// <seealso cref="DisposeAsyncCore"/>
        protected virtual void Dispose(bool disposing) {
            if (_isDisposed) {
                return;
            }

            if (disposing) {
                DisposeCommon();
                DisposeFeatures();
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
        /// <seealso cref="Dispose(bool)"/>
        protected virtual async ValueTask DisposeAsyncCore() {
            DisposeCommon();
            await DisposeFeaturesAsync().ConfigureAwait(false);
        }

        #endregion

    }
}
