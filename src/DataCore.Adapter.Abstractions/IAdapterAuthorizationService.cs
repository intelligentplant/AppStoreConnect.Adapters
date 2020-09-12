using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes a service for authorizing access to adapters and adapter features.
    /// </summary>
    public interface IAdapterAuthorizationService {

        /// <summary>
        /// Authorizes access to an adapter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="context">
        ///  The call context to authorize.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <paramref name="context"/> is authorized to access the 
        ///   adapter, or <see langword="false"/> otherwise.
        /// </returns>
        Task<bool> AuthorizeAdapter(
            IAdapter adapter, 
            IAdapterCallContext context, 
            CancellationToken cancellationToken
        );

        /// <summary>
        /// Authorizes access to an adapter feature.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="context">
        ///  The call context to authorize.
        /// </param>
        /// <param name="featureUri">
        ///   The URI of the adapter feature.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <paramref name="context"/> is authorized to access the 
        ///   adapter feature, or <see langword="false"/> otherwise.
        /// </returns>
        Task<bool> AuthorizeAdapterFeature(
            IAdapter adapter, 
            IAdapterCallContext context, 
            Uri featureUri,
            CancellationToken cancellationToken
        );

    }
}
