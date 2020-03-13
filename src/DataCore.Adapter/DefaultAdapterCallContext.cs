using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;

namespace DataCore.Adapter {

    /// <summary>
    /// Default <see cref="IAdapterCallContext"/> implementation.
    /// </summary>
    public sealed class DefaultAdapterCallContext : IAdapterCallContext {

        /// <inheritdoc/>
        public ClaimsPrincipal User { get; }

        /// <inheritdoc/>
        public string ConnectionId { get; }

        /// <inheritdoc/>
        public string CorrelationId { get; }

        /// <inheritdoc/>
        public IDictionary<object, object> Items { get; }


        /// <summary>
        /// Creates a new <see cref="DefaultAdapterCallContext"/> object.
        /// </summary>
        /// <param name="user">
        ///   The user.
        /// </param>
        /// <param name="connectionId">
        ///   The connection ID.
        /// </param>
        /// <param name="correlationId">
        ///   The correlation ID.
        /// </param>
        public DefaultAdapterCallContext(
            ClaimsPrincipal user = null,
            string connectionId = null,
            string correlationId = null
        ) {
            User = user;
            ConnectionId = connectionId ?? Guid.NewGuid().ToString();
            CorrelationId = correlationId ?? Guid.NewGuid().ToString();
            Items = new ConcurrentDictionary<object, object>();
        }

    }
}
