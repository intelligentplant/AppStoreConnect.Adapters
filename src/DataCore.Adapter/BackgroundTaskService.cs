using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter {

    /// <summary>
    /// Base <see cref="IBackgroundTaskService"/> implementation. Call the <see cref="RunAsync"/> 
    /// method from your implementation to start processing the queued items.
    /// </summary>
    public abstract class BackgroundTaskService : IBackgroundTaskService, IDisposable {

        /// <summary>
        /// The default background task service.
        /// </summary>
        public static IBackgroundTaskService Default { get; } = new DefaultBackgroundTaskService(null);

        /// <summary>
        /// Flags if the service has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The logger for the service.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// The currently-queued work items.
        /// </summary>
        private readonly ConcurrentQueue<BackgroundWorkItem> _queue = new ConcurrentQueue<BackgroundWorkItem>();

        /// <summary>
        /// Signals when an item is added to the <see cref="_queue"/>.
        /// </summary>
        private readonly SemaphoreSlim _queueSignal = new SemaphoreSlim(0);


        /// <summary>
        /// Creates a new <see cref="BackgroundTaskService"/> object.
        /// </summary>
        /// <param name="loggerFactory">
        ///   The logger factory for the service. Can be <see langword="null"/>.
        /// </param>
        protected BackgroundTaskService(ILoggerFactory loggerFactory) {
            Logger = loggerFactory?.CreateLogger(GetType()) ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        }


        /// <inheritdoc/>
        public void QueueBackgroundWorkItem(Action<CancellationToken> workItem) {
            if (workItem == null) {
                throw new ArgumentNullException(nameof(workItem));
            }
            _queue.Enqueue(new BackgroundWorkItem(workItem));
            _queueSignal.Release();
        }


        /// <inheritdoc/>
        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem) {
            if (workItem == null) {
                throw new ArgumentNullException(nameof(workItem));
            }
            _queue.Enqueue(new BackgroundWorkItem(workItem));
            _queueSignal.Release();
        }


        /// <summary>
        /// Starts a long-running task that will dequeue items as they are queued and dispatch 
        /// them to either <see cref="RunBackgroundWorkItem(Action{CancellationToken})"/> 
        /// or <see cref="RunBackgroundWorkItem(Func{CancellationToken, Task})"/>
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the task should stop processing queued work 
        ///   items.
        /// </param>
        /// <returns>
        ///   A long-running task that will end when the <paramref name="cancellationToken"/> fires.
        /// </returns>
        protected async Task RunAsync(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                await _queueSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (!_queue.TryDequeue(out var item)) {
                    continue;
                }

                if (item.WorkItem != null) {
                    RunBackgroundWorkItem(item.WorkItem);
                }
                else if (item.WorkItemAsync != null) {
                    RunBackgroundWorkItem(item.WorkItemAsync);
                }
            }
        }


        /// <summary>
        /// Runs a synchronous background work item.
        /// </summary>
        /// <param name="workItem">
        ///   The work item.
        /// </param>
        protected abstract void RunBackgroundWorkItem(Action<CancellationToken> workItem);


        /// <summary>
        /// Runs an asynchronous background work item.
        /// </summary>
        /// <param name="workItem">
        ///   The work item.
        /// </param>
        protected abstract void RunBackgroundWorkItem(Func<CancellationToken, Task> workItem);


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the service is being disposed, or <see langword="false"/> 
        ///   if it is being finalized.
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                _queueSignal.Dispose();
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            Dispose(true);
            _isDisposed = true;

            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~BackgroundTaskService() {
            Dispose(false);
        }


        /// <summary>
        /// Describes a work item that has been added to a <see cref="BackgroundTaskService"/>.
        /// </summary>
        private struct BackgroundWorkItem {

            /// <summary>
            /// The synchronous work item.
            /// </summary>
            public Action<CancellationToken> WorkItem { get; }

            /// <summary>
            /// The asynchronous work item.
            /// </summary>
            public Func<CancellationToken, Task> WorkItemAsync { get; }


            /// <summary>
            /// Creates a new <see cref="BackgroundWorkItem"/> with a synchronous work item.
            /// </summary>
            /// <param name="workItem">
            ///   The work item.
            /// </param>
            public BackgroundWorkItem(Action<CancellationToken> workItem) {
                WorkItem = workItem;
                WorkItemAsync = null;
            }


            /// <summary>
            /// Creates a new <see cref="BackgroundWorkItem"/> with an asynchronous work item.
            /// </summary>
            /// <param name="workItem">
            ///   The work item.
            /// </param>
            public BackgroundWorkItem(Func<CancellationToken, Task> workItem) {
                WorkItem = null;
                WorkItemAsync = workItem;
            }

        }

    }


    /// <summary>
    /// Default <see cref="IBackgroundTaskService"/> implementation that runs work items in the 
    /// background using <see cref="Task.Run(Action, CancellationToken)"/> or 
    /// <see cref="Task.Run(Func{Task}, CancellationToken)"/>.
    /// </summary>
    internal sealed class DefaultBackgroundTaskService : BackgroundTaskService {

        /// <summary>
        /// Fires when the service is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedSource = new CancellationTokenSource();


        /// <summary>
        /// Creates a new <see cref="DefaultBackgroundTaskService"/> object.
        /// </summary>
        /// <param name="loggerFactory">
        ///   The logger factory for the service.
        /// </param>
        internal DefaultBackgroundTaskService(ILoggerFactory loggerFactory) : base(loggerFactory) {
            RunBackgroundWorkItem(RunAsync);
        }


        /// <inheritdoc/>
        protected override void RunBackgroundWorkItem(Action<CancellationToken> workItem) {
            _ = Task.Run(() => {
                try {
                    workItem(_disposedSource.Token);
                }
                catch (OperationCanceledException) {
                    // Do nothing
                }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_ErrorInBackgroundTask, workItem);
                }
            }, _disposedSource.Token);
        }


        /// <inheritdoc/>
        protected override void RunBackgroundWorkItem(Func<CancellationToken, Task> workItem) {
            _ = Task.Run(async () => {
                try {
                    await workItem(_disposedSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    // Do nothing
                }
                catch (Exception e) {
                    Logger.LogError(e, Resources.Log_ErrorInBackgroundTask, workItem);
                }
            }, _disposedSource.Token);
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing) {
                _disposedSource.Cancel();
                _disposedSource.Dispose();
            }
        }

    }
}
