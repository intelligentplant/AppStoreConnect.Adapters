using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// <see cref="IBackgroundTaskService"/> implementation that runs as an <see cref="IHostedService"/> 
    /// in the ASP.NET Core host.
    /// </summary>
    public sealed class AspNetCoreBackgroundTaskService : BackgroundTaskService, IHostedService {

        /// <summary>
        /// The task that dequeues and runs queued background work items.
        /// </summary>
        private Task _executingTask;

        /// <summary>
        /// Fires when <see cref="StopAsync(CancellationToken)"/> is called or the service is 
        /// disposed.
        /// </summary>
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();


        /// <summary>
        /// Creates a new <see cref="AspNetCoreBackgroundTaskService"/> object.
        /// </summary>
        /// <param name="loggerFactory">
        ///   The logger factory for the service.
        /// </param>
        public AspNetCoreBackgroundTaskService(ILoggerFactory loggerFactory)
            : base(loggerFactory) { }


        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">
        ///   Indicates that the start process has been aborted.
        /// </param>
        public Task StartAsync(CancellationToken cancellationToken) {
            // Store the task we're executing
            _executingTask = RunAsync(_stoppingCts.Token);

            // If the task is completed then return it, this will bubble cancellation and failure to the caller
            if (_executingTask.IsCompleted) {
                return _executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }


        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">
        ///   Indicates that the shutdown process should no longer be graceful.
        /// </param>
        public async Task StopAsync(CancellationToken cancellationToken) {
            // Stop called without start
            if (_executingTask == null) {
                return;
            }

            try {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
            }

        }


        /// <inheritdoc/>
        protected override void RunBackgroundWorkItem(Action<CancellationToken> workItem) {
            _ = Task.Run(() => {
                try {
                    workItem(_stoppingCts.Token);
                }
                catch (OperationCanceledException) {
                    // Do nothing
                }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_ErrorInBackgroundTask, workItem);
                }
            }, _stoppingCts.Token);
        }


        /// <inheritdoc/>
        protected override void RunBackgroundWorkItem(Func<CancellationToken, Task> workItem) {
            _ = Task.Run(async () => {
                try {
                    await workItem(_stoppingCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    // Do nothing
                }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_ErrorInBackgroundTask, workItem);
                }
            }, _stoppingCts.Token);
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _stoppingCts.Cancel();
        }

    }
}
