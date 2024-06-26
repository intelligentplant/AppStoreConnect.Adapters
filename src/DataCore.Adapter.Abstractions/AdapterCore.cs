﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Logging;

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
        /// The logger factory for the adapter.
        /// </summary>
        /// <remarks>
        ///   All <see cref="ILogger"/> instances created by the factory define a scope that 
        ///   specifies the adapter's ID.
        /// </remarks>
        protected internal ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// The logger for the adapter.
        /// </summary>
        private readonly ILogger<AdapterCore> _logger;

        /// <summary>
        /// The logger for the adapter.
        /// </summary>
        [Obsolete("Use LoggerFactory to create ILogger instances instead.")]
        protected internal ILogger Logger => _logger;

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
        /// Fires when <see cref="Dispose()"/> or <see cref="DisposeAsync"/> are called.
        /// </summary>
        private readonly CancellationToken _disposedToken;

        /// <summary>
        /// Adapter properties.
        /// </summary>
        private readonly ConcurrentDictionary<string, AdapterProperty> _properties = new ConcurrentDictionary<string, AdapterProperty>();

        /// <inheritdoc/>
        public AdapterDescriptor Descriptor { get; private set; }

        /// <inheritdoc/>
        public AdapterTypeDescriptor TypeDescriptor => GetAdapterTypeDescriptor() ?? DefaultTypeDescriptor;

        /// <summary>
        /// The default adapter type descriptor generated from the adapter type.
        /// </summary>
        private AdapterTypeDescriptor DefaultTypeDescriptor => _defaultTypeDescriptor ??= GetType().CreateAdapterTypeDescriptor()!;

        /// <summary>
        /// The default adapter type descriptor generated from the adapter type.
        /// </summary>
        private AdapterTypeDescriptor? _defaultTypeDescriptor;

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
        public CancellationToken StopToken { get; private set; }

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
        /// <param name="loggerFactory">
        ///   The <see cref="ILoggerFactory"/> for the adapter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="descriptor"/> is <see langword="null"/>.
        /// </exception>
        protected AdapterCore(AdapterDescriptor descriptor, IBackgroundTaskService? backgroundTaskService, ILoggerFactory? loggerFactory) {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            _disposedToken = _disposedTokenSource.Token;
            BackgroundTaskService = new BackgroundTaskServiceWrapper(backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default, _disposedToken);

            LoggerFactory = loggerFactory ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;

            _logger = LoggerFactory.CreateLogger<AdapterCore>();

            // Create initial stopped token source and cancel it immediately so that StopToken is
            // initially in a cancelled state.
            CreateStopToken();
            _stopTokenSource!.Cancel();

            Enable();
        }


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
        /// <remarks>
        ///   Using this constructor overload will assign a <see cref="LoggerFactory"/> property 
        ///   that always returns the specified <paramref name="logger"/>.
        /// </remarks>
