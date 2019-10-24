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
    internal sealed class AspNetCoreBackgroundTaskService : BackgroundTaskService {

        internal CancellationToken StoppingToken { get; private set; }


        /// <summary>
        /// Creates a new <see cref="AspNetCoreBackgroundTaskService"/> object.
        /// </summary>
        /// <param name="loggerFactory">
        ///   The logger factory for the service.
        /// </param>
        public AspNetCoreBackgroundTaskService(ILoggerFactory loggerFactory)
            : base(loggerFactory) { }


        /// <inheritdoc/>
        protected override void RunBackgroundWorkItem(Action<CancellationToken> workItem) {
            _ = Task.Run(() => {
                try {
                    workItem(StoppingToken);
                }
                catch (OperationCanceledException) {
                    // Do nothing
                }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_ErrorInBackgroundTask, workItem);
                }
            }, StoppingToken);
        }


        /// <inheritdoc/>
        protected override void RunBackgroundWorkItem(Func<CancellationToken, Task> workItem) {
            _ = Task.Run(async () => {
                try {
                    await workItem(StoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    // Do nothing
                }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_ErrorInBackgroundTask, workItem);
                }
            }, StoppingToken);
        }


        /// <summary>
        /// Runs the service.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token that will fire when the service should stop.
        /// </param>
        internal Task RunInternal(CancellationToken cancellationToken) {
            return RunAsync(cancellationToken);
        }

    }


    internal class AspNetCoreBackgroundTaskServiceRunner : IHostedService {

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
        /// The background task service.
        /// </summary>
        private readonly AspNetCoreBackgroundTaskService _backgroundTaskService;


        /// <summary>
        /// Creates a new <see cref="AspNetCoreBackgroundTaskServiceRunner"/>.
        /// </summary>
        /// <param name="backgroundTaskService">
        ///   The <see cref="AspNetCoreBackgroundTaskService"/> that will dequeue and run 
        ///   background tasks.
        /// </param>
        public AspNetCoreBackgroundTaskServiceRunner(IBackgroundTaskService backgroundTaskService) {
            _backgroundTaskService = (AspNetCoreBackgroundTaskService) backgroundTaskService;
        }


        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">
        ///   Indicates that the start process has been aborted.
        /// </param>
        public Task StartAsync(CancellationToken cancellationToken) {
            // Store the task we're executing
            _executingTask = _backgroundTaskService.RunInternal(_stoppingCts.Token);

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
    }

}
