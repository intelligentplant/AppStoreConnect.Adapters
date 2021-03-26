using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using DataCore.Adapter.Extensions;
using DataCore.Adapter.Diagnostics.Extensions;

namespace DataCore.Adapter.Diagnostics.Extensions {
    public static class ExtensionFeaturesActivitySourceExtensions {

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


        public static Activity? StartDuplexStreamActivity(
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
