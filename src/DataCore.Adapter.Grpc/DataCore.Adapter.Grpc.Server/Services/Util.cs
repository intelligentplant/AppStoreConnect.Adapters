using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Utility methods for use by gRPC service implementations.
    /// </summary>
    internal static class Util {

        /// <summary>
        /// Gets the specified adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The adapter feature to request.
        /// </typeparam>
        /// <param name="callContext">
        ///   The context describing the caller.
        /// </param>
        /// <param name="adapterAccessor">
        ///   The adapter accessor to use.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter to retrieve the feature from.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The requested adapter feature.
        /// </returns>
        /// <exception cref="RpcException">
        ///   The adapter could not be resolved.
        /// </exception>
        /// <exception cref="RpcException">
        ///   The adapter does not provide the requested feature.
        /// </exception>
        internal static async Task<ResolvedAdapterFeature<TFeature>> ResolveAdapterAndFeature<TFeature>(IAdapterCallContext callContext, IAdapterAccessor adapterAccessor, string adapterId, CancellationToken cancellationToken) where TFeature : IAdapterFeature {
            var resolvedFeature = await adapterAccessor.GetAdapterAndFeature<TFeature>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                throw new RpcException(new Status(StatusCode.NotFound, string.Format(Resources.Error_CannotResolveAdapterId, adapterId)));
            }
            if (!resolvedFeature.IsFeatureResolved) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, string.Format(Resources.Error_UnsupportedInterface, typeof(TFeature).Name)));
            }
            if (!resolvedFeature.IsAuthorized) {
                throw new RpcException(new Status(StatusCode.PermissionDenied, Resources.Error_NotAuthorized));
            }

            return resolvedFeature;
        }

    }
}
