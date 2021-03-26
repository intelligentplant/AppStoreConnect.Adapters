using System;
using System.Diagnostics;
using System.Linq;

using DataCore.Adapter.Tags;

namespace DataCore.Adapter.Diagnostics.Tags {
    public static class TagsActivitySourceExtensions {

        public static Activity? StartGetTagPropertiesActivity(
            this ActivitySource source,
            string adapterId,
            GetTagPropertiesRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(ITagInfo), nameof(ITagInfo.GetTagProperties)),
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


        public static Activity? StartGetTagsActivity(
            this ActivitySource source,
            string adapterId,
            GetTagsRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(ITagInfo), nameof(ITagInfo.GetTags)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetRequestItemCountTag(request.Tags?.Count() ?? 0);
            }

            return result;
        }


        public static Activity? StartFindTagsActivity(
            this ActivitySource source,
            string adapterId,
            FindTagsRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(ITagSearch), nameof(ITagSearch.FindTags)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetPagingTags(request);
                result.SetTagWithNamespace("tags.result_fields", request.ResultFields);
            }

            return result;
        }

    }

}

