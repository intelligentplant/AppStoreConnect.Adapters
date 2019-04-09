using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Default <see cref="IBackgroundTaskQueue"/> implementation.
    /// </summary>
    public class BackgroundTaskQueue : IBackgroundTaskQueue {

        /// <summary>
        /// The queued work items.
        /// </summary>
        private readonly ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new ConcurrentQueue<Func<CancellationToken, Task>>();

        /// <summary>
        /// Signals to <see cref="DequeueAsync(CancellationToken)"/> that there is an item available 
        /// to dequeue.
        /// </summary>
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);


        /// <inheritdoc/>
        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem) {
            if (workItem == null) {
                throw new ArgumentNullException(nameof(workItem));
            }

            _workItems.Enqueue(workItem);
            _signal.Release();
        }


        /// <inheritdoc/>
        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken) {
            await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }
    }
}
