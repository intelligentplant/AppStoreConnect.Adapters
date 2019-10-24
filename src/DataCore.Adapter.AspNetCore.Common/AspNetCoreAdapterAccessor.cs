using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// <see cref="IAdapterAccessor"/> implementation that resolves <see cref="IAdapter"/> objects that 
    /// are passed in as constructor parameters via dependency injection.
    /// </summary>
    public class AspNetCoreAdapterAccessor : AdapterAccessor {

        /// <summary>
        /// The available adapters.
        /// </summary>
        private readonly IAdapter[] _adapters;


        /// <summary>
        /// Creates a new <see cref="AspNetCoreAdapterAccessor"/> object.
        /// </summary>
        /// <param name="authorizationService">
        ///   The authorization service that will be used to control access to adapters.
        /// </param>
        /// <param name="adapters">
        ///   The ASP.NET Core hosted services.
        /// </param>
        public AspNetCoreAdapterAccessor(IAdapterAuthorizationService authorizationService, IEnumerable<IAdapter> adapters) : base(authorizationService) {
            _adapters = adapters?.ToArray() ?? Array.Empty<IAdapter>();
        }


        /// <summary>
        /// Returns the available adapters.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The available adapters.
        /// </returns>
        protected override Task<IEnumerable<IAdapter>> GetAdapters(CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<IAdapter>>(_adapters);
        }
    }
}
