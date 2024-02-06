using System;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Logging {

    /// <summary>
    /// Wraps an <see cref="ILogger"/> and adds scope data to each log message.
    /// </summary>
    internal class ScopedLogger : ILogger, IDisposable {

        /// <summary>
        /// Specifies if the logger has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The underlying logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The scope data to add to each log message.
        /// </summary>
        private readonly IDisposable? _scope;

        /// <summary>
        /// Invoked when the logger is disposed.
        /// </summary>
        private readonly Action? _onDisposed;


        /// <summary>
        /// Creates a new <see cref="ScopedLogger"/> instance.
        /// </summary>
        /// <param name="logger">
        ///   The underlying logger.
        /// </param>
        /// <param name="scope">
        ///   The scope data to add to each log message.
        /// </param>
        /// <param name="onDisposed">
        ///   Invoked when the logger is disposed.
        /// </param>
        internal ScopedLogger(ILogger logger, object scope, Action? onDisposed) {
            _logger = logger;
            _scope = _logger.BeginScope(scope);
            _onDisposed = onDisposed;
        }


        /// <inheritdoc/>
        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }


        /// <inheritdoc/>
        bool ILogger.IsEnabled(LogLevel logLevel) {
            return _logger.IsEnabled(logLevel);
        }


        /// <inheritdoc/>
        IDisposable? ILogger.BeginScope<TState>(TState state) {
            return _logger.BeginScope(state);
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _scope?.Dispose();
            _onDisposed?.Invoke();

            _disposed = true;
        }

    }
}
