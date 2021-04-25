using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        /// Gets the available adapters matching the specified filter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The available adapters.
        /// </returns>
        /// <remarks>
        ///   Implementers should use <see cref="IsAuthorized"/> to determine if a caller 
        ///   is authorized to access a particular adapter. Only enabled adapters should 
        ///   be returned.
        /// </remarks>
        protected abstract IAsyncEnumerable<IAdapter> FindAdapters(
            IAdapterCallContext context, 
            FindAdaptersRequest request,
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Gets the available adapters matching the specified filter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter to retrieve.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The adapter, or <see langword="null"/> if the adapter cannot be resolved.
        /// </returns>
        /// <remarks>
        ///   Implementers should use <see cref="IsAuthorized"/> to determine if a caller 
        ///   is authorized to access a particular adapter. Only enabled adapters should 
        ///   be returned.
        /// </remarks>
        protected abstract Task<IAdapter?> GetAdapter(
            IAdapterCallContext context,
            string adapterId,
            CancellationToken cancellationToken
        );


        /// <inheritdoc/>
        async IAsyncEnumerable<IAdapter> IAdapterAccessor.FindAdapters(
            IAdapterCallContext context, 
            FindAdaptersRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            ValidationExtensions.ValidateObject(request);

            await foreach (var item in FindAdapters(context, request, cancellationToken).ConfigureAwait(false)) {
                if (item == null || !item.IsEnabled) {
                    continue;
                }
                yield return item;
            }
        }


        /// <inheritdoc/>
        async Task<IAdapter?> IAdapterAccessor.GetAdapter(
            IAdapterCallContext context, 
            string adapterId,
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = await GetAdapter(context, adapterId, cancellationToken).ConfigureAwait(false);
            if (result == null || !result.IsEnabled) {
                return null;
            }

            return result;
        }


        /// <summary>
        /// Tests if an adapter matches a search filter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="request">
        ///   The search filter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the adapter matches the filter, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        protected bool MatchesFilter(IAdapter adapter, FindAdaptersRequest request) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            if (!string.IsNullOrWhiteSpace(request.Id)) {
                if (!string.Equals(adapter.Descriptor.Id, request.Id, StringComparison.OrdinalIgnoreCase) && !adapter.Descriptor.Id.Like(request.Id)) {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Name)) {
                if (!string.Equals(adapter.Descriptor.Name, request.Name, StringComparison.OrdinalIgnoreCase) && !adapter.Descriptor.Name.Like(request.Name)) {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Description)) {
                if (!string.Equals(adapter.Descriptor.Description, request.Description, StringComparison.OrdinalIgnoreCase) && !adapter.Descriptor.Description.Like(request.Description)) {
                    return false;
                }
            }

            if (request.Features != null) {
                foreach (var feature in request.Features) {
                    if (string.IsNullOrWhiteSpace(feature)) {
                        return false;
                    }

                    if (!adapter.HasFeature(feature)) {
                        return false;
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Tests if a caller is authorized to access the specified adapter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return <see langword="true"/> if the caller 
        ///   is authorized to access the adapter, or <see langword="false"/> otherwise.
        /// </returns>
        protected Task<bool> IsAuthorized(IAdapter adapter, IAdapterCallContext context, CancellationToken cancellationToken) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            return AuthorizationService.AuthorizeAdapter(adapter, context, cancellationToken);
        }

    }
}
