using System;
using System.Collections.Concurrent;

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
        /// The loggers created by the factory.
        /// </summary>
        private readonly ConcurrentDictionary<string, ScopedLogger> _loggers = new ConcurrentDictionary<string, ScopedLogger>(StringComparer.Ordinal);


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
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            return _loggers.GetOrAdd(categoryName, name => new ScopedLogger(_loggerFactory.CreateLogger(name), _scope, () => RemoveLogger(name)));
        }


        /// <inheritdoc/>
        public void AddProvider(ILoggerProvider provider) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            _loggerFactory.AddProvider(provider);
        }


        /// <summary>
        /// Removes a logger from the cache.
        /// </summary>
        /// <param name="categoryName">
        ///   The logger category name.
        /// </param>
        private void RemoveLogger(string categoryName) {
            _loggers.TryRemove(categoryName, out _);
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            foreach (var item in _loggers.Values) {
                item.Dispose();
            }

            _loggers.Clear();
        }

    }
}
