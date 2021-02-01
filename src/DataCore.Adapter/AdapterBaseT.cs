using System;
using System.Threading.Tasks;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter {

    /// <summary>
    /// Base class for adapter implementations that use a strongly-typed options class.
    /// </summary>
    /// <typeparam name="TAdapterOptions">
    ///   The options type for the adapter.
    /// </typeparam>
    public abstract class AdapterBase<TAdapterOptions> : AdapterBase where TAdapterOptions : AdapterOptions, new() {

        /// <summary>
        /// The <typeparamref name="TAdapterOptions"/> monitor subscription.
        /// </summary>
        private readonly IDisposable _optionsMonitorSubscription;

        /// <summary>
        /// The adapter options.
        /// </summary>
        protected TAdapterOptions Options { get; private set; }

        /// <inheritdoc/>
        protected override bool IsEnabled {
            get { return Options.IsEnabled; }
        }


        /// <summary>
        /// Creates a new <see cref="Adapter"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID. Specify <see langword="null"/> or white space to generate an ID 
        ///   automatically.
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   The <paramref name="options"/> are not valid.
        /// </exception>
        protected AdapterBase(
            string id, 
            TAdapterOptions options, 
            IBackgroundTaskService? backgroundTaskService = null, 
            ILogger? logger = null
        ) : this(id, new AdapterOptionsMonitor<TAdapterOptions>(options), backgroundTaskService, logger) { }



        /// <summary>
        /// Creates a new <see cref="Adapter"/> object that can monitor for changes in 
        /// configuration. Note that changes in the adapter's ID will be ignored once the adapter 
        /// has been created.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID. Specify <see langword="null"/> or white space to generate an ID 
        ///   automatically.
        /// </param>
        /// <param name="optionsMonitor">
        ///   The monitor for the adapter's options type.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The logger factory for the adapter. Can be <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="optionsMonitor"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   The initial options retrieved from <paramref name="optionsMonitor"/> are not valid.
        /// </exception>
        protected AdapterBase(
            string id,
            IAdapterOptionsMonitor<TAdapterOptions> optionsMonitor, 
            IBackgroundTaskService? backgroundTaskService = null, 
            ILogger? logger = null
        ) : base(
            id, 
            optionsMonitor?.CurrentValue,
            backgroundTaskService,
            logger
        ) {
            if (optionsMonitor?.CurrentValue == null) {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            var options = optionsMonitor.CurrentValue;

            // Validate options.
            System.ComponentModel.DataAnnotations.Validator.ValidateObject(
                options,
                new System.ComponentModel.DataAnnotations.ValidationContext(options),
                true
            );

            Options = options;
            
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

                var previous = Options;
                Options = opts;
                OnOptionsChangeInternal(opts, previous);
            });
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing) {
                _optionsMonitorSubscription?.Dispose();
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask DisposeAsyncCore() {
            await base.DisposeAsyncCore().ConfigureAwait(false);
            _optionsMonitorSubscription?.Dispose();
        }



        /// <summary>
        /// Invoked when the adapter detects that its supplied <typeparamref name="TAdapterOptions"/> 
        /// have changed. This method will only be called if an <see cref="IAdapterOptionsMonitor{TAdapterOptions}"/> 
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
        /// Override this method in a subclass to receive notifications when the adapter's options 
        /// have changed.
        /// </summary>
        /// <param name="options">
        ///   The updated options.
        /// </param>
        protected virtual void OnOptionsChange(TAdapterOptions options) {
            // Do nothing.
        }

    }
}
