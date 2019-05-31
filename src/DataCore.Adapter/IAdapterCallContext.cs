using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes the context that is passed into adapters to identify the calling user.
    /// </summary>
    public interface IAdapterCallContext {

        /// <summary>
        /// The calling user.
        /// </summary>
        ClaimsPrincipal User { get; }

        /// <summary>
        /// The connection ID for the caller.
        /// </summary>
        string ConnectionId { get; }

    }
}
