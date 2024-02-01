using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Logging {

    /// <summary>
    /// Wraps an <see cref="ILoggerFactory"/> and adds scope data to each logger created.
    /// </summary>
    /// <remarks>
    ///   The factory must be disposed in order to dispose the underlying logger scopes.
    /// </remarks>
    internal class ScopedLoggerFactory : ILoggerFactory {

        /// <summary>
        /// Specifies if the factory has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The underlying logger factory.
        /// </summary>
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// The scope data to add to each logger.
        /// </summary>
        private readonly object _scope;

        /// <summary>
        /// The list of loggers created by the factory.
        /// </summary>
        private readonly List<ScopedLogger> _loggers = new List<ScopedLogger>();


        /// <summary>
        /// Creates a new <see cref="ScopedLoggerFactory"/> instance.
        /// </summary>
        /// <param name="loggerFactory">
        ///   The underlying logger factory.
        /// </param>
        /// <param name="scope">
        ///   The scope data to add to each logger.
        /// </param>
        public ScopedLoggerFactory(ILoggerFactory loggerFactory, object scope) {
            _loggerFactory = loggerFactory;
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }


        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName) {
            lock (_loggers) {
                if (_disposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                var logger = _loggerFactory.CreateLogger(categoryName);
            
                var wrapper = new ScopedLogger(logger, _scope);
                _loggers.Add(wrapper);

                return wrapper;
            }
        }


        /// <inheritdoc/>
        public void AddProvider(ILoggerProvider provider) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            _loggerFactory.AddProvider(provider);
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            lock (_loggers) {
                foreach (var logger in _loggers) {
                    logger.Dispose();
                }
                _loggers.Clear();
                _disposed = true;
            }
        }

    }
}
