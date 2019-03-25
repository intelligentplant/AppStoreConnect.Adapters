using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace DataCore.Adapter {

    /// <summary>
    /// <see cref="IAdapterAccessor"/> implementation that resolves <see cref="IAdapter"/> objects that 
    /// are registered as ASP.NET Core hosted services.
    /// </summary>
    public class HostedServiceAdapterAccessor : IAdapterAccessor {

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
        public HostedServiceAdapterAccessor(IEnumerable<IHostedService> hostedServices) {
            _adapters = hostedServices?.Select(x => x as IAdapter).Where(x => x != null).ToArray() ?? new IAdapter[0];
        }


        /// <inheritdoc/>
        Task<IEnumerable<IAdapter>> IAdapterAccessor.GetAdapters(IDataCoreContext context, CancellationToken cancellationToken) {
            return GetAdapters(context, cancellationToken);
        }


        /// <summary>
        /// Gets all available adapters.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IDataCoreContext"/> describing the calling user.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The available <see cref="IAdapter"/> objects.
        /// </returns>
        /// <remarks>
        ///   Override this method to apply restrictions on which adapters are returned.
        /// </remarks>
        protected virtual Task<IEnumerable<IAdapter>> GetAdapters(IDataCoreContext context, CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<IAdapter>>(_adapters);
        }


        /// <inheritdoc/>
        Task<IAdapter> IAdapterAccessor.GetAdapter(IDataCoreContext context, string adapterId, CancellationToken cancellationToken) {
            if (adapterId == null) {
                throw new ArgumentNullException(nameof(adapterId));
            }
            return GetAdapter(context, adapterId, cancellationToken);
        }


        /// <summary>
        /// Gets the specified adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IDataCoreContext"/> describing the calling user.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The adapter.
        /// </returns>
        /// <remarks>
        ///   Override this method to apply restrictions on which adapters are returned.
        /// </remarks>
        protected virtual Task<IAdapter> GetAdapter(IDataCoreContext context, string adapterId, CancellationToken cancellationToken) {
            var adapter = _adapters.FirstOrDefault(x => x.Descriptor.Id.Equals(adapterId));
            return Task.FromResult(adapter);
        }

    }
}
