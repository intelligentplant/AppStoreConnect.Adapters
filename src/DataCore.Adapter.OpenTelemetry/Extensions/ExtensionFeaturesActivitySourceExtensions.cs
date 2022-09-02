using System;
using System.Diagnostics;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Diagnostics.Extensions {

    /// <summary>
    /// Extensions for <see cref="ActivitySource"/> related to extension feature operations.
    /// </summary>
    [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
    public static class ExtensionFeaturesActivitySourceExtensions {

        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IAdapterExtensionFeature.GetDescriptor"/> call.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="adapterId"></param>
        /// <param name="featureId"></param>
        /// <param name="kind"></param>
        /// <param name="parentId"></param>
        /// <returns>
        ///   A new <see cref="Activity"/> instance, or <see langword="null"/> if the 
        ///   <paramref name="source"/> is not enabled.
        /// </returns>
        public static Activity? StartGetDescriptorActivity(
            this ActivitySource source,
            string adapterId,
            Uri? featureId,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IAdapterExtensionFeature), nameof(IAdapterExtensionFeature.GetDescriptor)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (featureId != null) {
                result.SetTagWithNamespace("extensions.feature_id", featureId);
            }

            return result;
        }


        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IAdapterExtensionFeature.GetOperations"/> call.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="adapterId"></param>
        /// <param name="featureId"></param>
        /// <param name="kind"></param>
        /// <param name="parentId"></param>
        /// <returns>
        ///   A new <see cref="Activity"/> instance, or <see langword="null"/> if the 
        ///   <paramref name="source"/> is not enabled.
        /// </returns>
        public static Activity? StartGetOperationsActivity(
            this ActivitySource source,
            string adapterId,
            Uri? featureId,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IAdapterExtensionFeature), nameof(IAdapterExtensionFeature.GetOperations)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (featureId != null) {
                result.SetTagWithNamespace("extensions.feature_id", featureId);
            }

            return result;
        }


        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IAdapterExtensionFeature.Invoke"/> call.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="adapterId"></param>
        /// <param name="request"></param>
        /// <param name="kind"></param>
        /// <param name="parentId"></param>
        /// <returns>
        ///   A new <see cref="Activity"/> instance, or <see langword="null"/> if the 
        ///   <paramref name="source"/> is not enabled.
        /// </returns>
        public static Activity? StartInvokeActivity(
            this ActivitySource source,
            string adapterId,
            InvocationRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IAdapterExtensionFeature), nameof(IAdapterExtensionFeature.Invoke)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetTagWithNamespace("extensions.operation_id", request.OperationId);
            }

            return result;
        }


        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IAdapterExtensionFeature.Stream"/> call.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="adapterId"></param>
        /// <param name="request"></param>
        /// <param name="kind"></param>
        /// <param name="parentId"></param>
        /// <returns>
        ///   A new <see cref="Activity"/> instance, or <see langword="null"/> if the 
        ///   <paramref name="source"/> is not enabled.
        /// </returns>
        public static Activity? StartStreamActivity(
            this ActivitySource source,
            string adapterId,
            InvocationRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IAdapterExtensionFeature), nameof(IAdapterExtensionFeature.Stream)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);
            result.SetSubscriptionFlag();

            if (request != null) {
                result.SetTagWithNamespace("extensions.operation_id", request.OperationId);
            }

            return result;
        }


        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IAdapterExtensionFeature.DuplexStream"/> call.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="adapterId"></param>
        /// <param name="request"></param>
        /// <param name="kind"></param>
        /// <param name="parentId"></param>
        /// <returns>
        ///   A new <see cref="Activity"/> instance, or <see langword="null"/> if the 
        ///   <paramref name="source"/> is not enabled.
        /// </returns>
        public static Activity? StartDuplexStreamActivity(
            this ActivitySource source,
            string adapterId,
            DuplexStreamInvocationRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IAdapterExtensionFeature), nameof(IAdapterExtensionFeature.DuplexStream)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);
            result.SetSubscriptionFlag();

            if (request != null) {
                result.SetTagWithNamespace("extensions.operation_id", request.OperationId);
            }

            return result;
        }

    }
}
