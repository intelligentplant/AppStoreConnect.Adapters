using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes the context that is passed into adapters to identify the calling user.
    /// </summary>
    public interface IAdapterCallContext {

        /// <summary>
        /// The calling user.
        /// </summary>
        ClaimsPrincipal? User { get; }

        /// <summary>
        /// The host-specified connection ID for the caller.
        /// </summary>
        string ConnectionId { get; }

        /// <summary>
        /// The host-specified correlation ID for the current operation.
        /// </summary>
        string CorrelationId { get; }

        /// <summary>
        /// The <see cref="System.Globalization.CultureInfo"/> for the caller.
        /// </summary>
        CultureInfo CultureInfo { get; }

        /// <summary>
        /// The service provider for the call context.
        /// </summary>
        /// <remarks>
        ///   Adapters can use the <see cref="Services"/> property to resolve scoped services for 
        ///   the calling user.
        /// </remarks>
        IServiceProvider Services { get; }

        /// <summary>
        /// Additional items related to the call context.
        /// </summary>
        IDictionary<object, object?> Items { get; }

    }
}
