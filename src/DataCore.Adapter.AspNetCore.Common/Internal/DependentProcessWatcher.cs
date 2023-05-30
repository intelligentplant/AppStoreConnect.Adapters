using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.AspNetCore.Internal {

    /// <summary>
    /// <see cref="IHostedService"/> that watches a set of dependent processes and will request 
    /// that the application gracefully exits if any of the dependent processes exit.
    /// </summary>
    internal sealed partial class DependentProcessWatcher : BackgroundService {

        /// <summary>
        /// The <see cref="IHostApplicationLifetime"/> that is used to request graceful shutdown 
        /// if required.
        /// </summary>
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger<DependentProcessWatcher> _logger;

        /// <summary>
        /// The process IDs to watch.
        /// </summary>
        private readonly int[] _pids;


        /// <summary>
        /// Creates a new <see cref="DependentProcessWatcher"/> instance.
        /// </summary>
        /// <param name="pids">
        ///   The process IDs to watch. Note that specifying a PID that does not exist will result 
        ///   in immediate shutdown of the host application when <see cref="ExecuteAsync"/> is called.
        /// </param>
        /// <param name="hostApplicationLifetime">
        ///   The <see cref="IHostApplicationLifetime"/>.
        /// </param>
        /// <param name="logger">
        ///   The <see cref="ILogger"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pids"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   Specifying a PID that does not exist will result in immediate shutdown of the host 
        ///   application when <see cref="ExecuteAsync"/> is called.
        /// </remarks>
        public DependentProcessWatcher(IEnumerable<int> pids, IHostApplicationLifetime hostApplicationLifetime, ILogger<DependentProcessWatcher> logger) {
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
            _pids = pids?.ToArray() ?? throw new ArgumentNullException(nameof(pids));
        }


        /// <inheritdoc/>
        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            foreach (var pid in _pids) {
                var process = Process.GetProcessById(pid);
                if (process == null) {
                    LogProcessNotFound(pid);
                    _hostApplicationLifetime.StopApplication();
                    break;
                }

                var name = process.ProcessName;

                LogWatchingProcess(pid, name);

                process.EnableRaisingEvents = true;
                process.Exited += (sender, args) => { 
                    if (!stoppingToken.IsCancellationRequested) {
                        LogProcessExited(pid, name);
                        _hostApplicationLifetime.StopApplication();
                    }
                };
            }

            return Task.CompletedTask;
        }


        [LoggerMessage(0, LogLevel.Information, "Watching dependent process '{name}' (PID: {pid}).")]
        partial void LogWatchingProcess(int pid, string name);

        [LoggerMessage(1, LogLevel.Warning, "Dependent process '{name}' (PID: {pid}) has exited.")]
        partial void LogProcessExited(int pid, string name);

        [LoggerMessage(2, LogLevel.Warning, "Dependent process {pid} does not exist or has already exited.")]
        partial void LogProcessNotFound(int pid);

    }
}
