using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore;
using DataCore.Adapter.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;

namespace DataCore.Adapter {

    /// <summary>
    /// <see cref="IAdapterAccessor"/> implementation that resolves <see cref="IAdapter"/> objects that 
    /// are registered as ASP.NET Core hosted services.
    /// </summary>
    public class HostedServiceAdapterAccessor : AdapterAccessor {

        /// <summary>
        /// The available adapters.
        /// </summary>
        private readonly IAdapter[] _adapters;


        /// <summary>
        /// Creates a new <see cref="HostedServiceAdapterAccessor"/> object.
        /// </summary>
        /// <param name="hostedServices">
        ///   The ASP.NET Core hosted services.
        /// </param>
        public HostedServiceAdapterAccessor(AdapterApiAuthorizationService authorizationService, IEnumerable<IHostedService> hostedServices) : base(authorizationService) {
            _adapters = hostedServices?.Select(x => x as IAdapter).Where(x => x != null).ToArray() ?? new IAdapter[0];
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
