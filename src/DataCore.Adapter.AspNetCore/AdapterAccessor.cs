using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// Base <see cref="IAdapterAccessor"/> implementation that will authorize access to individual 
    /// adapters based on the <see cref="AdapterApiAuthorizationService"/>.
    /// </summary>
    public abstract class AdapterAccessor: IAdapterAccessor {

        /// <summary>
        /// The adapter API authorization service to use.
        /// </summary>
        private readonly AdapterApiAuthorizationService _authorizationService;


        /// <summary>
        /// Creates a new <see cref="AdapterAccessor"/> object.
        /// </summary>
        /// <param name="authorizationService">
        ///   The adapter API authorization service to use.
        /// </param>
        protected AdapterAccessor(AdapterApiAuthorizationService authorizationService) {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }


        /// <summary>
        /// Gets the available adapters.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The available adapters.
        /// </returns>
        protected abstract Task<IEnumerable<IAdapter>> GetAdapters(CancellationToken cancellationToken);


        /// <inheritdoc/>
        async Task<IEnumerable<IAdapter>> IAdapterAccessor.GetAdapters(IAdapterCallContext context, CancellationToken cancellationToken) {
            var adapters = await GetAdapters(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (adapters == null || !adapters.Any()) {
                return new IAdapter[0];
            }

            var result = new List<IAdapter>(adapters.Count());

            foreach (var adapter in adapters) {
                var authResult = await _authorizationService.AuthorizeAsync(context.User, adapter).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (authResult.Succeeded) {
                    result.Add(adapter);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        async Task<IAdapter> IAdapterAccessor.GetAdapter(IAdapterCallContext context, string adapterId, CancellationToken cancellationToken) {
            var adapters = await GetAdapters(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var adapter = adapters?.FirstOrDefault(x => x.Descriptor.Id.Equals(adapterId, StringComparison.OrdinalIgnoreCase));
            if (adapter == null) {
                return null;
            }

            var authResult = await _authorizationService.AuthorizeAsync(context.User, adapter).ConfigureAwait(false);
            return authResult.Succeeded
                ? adapter
                : null;
        }
    }
}