#pragma warning disable RS0027 // API with optional parameter(s) should have the most parameters amongst its public overloads
        [Obsolete("Use an overload that accepts an ILoggerFactory instead.")]
        protected AdapterCore(AdapterDescriptor descriptor, IBackgroundTaskService? backgroundTaskService = null, ILogger? logger = null) {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            _disposedToken = _disposedTokenSource.Token;
            BackgroundTaskService = new BackgroundTaskServiceWrapper(backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default, _disposedToken);

            LoggerFactory = new WrapperLoggerFactory(logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
            _logger = LoggerFactory.CreateLogger<AdapterCore>();

            // Create initial stopped token source and cancel it immediately so that StopToken is
            // initially in a cancelled state.
            CreateStopToken();
            _stopTokenSource!.Cancel();

            Enable();
        }
#pragma warning restore RS0027 // API with optional parameter(s) should have the most parameters amongst its public overloads


        /// <summary>
        /// Gets the type descriptor for the adapter.
        /// </summary>
        /// <returns>
        ///   The type descriptor for the adapter, or <see langword="null"/> if a type descriptor 
        ///   should be inferred from the adapter type.
        /// </returns>
        protected virtual AdapterTypeDescriptor? GetAdapterTypeDescriptor() {
            return null;
        }


        /// <summary>
        /// Creates an object to use as the state for a logger scope.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <returns>
        ///   An object to use as the state for a logger scope.
        /// </returns>
        private static object CreateLoggerState(string adapterId) {
            return new Dictionary<string, object?>() {
                ["AdapterId"] = adapterId
            };
        }


        /// <summary>
        /// Creates an object to use as the state for a logger scope.
        /// </summary>
        /// <returns>
        ///   An object to use as the state for a logger scope.
        /// </returns>
        private object CreateLoggerState() => CreateLoggerState(Descriptor.Id);


        /// <summary>
        /// Begins a logger scope for an adapter.
        /// </summary>
        /// <param name="logger">
        ///   The logger to begin the scope for.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <returns>
        ///   An <see cref="IDisposable"/> that ends the scope when disposed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        public static IDisposable? BeginLoggerScope(ILogger logger, string adapterId) {
            if (logger == null) {
                throw new ArgumentNullException(nameof(logger));
            }
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentOutOfRangeException(nameof(adapterId));
            }

            return logger.BeginScope(CreateLoggerState(adapterId));
        }


        /// <summary>
        /// Begins a logger scope for an adapter.
        /// </summary>
        /// <param name="logger">
        ///   The logger to begin the scope for.
        /// </param>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   An <see cref="IDisposable"/> that ends the scope when disposed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        internal static IDisposable? BeginLoggerScope(ILogger logger, IAdapter adapter) => BeginLoggerScope(logger, adapter?.Descriptor.Id ?? throw new ArgumentNullException(nameof(adapter)));


        /// <summary>
        /// Begins a logger scope for the adapter.
        /// </summary>
        /// <param name="logger">
        ///   The logger to begin the scope for.
        /// </param>
        /// <returns>
        ///   An <see cref="IDisposable"/> that ends the scope when disposed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   The state for the scope is an object that specifies the adapter ID.
        /// </remarks>
        protected internal IDisposable? BeginLoggerScope(ILogger logger) => BeginLoggerScope(logger, this);


        /// <summary>
        /// Begins a logger scope for the adapter's default logger.
        /// </summary>
        /// <returns>
        ///   An <see cref="IDisposable"/> that ends the scope when disposed.
        /// </returns>
        internal IDisposable? BeginLoggerScope() => BeginLoggerScope(_logger);


        /// <summary>
        /// Creates a new <see cref="CancellationTokenSource"/> that will cancel when the adapter 
        /// is stopped or disposed.
        /// </summary>
        private void CreateStopToken() {
            _stopTokenSource?.Cancel();
            _stopTokenSource?.Dispose();
            _stopTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_disposedToken);
            StopToken = _stopTokenSource.Token;
        }


        /// <inheritdoc/>
        async Task IAdapter.StartAsync(CancellationToken cancellationToken) {
            using var scope = BeginLoggerScope();

            _hasStartBeenCalled = true;

            if (!IsEnabled) {
                LogAdapterDisabled(_logger, Descriptor.Id);
                return;
            }

            CheckDisposed();

            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposedToken))
            using (await _startupLock.LockAsync(ctSource.Token).ConfigureAwait(false)) {
                await _shutdownInProgress.WaitAsync(ctSource.Token).ConfigureAwait(false);

                if (IsRunning) {
                    return;
                }

                if (StopToken.IsCancellationRequested) {
                    CreateStopToken();
                }

                using (StartActivity(GetActivityName(nameof(IAdapter), nameof(IAdapter.StartAsync)))) {
                    LogAdapterStarting(_logger, Descriptor.Id);
                    IsStarting = true;
                    try {
                        using (var ctSource2 = CancellationTokenSource.CreateLinkedTokenSource(ctSource.Token, StopToken)) {
                            await StartAsyncCore(ctSource2.Token).ConfigureAwait(false);
                            _isRunning = true;
                        }
                    }
                    finally {
                        IsStarting = false;
                    }

                    Telemetry.EventSource.AdapterStarted(Descriptor.Id);
                    LogAdapterStarted(_logger, Descriptor.Id);

                    BackgroundTaskService.QueueBackgroundWorkItem(OnStartedAsync, StopToken);
                    if (Started != null) {
                        await Started.Invoke(this).ConfigureAwait(false);
                    }
                }
            }
        }


        /// <inheritdoc/>
        async Task IAdapter.StopAsync(CancellationToken cancellationToken) {
            using var scope = BeginLoggerScope();

            CheckDisposed();

            using (await _shutdownLock.LockAsync(cancellationToken).ConfigureAwait(false)) {
                if (!IsStarting && !IsRunning) {
                    return;
                }

                using (StartActivity(GetActivityName(nameof(IAdapter), nameof(IAdapter.StopAsync)))) {
                    _shutdownInProgress.Reset();

                    try {
                        LogAdapterStopping(_logger, Descriptor.Id);
                        await StopAsyncCore(default).ConfigureAwait(false);
                        Telemetry.EventSource.AdapterStopped(Descriptor.Id);
                        LogAdapterStopped(_logger, Descriptor.Id);

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
            return CancellationTokenSource.CreateLinkedTokenSource(new List<CancellationToken>(additionalTokens) { StopToken }.ToArray());
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
                try {
                    DisposeFeatures();
                }
                finally {
                    DisposeCommon();
                }
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

            try {
                await DisposeFeaturesAsync().ConfigureAwait(false);
            }
            finally {
                DisposeCommon();
            }
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
