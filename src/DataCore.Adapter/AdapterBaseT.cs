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
    public abstract class AdapterBase<TAdapterOptions> : AdapterBase where TAdapterOptions : AdapterOptions {

        /// <summary>
        /// The <typeparamref name="TAdapterOptions"/> monitor subscription.
        /// </summary>
        private readonly IDisposable _optionsMonitorSubscription;

        /// <summary>
        /// The adapter options.
        /// </summary>
        protected TAdapterOptions Options { get; private set; }


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
        /// <param name="taskScheduler">
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
        protected AdapterBase(string id, TAdapterOptions options, IBackgroundTaskService taskScheduler, ILogger logger)
            : this(id, new AdapterOptionsMonitor<TAdapterOptions>(options), taskScheduler, logger) { }


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
        /// <param name="taskScheduler">
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
            IBackgroundTaskService taskScheduler, 
            ILogger logger
        ) : base(
            id, 
            optionsMonitor?.CurrentValue?.Name, 
            optionsMonitor?.CurrentValue.Description,
            taskScheduler,
            logger
        ) {
            if (optionsMonitor == null) {
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
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                    Logger.LogError(e, Resources.Log_InvalidAdapterOptionsUpdate);
                    return;
                }

                Options = opts;
                OnOptionsChangeInternal(opts);
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
        protected override async ValueTask DisposeAsync(bool disposing) {
            await base.DisposeAsync(disposing).ConfigureAwait(false);
            if (disposing) {
                _optionsMonitorSubscription?.Dispose();
            }
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

            var currentDescriptor = Descriptor;

            if (!string.Equals(options.Name, currentDescriptor.Name, StringComparison.Ordinal) || 
                !string.Equals(options.Description, currentDescriptor.Description, StringComparison.Ordinal)
            ) {
                UpdateDescriptor(options.Name, options.Description);
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

    }
}
