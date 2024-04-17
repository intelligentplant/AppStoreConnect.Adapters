using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#pragma warning disable RS0027 // API with optional parameter(s) should have the most parameters amongst its public overloads

namespace DataCore.Adapter {

    /// <summary>
    /// Base class for adapter implementations that use a strongly-typed options class.
    /// </summary>
    /// <typeparam name="TAdapterOptions">
    ///   The options type for the adapter.
    /// </typeparam>
    [AutomaticFeatureRegistration(true)]
    public abstract partial class AdapterBase<TAdapterOptions> : AdapterCore where TAdapterOptions : AdapterOptions, new() {

        #region [ Fields / Properties ]

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The <typeparamref name="TAdapterOptions"/> monitor subscription.
        /// </summary>
        private readonly IDisposable? _optionsMonitorSubscription;

        /// <summary>
        /// The adapter options.
        /// </summary>
        protected TAdapterOptions Options { get; private set; }

        /// <summary>
        /// The <see cref="HealthCheckManager{TAdapterOptions}"/> that provides the <see cref="IHealthCheck"/> feature.
        /// </summary>
        private readonly HealthCheckManager<TAdapterOptions> _healthCheckManager;

        #endregion

        #region [ Events ]

        /// <summary>
        /// Invoked when <see cref="Options"/> is modified.
        /// </summary>
        public event Action<TAdapterOptions>? OptionsChanged;

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
        /// <param name="loggerFactory">
        ///   The logger factory for the adapter. Can be <see langword="null"/>.
        /// </param>
        private AdapterBase(
            string id,
            IBackgroundTaskService? backgroundTaskService,
            ILoggerFactory? loggerFactory
        ) : base(new AdapterDescriptor(id, id, null), backgroundTaskService, loggerFactory) {
            if (string.IsNullOrWhiteSpace(id)) {
                throw new ArgumentException(Resources.Error_AdapterIdIsRequired);
            }

            if (id.Length > AdapterConstants.MaxIdLength) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterIdIsTooLong, AdapterConstants.MaxIdLength), nameof(id));
            }

            _logger = LoggerFactory.CreateLogger($"{typeof(AdapterBase).FullName}.{nameof(AdapterBase<TAdapterOptions>)}");

            Options = default!;
            _healthCheckManager = new HealthCheckManager<TAdapterOptions>(this);

            // Register default features.
            AddDefaultFeatures();

            // Automatically register features implemented directly on the adapter if required. 
            var autoRegisterFeatures = GetType().GetCustomAttribute<AutomaticFeatureRegistrationAttribute>(true);
            if (autoRegisterFeatures?.IsEnabled ?? true) {
                AddFeatures(this);
            }

            Started += adapter => {
                OnHealthStatusChanged();
                return Task.CompletedTask;
            };
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
        /// <param name="loggerFactory">
        ///   The logger factory for the adapter. Can be <see langword="null"/>.
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
            IBackgroundTaskService? backgroundTaskService,
            ILoggerFactory? loggerFactory
        ) : this(id, Microsoft.Extensions.Options.Options.Create(options ?? throw new ArgumentNullException(nameof(options))), backgroundTaskService, loggerFactory) { }


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
        /// <param name="loggerFactory">
        ///   The logger factory for the adapter. Can be <see langword="null"/>.
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
            IBackgroundTaskService? backgroundTaskService,
            ILoggerFactory? loggerFactory = null
        ) : this(id, backgroundTaskService, loggerFactory) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            var opts = options.Value;
            if (opts == null) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_NoOptionsFoundForAdapter, id));
            }
            Validator.ValidateObject(opts, new ValidationContext(opts), true);
            Options = opts;
            UpdateDescriptor(opts.Name, opts.Description);
            if (!Options.IsEnabled) {
                Disable();
            }
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
        /// <param name="loggerFactory">
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
            IBackgroundTaskService? backgroundTaskService,
            ILoggerFactory? loggerFactory
        ) : this(
            id,
            backgroundTaskService,
            loggerFactory
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
            UpdateDescriptor(options.Name, options.Description);
            if (!Options.IsEnabled) {
                Disable();
            }

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
                    LogAdapterOptionsUpdateInvalid(_logger, e, id);
                    return;
                }

                var previous = Options;
                Options = opts;
                OnOptionsChangeInternal(opts, previous);
            });
        }


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
        [Obsolete("Use an overload that accepts an ILoggerFactory instead.")]
        private AdapterBase(
            string id,
            IBackgroundTaskService? backgroundTaskService,
            ILogger? logger
        ) : base(new AdapterDescriptor(id, id, null), backgroundTaskService, logger) {
            if (string.IsNullOrWhiteSpace(id)) {
                throw new ArgumentException(Resources.Error_AdapterIdIsRequired);
            }

            if (id.Length > AdapterConstants.MaxIdLength) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterIdIsTooLong, AdapterConstants.MaxIdLength), nameof(id));
            }

            _logger = LoggerFactory.CreateLogger($"{typeof(AdapterBase).FullName}.{nameof(AdapterBase<TAdapterOptions>)}");

            Options = default!;
            _healthCheckManager = new HealthCheckManager<TAdapterOptions>(this);

            // Register default features.
            AddDefaultFeatures();

            // Automatically register features implemented directly on the adapter if required. 
            var autoRegisterFeatures = GetType().GetCustomAttribute<AutomaticFeatureRegistrationAttribute>(true);
            if (autoRegisterFeatures?.IsEnabled ?? true) {
                AddFeatures(this);
            }

            Started += adapter => {
                OnHealthStatusChanged();
                return Task.CompletedTask;
            };
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
        [Obsolete("Use an overload that accepts an ILoggerFactory instead.")]
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
        [Obsolete("Use an overload that accepts an ILoggerFactory instead.")]
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
            UpdateDescriptor(opts.Name, opts.Description);
            if (!Options.IsEnabled) {
                Disable();
            }
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
        ///   The logger for the adapter. Can be <see langword="null"/>.
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
        [Obsolete("Use an overload that accepts an ILoggerFactory instead.")]
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
            UpdateDescriptor(options.Name, options.Description);
            if (!Options.IsEnabled) {
                Disable();
            }

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
                    LogAdapterOptionsUpdateInvalid(_logger, e, id);
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
        /// Adds the default features for the adapter.
        /// </summary>
        private void AddDefaultFeatures() {
            AddFeature<IHealthCheck>(_healthCheckManager);
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

            if (newOptions.IsEnabled) {
                Enable();
            }
            else {
                Disable();
                return;
            }

            // Call the handler on the implementing class.

            OnOptionsChange(newOptions);
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

            foreach (var key in Features.Keys) {
                if (cancellationToken.IsCancellationRequested) {
                    return Array.Empty<HealthCheckResult>();
                }

                var feature = Features[key];

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

        #endregion

        #region [ Abstract / Virtual Methods ]

        /// <inheritdoc/>
        protected sealed override async Task StartAsyncCore(CancellationToken cancellationToken) {
            await _healthCheckManager.InitAsync(cancellationToken).ConfigureAwait(false);
            await StartAsync(cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected sealed override async Task StopAsyncCore(CancellationToken cancellationToken) {
            await StopAsync(cancellationToken).ConfigureAwait(false);
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
        ///   is called.
        /// </remarks>
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
        ///   implementation will return unhealthy status if <see cref="AdapterCore.IsRunning"/> is 
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
            OptionsChanged?.Invoke(options);
        }

        #endregion

        #region [ Disposable Pattern ]

        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);

            if (disposing) {
                _optionsMonitorSubscription?.Dispose();
                _healthCheckManager.Dispose();
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask DisposeAsyncCore() {
            await base.DisposeAsyncCore().ConfigureAwait(false);

            _optionsMonitorSubscription?.Dispose();
            _healthCheckManager.Dispose();
        }

        #endregion

        #region [ Logger Messages ]

        [LoggerMessage(100, LogLevel.Error, "Updated options for adapter '{id}' are not valid.")]
        static partial void LogAdapterOptionsUpdateInvalid(ILogger logger, Exception error, string id);

        #endregion

    }
}
#pragma warning restore RS0027 // API with optional parameter(s) should have the most parameters amongst its public overloads
