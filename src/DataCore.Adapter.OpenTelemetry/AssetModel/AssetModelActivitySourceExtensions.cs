using System;
using System.Diagnostics;
using System.Linq;

using DataCore.Adapter.AssetModel;

namespace DataCore.Adapter.Diagnostics.AssetModel {

    /// <summary>
    /// Extensions for <see cref="ActivitySource"/> related to asset model operations.
    /// </summary>
    public static class AssetModelActivitySourceExtensions {

        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IAssetModelBrowse.BrowseAssetModelNodes"/> call.
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


        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IAssetModelBrowse.GetAssetModelNodes"/> call.
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


        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IAssetModelSearch.FindAssetModelNodes"/> call.
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
