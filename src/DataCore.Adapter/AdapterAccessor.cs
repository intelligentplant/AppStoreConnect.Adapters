using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Base <see cref="IAdapterAccessor"/> implementation that will authorize access to individual 
    /// adapters based on the provided <see cref="IAdapterAuthorizationService"/>.
    /// </summary>
    public abstract class AdapterAccessor : IAdapterAccessor {

        /// <summary>
        /// The adapter API authorization service to use.
        /// </summary>
        private readonly IAdapterAuthorizationService _authorizationService;


        /// <summary>
        /// Creates a new <see cref="AdapterAccessor"/> object.
        /// </summary>
        /// <param name="authorizationService">
        ///   The adapter authorization service to use.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="authorizationService"/> is <see langword="null"/>.
        /// </exception>
        protected AdapterAccessor(IAdapterAuthorizationService authorizationService) {
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
                return Array.Empty<IAdapter>();
            }

            var result = new List<IAdapter>(adapters.Count());

            foreach (var adapter in adapters) {
                var authResult = await _authorizationService.AuthorizeAdapter(adapter, context, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (authResult) {
                    result.Add(adapter);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        async Task<IAdapter> IAdapterAccessor.GetAdapter(IAdapterCallContext context, string adapterId, CancellationToken cancellationToken) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var adapters = await GetAdapters(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var adapter = adapters?.FirstOrDefault(x => x.Descriptor.Id.Equals(adapterId, StringComparison.OrdinalIgnoreCase));
            if (adapter == null) {
                return null;
            }

            var authResult = await _authorizationService.AuthorizeAdapter(adapter, context, cancellationToken).ConfigureAwait(false);
            return authResult
                ? adapter
                : null;
        }

        /// <inheritdoc/>
        async Task<ResolvedAdapterFeature<TFeature>> IAdapterAccessor.GetAdapterAndFeature<TFeature>(IAdapterCallContext context, string adapterId, CancellationToken cancellationToken) {
            var adapter = await ((IAdapterAccessor) this).GetAdapter(context, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return new ResolvedAdapterFeature<TFeature>(null, default, false);
            }

            var feature = adapter.Features.Get<TFeature>();
            if (feature == null) {
                return new ResolvedAdapterFeature<TFeature>(adapter, default, false);
            }

            var isAuthorized = await _authorizationService.AuthorizeAdapterFeature<TFeature>(adapter, context, cancellationToken).ConfigureAwait(false);
            return new ResolvedAdapterFeature<TFeature>(adapter, feature, isAuthorized);
        }
    }
}
