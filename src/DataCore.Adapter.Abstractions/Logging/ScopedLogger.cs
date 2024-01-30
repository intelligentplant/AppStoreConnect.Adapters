using System;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Logging {

    /// <summary>
    /// Wraps an <see cref="ILogger"/> and adds scope data to each log message.
    /// </summary>
    internal class ScopedLogger : ILogger, IDisposable {

        /// <summary>
        /// The underlying logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The scope data to add to each log message.
        /// </summary>
        private readonly IDisposable? _scope;


        /// <summary>
        /// Creates a new <see cref="ScopedLogger"/> instance.
        /// </summary>
        /// <param name="logger">
        ///   The underlying logger.
        /// </param>
        /// <param name="scope">
        ///   The scope data to add to each log message.
        /// </param>
        internal ScopedLogger(ILogger logger, object scope) {
            _logger = logger;
            _scope = _logger.BeginScope(scope);
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
            _scope?.Dispose();
        }

    }
}
