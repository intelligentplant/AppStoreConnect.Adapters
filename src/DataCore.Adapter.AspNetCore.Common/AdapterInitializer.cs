using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// Background service that starts registered adapters at startup time and stops them at shutdown 
    /// time.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via dependency injection")]
    internal class AdapterInitializer : IHostedService {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// For accessing the available adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="AdapterInitializer"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The adapter accessor service.
        /// </param>
        /// <param name="logger">
        ///   The logger for the service.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterAccessor"/> is <see langword="null"/>.
        /// </exception>
        public AdapterInitializer(IAdapterAccessor adapterAccessor, ILogger<AdapterInitializer> logger) {
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
            _logger = logger ?? (ILogger) Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        }


        /// <summary>
        /// Starts the adapters.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will start the registered adapters.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Adapter startup failure should not propagate")]
        public Task StartAsync(CancellationToken cancellationToken) {
            return Task.Run(async () => {
                var adapters = await _adapterAccessor.GetAllAdapters(new DefaultAdapterCallContext(), cancellationToken).ConfigureAwait(false);
                foreach (var adapter in adapters) {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }
                    if (!adapter.IsEnabled) {
                        continue;
                    }

                    try { 
                        _logger.LogDebug(Resources.Log_StartingAdapter, adapter.Descriptor.Name, adapter.Descriptor.Id);
                        await adapter.StartAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) {
                        _logger.LogError(e, Resources.Log_AdapterStartError, adapter.Descriptor.Name, adapter.Descriptor.Id);
                    }
                }
            }, cancellationToken);
        }



        /// <summary>
        /// Stops the adapters.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will stop the registered adapters.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Adapter shutdown failure should not propagate")]
        public async Task StopAsync(CancellationToken cancellationToken) {
            var adapters = await _adapterAccessor.GetAllAdapters(new DefaultAdapterCallContext(), cancellationToken).ConfigureAwait(false);
            await Task.WhenAll(adapters.Where(x => x.IsRunning).Select(async x => { 
                try {
                    _logger.LogDebug(Resources.Log_StoppingAdapter, x.Descriptor.Name, x.Descriptor.Id);
                    await x.StopAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) { 
                    _logger.LogError(e, Resources.Log_AdapterStopError, x.Descriptor.Name, x.Descriptor.Id);
                }
            })).WithCancellation(cancellationToken).ConfigureAwait(false);
        }
    }
}
