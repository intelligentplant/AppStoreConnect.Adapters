﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;

namespace DataCore.Adapter {

    /// <summary>
    /// Default <see cref="IAdapterCallContext"/> implementation.
    /// </summary>
    public class DefaultAdapterCallContext : IAdapterCallContext {

        /// <inheritdoc/>
        public ClaimsPrincipal? User { get; }

        /// <inheritdoc/>
        public string ConnectionId { get; }

        /// <inheritdoc/>
        public string CorrelationId { get; }

        /// <inheritdoc/>
        public CultureInfo CultureInfo { get; }

        /// <inheritdoc/>
        public IDictionary<object, object?> Items { get; }

        /// <inheritdoc/>
        public IServiceProvider Services { get; }


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
        /// <param name="cultureInfo">
        ///   The culture info.
        /// </param>
        /// <param name="serviceProvider">
        ///   The service provider.
        /// </param>
        public DefaultAdapterCallContext(
            ClaimsPrincipal? user = null,
            string? connectionId = null,
            string? correlationId = null,
            CultureInfo? cultureInfo = null,
            IServiceProvider? serviceProvider = null
        ) {
            User = user;
            ConnectionId = connectionId ?? Guid.NewGuid().ToString();
            CorrelationId = correlationId ?? Guid.NewGuid().ToString();
            CultureInfo = cultureInfo ?? CultureInfo.CurrentUICulture;
            Items = new ConcurrentDictionary<object, object?>();
            Services = serviceProvider ?? NullServiceProvider.Instance;
        }


        /// <summary>
        /// Default <see cref="IServiceProvider"/> implementation that always returns <see langword="null"/> 
        /// when resolving a service.
        /// </summary>
        private class NullServiceProvider : IServiceProvider {

            /// <summary>
            /// Singleton instance.
            /// </summary>
            public static IServiceProvider Instance { get; } = new NullServiceProvider();


            /// <summary>
            /// Creates a new <see cref="NullServiceProvider"/> instance.
            /// </summary>
            private NullServiceProvider() { }


            /// <inheritdoc/>
            public object GetService(Type serviceType) {
                return null!;
            }

        }

    }
}
