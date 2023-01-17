using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// Implements <see cref="IAdapterLifetime"/> using callback functions.
    /// </summary>
    public class AdapterLifetime : IAdapterLifetime {

        /// <summary>
        /// The started callback function.
        /// </summary>
        private readonly Func<IAdapter, CancellationToken, Task>? _started;

        /// <summary>
        /// The stopped callback function.
        /// </summary>
        private readonly Func<IAdapter, CancellationToken, Task>? _stopped;


        /// <summary>
        /// Creates a new <see cref="AdapterLifetime"/> instance using the specified callbacks.
        /// </summary>
        /// <param name="started">
        ///   The started callback to use.
        /// </param>
        /// <param name="stopped">
        ///   The stopped callback to use.
        /// </param>
        public AdapterLifetime(
            Func<IAdapter, CancellationToken, Task>? started = null, 
            Func<IAdapter, CancellationToken, Task>? stopped = null
        ) {
            _started = started;
            _stopped = stopped;
        }


        /// <inheritdoc/>
        public async Task StartedAsync(IAdapter adapter, CancellationToken cancellationToken) {
            if (_started != null) {
                await _started.Invoke(adapter, cancellationToken).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        public async Task StoppedAsync(IAdapter adapter, CancellationToken cancellationToken) {
            if (_stopped != null) {
                await _stopped.Invoke(adapter, cancellationToken).ConfigureAwait(false);
            }
        }

    }
}
