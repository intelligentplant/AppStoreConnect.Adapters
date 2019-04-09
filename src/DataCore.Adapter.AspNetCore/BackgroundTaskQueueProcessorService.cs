using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// Service for processing background tasks queued via an <see cref="IBackgroundTaskQueue"/>.
    /// </summary>
    public class BackgroundTaskQueueProcessorService : BackgroundService {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The task queue.
        /// </summary>
        public IBackgroundTaskQueue TaskQueue { get; }


        /// <summary>
        /// Creates a new <see cref="BackgroundTaskQueueProcessorService"/> object.
        /// </summary>
        /// <param name="taskQueue">
        ///   The background task queue.
        /// </param>
        /// <param name="loggerFactory">
        ///   The logger factory.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="taskQueue"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="loggerFactory"/> is <see langword="null"/>.
        /// </exception>
        public BackgroundTaskQueueProcessorService(IBackgroundTaskQueue taskQueue, ILoggerFactory loggerFactory) {
            TaskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
            _logger = loggerFactory?.CreateLogger<BackgroundTaskQueueProcessorService>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        
        /// <summary>
        /// Runs the background service.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the service is shutting down.
        /// </param>
        /// <returns>
        ///   The background service task.
        /// </returns>
        protected async override Task ExecuteAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Background task queue processor service is starting.");

            while (!cancellationToken.IsCancellationRequested) {
                var workItem = await TaskQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(async () => {
                    try {
                        await workItem(cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) {
                        _logger.LogError(ex, $"Error occurred executing {nameof(workItem)}.");
                    }
                }, cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            _logger.LogInformation("Background task queue processor service is stopping.");
        }
    }
}
