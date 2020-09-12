using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes a service for resolving adapters at runtime.
    /// </summary>
    public interface IAdapterAccessor {

        /// <summary>
        /// The authorization service used by the <see cref="IAdapterAccessor"/> to determine if a 
        /// user is authorized to access an adapter or an adapter feature.
        /// </summary>
        IAdapterAuthorizationService AuthorizationService { get; }


        /// <summary>
        /// Gets the available adapters matching the specified filter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The search filter.
        /// </param>
        /// <param name="enabledOnly">
        ///   When <see langword="true"/>, only enabled adapters will be returned.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The adapters available to the caller.
        /// </returns>
        Task<IEnumerable<IAdapter>> FindAdapters(
            IAdapterCallContext context, 
            FindAdaptersRequest request, 
            bool enabledOnly = true,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Gets the specified adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter.
        /// </param>
        /// <param name="enabledOnly">
        ///   When <see langword="true"/>, only enabled adapters will be returned.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The requested adapter.
        /// </returns>
        Task<IAdapter?> GetAdapter(
            IAdapterCallContext context, 
            string adapterId,
            bool enabledOnly = true,
            CancellationToken cancellationToken = default
        );

    }

}
