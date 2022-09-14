#if NETFRAMEWORK

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.AspNetCore.Internal {

    internal partial class AdapterInitializer : IHostedService {

        /// <summary>
        /// Cancellation token source that will fire when <see cref="StopAsync(CancellationToken)"/> 
        /// is called.
        /// </summary>
        private readonly CancellationTokenSource _ctSource = new CancellationTokenSource();

        /// <summary>
        /// Long-running task initialised by <see cref="StartAsync(CancellationToken)"/>.
        /// </summary>
        private Task? _task;


        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken) {
            _task ??= Task.Run(async () => { 
                try {
                    await RunAsync(_ctSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            });
            return Task.CompletedTask;
        }


        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken) {
            if (_task == null) {
                return;
            }

            _ctSource.Cancel();
            await _task.WithCancellation(cancellationToken).ConfigureAwait(false);
        }
    }
}

#endif
