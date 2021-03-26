using System;
using System.Diagnostics;
using System.Linq;

using DataCore.Adapter.AssetModel;

namespace DataCore.Adapter.Diagnostics.AssetModel {
    public static class AssetModelActivitySourceExtensions {

        public static Activity? StartBrowseAssetModelNodesActivity(
            this ActivitySource source,
            string adapterId,
            BrowseAssetModelNodesRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IAssetModelBrowse), nameof(IAssetModelBrowse.BrowseAssetModelNodes)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetTagWithNamespace("assetmodel.parent_id", request.ParentId);
                result.SetPagingTags(request);
            }

            return result;
        }


        public static Activity? StartGetAssetModelNodesActivity(
            this ActivitySource source,
            string adapterId,
            GetAssetModelNodesRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IAssetModelBrowse), nameof(IAssetModelBrowse.GetAssetModelNodes)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetRequestItemCountTag(request.Nodes?.Count() ?? 0);
            }

            return result;
        }


        public static Activity? StartFindAssetModelNodesActivity(
            this ActivitySource source,
            string adapterId,
            FindAssetModelNodesRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IAssetModelSearch), nameof(IAssetModelSearch.FindAssetModelNodes)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetPagingTags(request);
            }

            return result;
        }

    }
}
