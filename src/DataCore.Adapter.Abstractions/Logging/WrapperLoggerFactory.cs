using System;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Logging {

    /// <summary>
    /// <see cref="ILoggerFactory"/> that wraps an existing <see cref="ILogger"/>.
    /// </summary>
    internal class WrapperLoggerFactory : ILoggerFactory {

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger _logger;


        /// <summary>
        /// Creates a new <see cref="WrapperLoggerFactory"/> instance.
        /// </summary>
        /// <param name="logger">
        ///   The logger.
        /// </param>
        public WrapperLoggerFactory(ILogger logger) {
            _logger = logger;
        }


        /// <inheritdoc/>
        ILogger ILoggerFactory.CreateLogger(string categoryName) {
            return _logger;
        }


        /// <inheritdoc/>
        void ILoggerFactory.AddProvider(ILoggerProvider provider) {
            // No-op
        }


        /// <inheritdoc/>
        void IDisposable.Dispose() {
            // No-op
        }

    }
}
