using System;
using System.Diagnostics;
using System.Linq;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.Diagnostics.Events {

    /// <summary>
    /// Extensions for <see cref="ActivitySource"/> related to event operations.
    /// </summary>
    public static class EventsActivitySourceExtensions {

        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IEventMessagePush.Subscribe"/> call.
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


        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IEventMessagePushWithTopics.Subscribe"/> call.
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


        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IReadEventMessagesForTimeRange.ReadEventMessagesForTimeRange"/> call.
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


        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IReadEventMessagesUsingCursor.ReadEventMessagesUsingCursor"/> call.
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


        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IWriteEventMessages.WriteEventMessages"/> call.
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
        public static Activity? StartWriteEventMessagesActivity(
            this ActivitySource source,
            string adapterId,
            WriteEventMessagesRequest request,
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
