﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Extensions;

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
                throw new RpcException(new Status(StatusCode.NotFound, string.Format(callContext?.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)));
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, string.Format(callContext?.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)));
            }
            if (!resolvedFeature.IsFeatureResolved) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, string.Format(callContext?.CultureInfo, Resources.Error_UnsupportedInterface, typeof(TFeature).Name)));
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                CreatePermissionDeniedException();
            }

            return resolvedFeature;
        }


        /// <summary>
        /// Gets the specified adapter feature.
        /// </summary>
        /// <param name="callContext">
        ///   The context describing the caller.
        /// </param>
        /// <param name="adapterAccessor">
        ///   The adapter accessor to use.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter to retrieve the feature from.
        /// </param>
        /// <param name="featureUri">
        ///   The feature URI.
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
        [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
        internal static async Task<ResolvedAdapterFeature<IAdapterExtensionFeature>> ResolveAdapterAndExtensionFeature(IAdapterCallContext callContext, IAdapterAccessor adapterAccessor, string adapterId, Uri featureUri, CancellationToken cancellationToken) {
            var resolvedFeature = await adapterAccessor.GetAdapterAndFeature<IAdapterExtensionFeature>(callContext, adapterId, featureUri, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                throw new RpcException(new Status(StatusCode.NotFound, string.Format(callContext?.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)));
            }
            if (!resolvedFeature.IsFeatureResolved) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, string.Format(callContext?.CultureInfo, Resources.Error_UnsupportedInterface, featureUri)));
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                throw CreatePermissionDeniedException();
            }

            return resolvedFeature;
        }


        /// <summary>
        /// Creates an <see cref="RpcException"/> with a <see cref="StatusCode.PermissionDenied"/> status.
        /// </summary>
        internal static RpcException CreatePermissionDeniedException() {
            return new RpcException(new Status(StatusCode.PermissionDenied, Resources.Error_NotAuthorized));
        }


        /// <summary>
        /// Validates the specified object. This method should be called on any adapter request objects 
        /// prior to passing them to an adapter.
        /// </summary>
        /// <param name="instance">
        ///   The object to validate.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="instance"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   <paramref name="instance"/> is not valid.
        /// </exception>
        internal static void ValidateObject(object instance) {
            if (instance == null) {
                throw new ArgumentNullException(nameof(instance));
            }

            Validator.ValidateObject(instance, new ValidationContext(instance), true);
        }

    }
}
