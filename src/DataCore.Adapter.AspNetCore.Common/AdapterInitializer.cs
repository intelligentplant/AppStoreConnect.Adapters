using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// Background service that starts registered adapters at startup time and stops them at shutdown 
    /// time.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via dependency injection")]
    internal partial class AdapterInitializer {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// For accessing the available adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// Services that perform started/stopped actions on adapters.
        /// </summary>
        private readonly IEnumerable<IAdapterLifetime> _lifetimeServices;


        /// <summary>
        /// Creates a new <see cref="AdapterInitializer"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The adapter accessor service.
        /// </param>
        /// <param name="lifetimeServices">
        ///   Services that perform started/stopped actions on adapters.
        /// </param>
        /// <param name="logger">
        ///   The logger for the service.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterAccessor"/> is <see langword="null"/>.
        /// </exception>
        public AdapterInitializer(IAdapterAccessor adapterAccessor, IEnumerable<IAdapterLifetime> lifetimeServices, ILogger<AdapterInitializer> logger) {
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
            _lifetimeServices = lifetimeServices ?? Array.Empty<IAdapterLifetime>();
            _logger = logger ?? (ILogger) Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        }


        /// <summary>
        /// Runs the <see cref="AdapterInitializer"/> service.
        /// </summary>
        /// <param name="stoppingToken">
        ///   The <see cref="CancellationToken"/> that will request cancellation when the service 
        ///   is being stopped.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will run the service.
        /// </returns>
        private async Task RunAsync(CancellationToken stoppingToken) {
            var context = new DefaultAdapterCallContext();
            
            try {
                await foreach (var adapter in _adapterAccessor.GetAllAdapters(context, stoppingToken).ConfigureAwait(false)) {
                    if (stoppingToken.IsCancellationRequested) {
                        break;
                    }

                    adapter.Started += async _ => {
                        foreach (var item in _lifetimeServices) {
                            await item.StartedAsync(adapter, stoppingToken).ConfigureAwait(false);
                        }
                    };
                    adapter.Stopped += async _ => {
                        foreach (var item in _lifetimeServices) {
                            await item.StoppedAsync(adapter, stoppingToken).ConfigureAwait(false);
                        }
                    };

                    try {
                        _logger.LogDebug(Resources.Log_StartingAdapter, adapter.Descriptor.Name, adapter.Descriptor.Id);
                        await adapter.StartAsync(stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException e) {
                        if (stoppingToken.IsCancellationRequested) {
                            throw;
                        }
                        _logger.LogError(e, Resources.Log_AdapterStartError, adapter.Descriptor.Name, adapter.Descriptor.Id);
                    }
                    catch (Exception e) {
                        _logger.LogError(e, Resources.Log_AdapterStartError, adapter.Descriptor.Name, adapter.Descriptor.Id);
                    }
                }

                await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            }
            finally {
                using (var ctSource = new CancellationTokenSource(TimeSpan.FromSeconds(10))) {
                    await foreach (var adapter in _adapterAccessor.GetAllAdapters(context, ctSource.Token).ConfigureAwait(false)) {
                        if (!adapter.IsRunning) {
                            continue;
                        }

                        try {
                            _logger.LogDebug(Resources.Log_StoppingAdapter, adapter.Descriptor.Name, adapter.Descriptor.Id);
                            await adapter.StopAsync(ctSource.Token).ConfigureAwait(false);
                        }
                        catch (Exception e) {
                            _logger.LogError(e, Resources.Log_AdapterStopError, adapter.Descriptor.Name, adapter.Descriptor.Id);
                        }
                    }
                }
            }
        }

    }
}
