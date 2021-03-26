using System;
using System.Diagnostics;
using System.Linq;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Diagnostics.RealTimeData {
    public static class RealTimeDataActivitySourceExtensions {

        public static Activity? StartReadPlotTagValuesActivity(
            this ActivitySource source,
            string adapterId,
            ReadPlotTagValuesRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IReadPlotTagValues), nameof(IReadPlotTagValues.ReadPlotTagValues)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetQueryTimeRangeTags(request.UtcStartTime, request.UtcEndTime);
                result.SetTagWithNamespace("realtimedata.intervals", request.Intervals);
                result.SetRequestItemCountTag(request.Tags?.Count() ?? 0);
            }

            return result;
        }


        public static Activity? StartGetSupportedDataFunctionsActivity(
            this ActivitySource source,
            string adapterId,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IReadProcessedTagValues), nameof(IReadProcessedTagValues.GetSupportedDataFunctions)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            return result;
        }


        public static Activity? StartReadProcessedTagValuesActivity(
            this ActivitySource source,
            string adapterId,
            ReadProcessedTagValuesRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IReadProcessedTagValues), nameof(IReadProcessedTagValues.ReadProcessedTagValues)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetQueryTimeRangeTags(request.UtcStartTime, request.UtcEndTime);
                result.SetTagWithNamespace("realtimedata.sample_interval", request.SampleInterval);
                result.SetTagWithNamespace("realtimedata.data_functions", string.Join(", ", request.DataFunctions));
                result.SetRequestItemCountTag(request.Tags?.Count() ?? 0);
            }

            return result;
        }


        public static Activity? StartReadRawTagValuesActivity(
            this ActivitySource source,
            string adapterId,
            ReadRawTagValuesRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IReadRawTagValues), nameof(IReadRawTagValues.ReadRawTagValues)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetQueryTimeRangeTags(request.UtcStartTime, request.UtcEndTime);
                result.SetTagWithNamespace("realtimedata.sample_count", request.SampleCount);
                result.SetTagWithNamespace("realtimedata.boundary_type", request.BoundaryType);
                result.SetRequestItemCountTag(request.Tags?.Count() ?? 0);
            }

            return result;
        }


        public static Activity? StartReadSnapshotTagValuesActivity(
            this ActivitySource source,
            string adapterId,
            ReadSnapshotTagValuesRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IReadSnapshotTagValues), nameof(IReadSnapshotTagValues.ReadSnapshotTagValues)),
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


        public static Activity? StartReadAnnotationsActivity(
            this ActivitySource source,
            string adapterId,
            ReadAnnotationsRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IReadTagValueAnnotations), nameof(IReadTagValueAnnotations.ReadAnnotations)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetQueryTimeRangeTags(request.UtcStartTime, request.UtcEndTime);
                result.SetRequestItemCountTag(request.Tags?.Count() ?? 0);
                result.SetTagWithNamespace("realtimedata.annotation_count", request.AnnotationCount);
            }

            return result;
        }


        public static Activity? StartReadAnnotationActivity(
            this ActivitySource source,
            string adapterId,
            ReadAnnotationRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IReadTagValueAnnotations), nameof(IReadTagValueAnnotations.ReadAnnotation)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetTagWithNamespace("realtimedata.tag_id", request.Tag);
                result.SetTagWithNamespace("realtimedata.annotation_id", request.AnnotationId);
            }

            return result;
        }


        public static Activity? StartReadTagValuesAtTimesActivity(
            this ActivitySource source,
            string adapterId,
            ReadTagValuesAtTimesRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IReadTagValuesAtTimes), nameof(IReadTagValuesAtTimes.ReadTagValuesAtTimes)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetTagWithNamespace("realtimedata.sample_time_count", request.UtcSampleTimes?.Count() ?? 0);
                result.SetRequestItemCountTag(request.Tags?.Count() ?? 0);
            }

            return result;
        }


        public static Activity? StartSnapshotTagValuePushSubscribeActivity(
            this ActivitySource source,
            string adapterId,
            CreateSnapshotTagValueSubscriptionRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(ISnapshotTagValuePush), nameof(ISnapshotTagValuePush.Subscribe)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);
            result.SetSubscriptionFlag();

            if (request != null) {
                result.SetPublishIntervalTag(request.PublishInterval);
            }

            return result;
        }


        public static Activity? StartWriteHistoricalTagValuesActivity(
            this ActivitySource source,
            string adapterId,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IWriteHistoricalTagValues), nameof(IWriteHistoricalTagValues.WriteHistoricalTagValues)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);
            result.SetSubscriptionFlag();

            return result;
        }


        public static Activity? StartWriteSnapshotTagValuesActivity(
            this ActivitySource source,
            string adapterId,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IWriteSnapshotTagValues), nameof(IWriteSnapshotTagValues.WriteSnapshotTagValues)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);
            result.SetSubscriptionFlag();

            return result;
        }


        public static Activity? StartCreateAnnotationActivity(
            this ActivitySource source,
            string adapterId,
            CreateAnnotationRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IWriteTagValueAnnotations), nameof(IWriteTagValueAnnotations.CreateAnnotation)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            return result;
        }


        public static Activity? StartUpdateAnnotationActivity(
            this ActivitySource source,
            string adapterId,
            UpdateAnnotationRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IWriteTagValueAnnotations), nameof(IWriteTagValueAnnotations.UpdateAnnotation)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetTagWithNamespace("realtimedata.tag_id", request.Tag);
                result.SetTagWithNamespace("realtimedata.annotation_id", request.AnnotationId);
            }

            return result;
        }


        public static Activity? StartDeleteAnnotationActivity(
            this ActivitySource source,
            string adapterId,
            DeleteAnnotationRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IWriteTagValueAnnotations), nameof(IWriteTagValueAnnotations.DeleteAnnotation)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetTagWithNamespace("realtimedata.tag_id", request.Tag);
                result.SetTagWithNamespace("realtimedata.annotation_id", request.AnnotationId);
            }

            return result;
        }

    }
}
