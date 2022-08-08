#if NETCOREAPP

using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace DataCore.Adapter.AspNetCore {

    internal partial class AdapterInitializer : BackgroundService {

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await RunAsync(stoppingToken).ConfigureAwait(false);
        }

    }
}

#endif
