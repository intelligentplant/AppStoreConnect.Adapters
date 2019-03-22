using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes the context that is passed into drivers to identify the calling user.
    /// </summary>
    public interface IDataCoreContext : IServiceProvider {

        /// <summary>
        /// The calling user.
        /// </summary>
        ClaimsPrincipal User { get; }

    }
}
