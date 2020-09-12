using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common;

namespace DataCore.Adapter {

    /// <summary>
    /// Base <see cref="IAdapterAccessor"/> implementation that will authorize access to individual 
    /// adapters based on the provided <see cref="IAdapterAuthorizationService"/>.
    /// </summary>
    public abstract class AdapterAccessor : IAdapterAccessor {

        /// <inheritdoc/>
        public IAdapterAuthorizationService AuthorizationService { get; }


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
            AuthorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
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
        public async Task<IEnumerable<IAdapter>> FindAdapters(
            IAdapterCallContext context, 
            FindAdaptersRequest request, 
            bool enabledOnly = true,
            CancellationToken cancellationToken = default
        ) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            var adapters = await GetAdapters(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (enabledOnly) {
                adapters = adapters?.Where(x => x.IsEnabled)?.ToArray();
            }

            if (adapters == null || !adapters.Any()) {
                return Array.Empty<IAdapter>();
            }

            if (!string.IsNullOrWhiteSpace(request.Id)) {
                adapters = adapters.Where(x => x.Descriptor.Id.Like(request.Id));
            }
            if (!string.IsNullOrWhiteSpace(request.Name)) {
                adapters = adapters.Where(x => x.Descriptor.Name.Like(request.Name));
            }
            if (!string.IsNullOrWhiteSpace(request.Description)) {
                adapters = adapters.Where(x => x.Descriptor.Description!.Like(request.Description));
            }
            if (request.Features != null) {
                foreach (var item in request.Features) {
                    if (string.IsNullOrWhiteSpace(item)) {
                        continue;
                    }
                    adapters = adapters.Where(x => x.HasFeature(item));
                }
            }

            adapters = adapters
                .OrderBy(x => x.Descriptor.Name)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize);

            var result = new List<IAdapter>();

            foreach (var adapter in adapters) {
                var authResult = await AuthorizationService.AuthorizeAdapter(adapter, context, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (authResult) {
                    result.Add(adapter);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<IAdapter?> GetAdapter(
            IAdapterCallContext context, 
            string adapterId, 
            bool enabledOnly = true,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var adapters = await GetAdapters(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var adapter = adapters?.FirstOrDefault(x => x.Descriptor.Id.Equals(adapterId, StringComparison.OrdinalIgnoreCase));
            if (adapter == null || (enabledOnly && !adapter.IsEnabled)) {
                return null;
            }

            var authResult = await AuthorizationService.AuthorizeAdapter(adapter, context, cancellationToken).ConfigureAwait(false);
            return authResult
                ? adapter
                : null;
        }

    }
}
