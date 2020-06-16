using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// Background service that starts registered adapters at startup time and stops them at shutdown 
    /// time.
    /// </summary>
    internal class AdapterInitializer : IHostedService {

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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterAccessor"/> is <see langword="null"/>.
        /// </exception>
        public AdapterInitializer(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
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
        public Task StartAsync(CancellationToken cancellationToken) {
            return Task.Run(async () => {
                var adapters = await _adapterAccessor.GetAllAdapters(null, cancellationToken).ConfigureAwait(false);
                await Task.WhenAll(adapters.Where(x => x.IsEnabled).Select(x => x.StartAsync(cancellationToken))).WithCancellation(cancellationToken).ConfigureAwait(false);
            });
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
        public async Task StopAsync(CancellationToken cancellationToken) {
            var adapters = await _adapterAccessor.GetAllAdapters(null, cancellationToken).ConfigureAwait(false);
            await Task.WhenAll(adapters.Select(x => x.StopAsync(cancellationToken))).WithCancellation(cancellationToken).ConfigureAwait(false);
        }
    }
}
