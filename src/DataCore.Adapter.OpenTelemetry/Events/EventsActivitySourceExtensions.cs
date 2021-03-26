using System;
using System.Diagnostics;
using System.Linq;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.Diagnostics.Events {
    public static class EventsActivitySourceExtensions {

        public static Activity? StartEventMessagePushSubscribeActivity(
            this ActivitySource source,
            string adapterId,
            CreateEventMessageSubscriptionRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IEventMessagePush), nameof(IEventMessagePush.Subscribe)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);
            result.SetSubscriptionFlag();

            if (request != null) {
                result.SetTagWithNamespace("events.subscription_type", request.SubscriptionType);
            }

            return result;
        }


        public static Activity? StartEventMessagePushWithTopicsSubscribeActivity(
            this ActivitySource source,
            string adapterId,
            CreateEventMessageTopicSubscriptionRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IEventMessagePush), nameof(IEventMessagePush.Subscribe)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);
            result.SetSubscriptionFlag();

            if (request != null) {
                result.SetTagWithNamespace("events.subscription_type", request.SubscriptionType);
            }

            return result;
        }


        public static Activity? StartReadEventMessagesForTimeRangeActivity(
            this ActivitySource source,
            string adapterId,
            ReadEventMessagesForTimeRangeRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IReadEventMessagesForTimeRange), nameof(IReadEventMessagesForTimeRange.ReadEventMessagesForTimeRange)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetQueryTimeRangeTags(request.UtcStartTime, request.UtcEndTime);
                result.SetTagWithNamespace("events.read_direction", request.Direction);
                result.SetRequestItemCountTag(request.Topics?.Count() ?? 0);
                result.SetPagingTags(request);
            }

            return result;
        }


        public static Activity? StartReadEventMessagesUsingCursorActivity(
            this ActivitySource source,
            string adapterId,
            ReadEventMessagesUsingCursorRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IReadEventMessagesUsingCursor), nameof(IReadEventMessagesUsingCursor.ReadEventMessagesUsingCursor)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            if (request != null) {
                result.SetTagWithNamespace("events.read_direction", request.Direction);
                result.SetPageSizeTag(request.PageSize);
            }

            return result;
        }


        public static Activity? StartWriteEventMessagesActivity(
            this ActivitySource source,
            string adapterId,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IWriteEventMessages), nameof(IWriteEventMessages.WriteEventMessages)),
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

    }
}
