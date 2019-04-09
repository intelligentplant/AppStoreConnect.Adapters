using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Service for registering background tasks for execution.
    /// </summary>
    public interface IBackgroundTaskQueue {

        /// <summary>
        /// Queues the specified background work item.
        /// </summary>
        /// <param name="workItem">
        ///   The background work item.
        /// </param>
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

        /// <summary>
        /// Dequeues a background work item for processing.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that can be used to cancel the wait operation.
        /// </param>
        /// <returns>
        ///   A background work item to be executed.
        /// </returns>
        Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);

    }
}
