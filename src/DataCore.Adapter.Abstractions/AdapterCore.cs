using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter {

    /// <summary>
    /// Base implementation of <see cref="IAdapter"/>.
    /// </summary>
    public abstract partial class AdapterCore : IAdapter {

        /// <summary>
        /// Specifies if the adapter has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Logging.
        /// </summary>
        protected internal ILogger Logger { get; }

        /// <summary>
        /// Specifies if the adapter is currently running.
        /// </summary>
        private bool _isRunning;

        /// <summary>
        /// Specifies if <see cref="IAdapter.StartAsync"/> has been called at least once.
        /// </summary>
        private bool _hasStartBeenCalled;

        /// <summary>
        /// Ensures that only one startup attempt can occur at a time.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncLock _startupLock = new Nito.AsyncEx.AsyncLock();

        /// <summary>
        /// Ensures that only one shutdown attempt can occur at a time.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncLock _shutdownLock = new Nito.AsyncEx.AsyncLock();

        /// <summary>
        /// Manual reset event that is reset when a shutdown starts and is set when a shutdown 
        /// completes.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncManualResetEvent _shutdownInProgress = new Nito.AsyncEx.AsyncManualResetEvent(true); // Initial state is set

        /// <summary>
        /// Fires when <see cref="Dispose()"/> or <see cref="DisposeAsync"/> are called.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Adapter properties.
        /// </summary>
        private readonly ConcurrentDictionary<string, AdapterProperty> _properties = new ConcurrentDictionary<string, AdapterProperty>();

        /// <inheritdoc/>
        public AdapterDescriptor Descriptor { get; private set; }

        /// <inheritdoc/>
        public AdapterTypeDescriptor TypeDescriptor { get; }

        /// <inheritdoc/>
        public IAdapterFeaturesCollection Features => this;

        /// <inheritdoc/>
        public IEnumerable<AdapterProperty> Properties {
            get {
                CheckDisposed();
                return _properties.Values.OrderBy(x => x.Name).ToArray();
            }
        }

        /// <inheritdoc/>
        public bool IsEnabled { get; private set; }

        /// <inheritdoc/>
        public bool IsRunning => _isRunning && !_disposed;

        /// <summary>
        /// Indicates if the adapter is starting.
        /// </summary>
        protected bool IsStarting { get; private set; }

        /// <inheritdoc/>
        public IBackgroundTaskService BackgroundTaskService { get; }

        /// <summary>
        /// Fires when <see cref="IAdapter.StopAsync(CancellationToken)"/> is called.
        /// </summary>
        private CancellationTokenSource _stopTokenSource;

        /// <summary>
        /// Gets a cancellation token that will fire when the adapter is stopped.
        /// </summary>
        public CancellationToken StopToken => _stopTokenSource.Token;

        /// <inheritdoc/>
        public event Func<IAdapter, Task>? Started;

        /// <inheritdoc/>
        public event Func<IAdapter, Task>? Stopped;


        /// <summary>
        /// Creates a new <see cref="AdapterCore"/> instance.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor for the adapter.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> for the adapter.
        /// </param>
        /// <param name="logger">
        ///   The <see cref="ILogger"/> for the adapter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="descriptor"/> is <see langword="null"/>.
        /// </exception>
        protected AdapterCore(AdapterDescriptor descriptor, IBackgroundTaskService? backgroundTaskService = null, ILogger? logger = null) {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            TypeDescriptor = GetType().CreateAdapterTypeDescriptor()!;
            BackgroundTaskService = new BackgroundTaskServiceWrapper(backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default, _disposedTokenSource.Token);
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

            // Create initial stopped token source and cancel it immediately so that StopToken is
            // initially in a cancelled state.
            _stopTokenSource = new CancellationTokenSource();
            _stopTokenSource.Cancel();
        }


        /// <inheritdoc/>
        async Task IAdapter.StartAsync(CancellationToken cancellationToken) {
            _hasStartBeenCalled = true;

            if (!IsEnabled) {
                Logger.LogWarning(AbstractionsResources.Log_AdapterIsDisabled, Descriptor.Id);
                return;
            }

            CheckDisposed();
            using (await _startupLock.LockAsync(cancellationToken).ConfigureAwait(false)) {
                await _shutdownInProgress.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (IsRunning) {
                    return;
                }

                if (StopToken.IsCancellationRequested) {
                    _stopTokenSource = new CancellationTokenSource();
                }

                using (StartActivity(GetActivityName(nameof(IAdapter), nameof(IAdapter.StartAsync)))) {
                    Logger.LogInformation(AbstractionsResources.Log_StartingAdapter, Descriptor.Id);
                    IsStarting = true;
                    try {
                        using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, StopToken)) {
                            await StartAsyncCore(ctSource.Token).ConfigureAwait(false);
                            _isRunning = true;
                        }
                    }
                    finally {
                        IsStarting = false;
                    }

                    Telemetry.EventSource.AdapterStarted(Descriptor.Id);
                    Logger.LogInformation(AbstractionsResources.Log_StartedAdapter, Descriptor.Id);

                    BackgroundTaskService.QueueBackgroundWorkItem(OnStartedAsync, StopToken);
                    if (Started != null) {
                        await Started.Invoke(this).ConfigureAwait(false);
                    }
                }
            }
        }


        /// <inheritdoc/>
        async Task IAdapter.StopAsync(CancellationToken cancellationToken) {
            CheckDisposed();
            using (await _shutdownLock.LockAsync(cancellationToken).ConfigureAwait(false)) {
                if (!IsStarting && !IsRunning) {
                    return;
                }

                using (StartActivity(GetActivityName(nameof(IAdapter), nameof(IAdapter.StartAsync)))) {
                    _shutdownInProgress.Reset();

                    try {
                        Logger.LogInformation(AbstractionsResources.Log_StoppingAdapter, Descriptor.Id);
                        await StopAsyncCore(default).ConfigureAwait(false);
                        Telemetry.EventSource.AdapterStopped(Descriptor.Id);
                        Logger.LogInformation(AbstractionsResources.Log_StoppedAdapter, Descriptor.Id);

                        _isRunning = false;
                        _stopTokenSource.Cancel();

                        if (Stopped != null) {
                            await Stopped.Invoke(this).ConfigureAwait(false);
                        }
                    }
                    finally {
                        _shutdownInProgress.Set();
                    }
                }


            }
        }

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

        #region [ Helper Methods ]

        /// <summary>
        /// Enables the adapter.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if the adapter status changed from enabled to disabled, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        protected bool Enable() {
            if (IsEnabled) {
                return false;
            }

            IsEnabled = true;

            if (_hasStartBeenCalled && !(IsStarting || IsRunning)) {
                // The adapter was disabled but has now been enabled, and StartAsync has been
                // called at least once before, so we will restart the adapter automatically.

                BackgroundTaskService.QueueBackgroundWorkItem(async ct => {
                    await ((IAdapter) this).StartAsync(ct).ConfigureAwait(false);
                });
            }

            return true;
        }


        /// <summary>
        /// Disables the adapter.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if the adapter status changed from enabled to disabled, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        protected bool Disable() {
            if (!IsEnabled) {
                return false;
            }

            IsEnabled = false;

            if (IsStarting || IsRunning) {
                // The adapter is already running and has now been disabled, so we will stop the
                // adapter.

                BackgroundTaskService.QueueBackgroundWorkItem(async ct => {
                    await ((IAdapter) this).StopAsync(ct).ConfigureAwait(false);
                });
            }

            return true;
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
                Descriptor = new AdapterDescriptor(Descriptor.Id, name!, description ?? Descriptor.Description);
            }
            else if (description != null) {
                Descriptor = new AdapterDescriptor(Descriptor.Id, Descriptor.Name, description);
            }
        }


        /// <summary>
        /// Starts a new <see cref="System.Diagnostics.Activity"/> with the specified name.
        /// </summary>
        /// <param name="name">
        ///   The activity name.
        /// </param>
        /// <returns>
        ///   The <see cref="System.Diagnostics.Activity"/>, or <see langword="null"/> if there 
        ///   are no observers for <see cref="Telemetry.ActivitySource"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        protected internal System.Diagnostics.Activity? StartActivity(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentOutOfRangeException(nameof(name));
            }

            var result = Telemetry.ActivitySource.StartActivity(name);
            if (result != null) {
                result.AddTag("adapter_id", Descriptor.Id);
            }

            return result;
        }


        /// <summary>
        /// Starts an activity for the specified member on an adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="memberName">
        ///   The feature member name.
        /// </param>
        /// <returns>
        ///   The <see cref="System.Diagnostics.Activity"/>, or <see langword="null"/> if there 
        ///   are no observers for <see cref="Telemetry.ActivitySource"/>.
        /// </returns>
        internal System.Diagnostics.Activity? StartActivity<TFeature>(string memberName) where TFeature : IAdapterFeature {
            return StartActivity(GetActivityName(typeof(TFeature).Name, memberName));
        }


        /// <summary>
        /// Gets the activity name to use for the specified parts.
        /// </summary>
        /// <param name="parts">
        ///   The activity name parts.
        /// </param>
        /// <returns>
        ///   The activity name to use.
        /// </returns>
        private string GetActivityName(params string[] parts) => string.Join("/", parts);


        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the adapter has been disposed.
        /// </summary>
        protected internal void CheckDisposed() {
            if (_disposed) {
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
        protected internal void CheckStarted(bool allowStarting = false) {
            if (IsEnabled && (IsRunning || (allowStarting && IsStarting))) {
                return;
            }

            throw new InvalidOperationException(AbstractionsResources.Error_AdapterIsNotStarted);
        }


        /// <summary>
        /// Creates a new <see cref="CancellationTokenSource"/> that will cancel when the adapter 
        /// is disposed, its <see cref="StopToken"/> requests cancellation, or any of the 
        /// specified additional tokens request cancellation.
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
            CheckDisposed();
            return CancellationTokenSource.CreateLinkedTokenSource(new List<CancellationToken>(additionalTokens) { StopToken, _disposedTokenSource.Token }.ToArray());
        }

        #endregion

        #region [ Virtual / Abstract Methods ]

        /// <summary>
        /// Starts the adapter.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that represents the start operation.
        /// </returns>
        protected abstract Task StartAsyncCore(CancellationToken cancellationToken);


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
        ///   The <see cref="StopAsyncCore"/> method is intended to allow the same adapter to be 
        ///   started and stopped multiple times. Therefore, only resources that are created 
        ///   when <see cref="StartAsyncCore"/> is called should be disposed when <see cref="StopAsyncCore"/> 
        ///   is called. The <see cref="Dispose(bool)"/> and <see cref="DisposeAsyncCore"/> 
        ///   methods should be used to dispose of all resources, including those created by 
        ///   calls to <see cref="StartAsyncCore"/>.
        /// </remarks>
        protected abstract Task StopAsyncCore(CancellationToken cancellationToken);


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
        ///   objects passed into your adapter. The default behaviour in <see cref="AdapterCore"/>
        ///   is to ensure that the <paramref name="context"/> is not <see langword="null"/>.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="ValidateInvocation(IAdapterCallContext, object[])"/>
        protected virtual void ValidateContext(IAdapterCallContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
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
        ///   feature invocation. The default behaviour in <see cref="AdapterCore"/>
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
        ///   The default behaviour in <see cref="AdapterCore"/> is to validate the <paramref name="context"/> 
        ///   and <paramref name="invocationParameters"/> by calling <see cref="ValidateContext(IAdapterCallContext)"/> 
        ///   and <see cref="ValidateInvocationParameter(object)"/> respectively.
        /// </para>
        /// 
        /// </remarks>
        public virtual void ValidateInvocation(IAdapterCallContext context, params object[] invocationParameters) {
            ValidateContext(context);
            foreach (var item in invocationParameters) {
                ValidateInvocationParameter(item);
            }
        }

        #endregion

        #region [ IDisposable / IAsyncDisposable Implementation ]

        /// <inheritdoc/>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
            Telemetry.EventSource.AdapterDisposed(Descriptor.Id);
        }


        /// <inheritdoc/>
        public async ValueTask DisposeAsync() {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
            Telemetry.EventSource.AdapterDisposed(Descriptor.Id);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~AdapterCore() {
            Dispose(false);
        }


        /// <summary>
        /// Disposes of adapter resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the adapter is being disposed or <see langword="false"/> 
        ///   if it is being finalized.
        /// </param>
        /// <seealso cref="DisposeAsyncCore"/>
        protected virtual void Dispose(bool disposing) { 
            if (_disposed) {
                return;
            }

            if (disposing) {
                DisposeCommon();
                DisposeFeatures();
            }

            _disposed = true;
        }


        /// <summary>
        /// Asynchronously disposes of adapter resources.
        /// </summary>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will asynchronously dispose of adapter resources.
        /// </returns>
        /// <remarks>
        ///   Calls to <see cref="DisposeAsync"/> will call <see cref="DisposeAsyncCore"/> followed 
        ///   by <see cref="Dispose(bool)"/>, passing <see langword="false"/> as a parameter to the 
        ///   latter.
        /// </remarks>
        /// <seealso cref="Dispose(bool)"/>
        protected virtual async ValueTask DisposeAsyncCore() {
            if (_disposed) {
                return;
            }

            DisposeCommon();
            await DisposeFeaturesAsync().ConfigureAwait(false);
        }


        /// <summary>
        /// Disposes of items common to both <see cref="Dispose(bool)"/> and 
        /// <see cref="DisposeAsyncCore"/>.
        /// </summary>
        private void DisposeCommon() {
            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();
            _stopTokenSource?.Cancel();
            _stopTokenSource?.Dispose();
            _properties.Clear();
        }

        #endregion

    }
}
