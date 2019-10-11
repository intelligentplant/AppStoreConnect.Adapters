
using DataCore.Adapter.Common;
using DataCore.Adapter.Grpc;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Extension methods for converting from gRPC types to their adapter equivalents, and vice versa.
    /// </summary>
    public static class GrpcEventsExtensions {

        public static Models.EventMessage ToAdapterEventMessage(this EventMessage eventMessage) {
            if (eventMessage == null) {
                return null;
            }

            return Models.EventMessage.Create(
                eventMessage.Id,
                eventMessage.UtcEventTime.ToDateTime(),
                eventMessage.Priority.ToAdapterEventPriority(),
                eventMessage.Category,
                eventMessage.Message,
                eventMessage.Properties
            );
        }


        public static Models.EventMessageWithCursorPosition ToAdapterEventMessage(this EventMessageWithCursorPosition eventMessage) {
            if (eventMessage == null) {
                return null;
            }

            return Models.EventMessageWithCursorPosition.Create(
                eventMessage.EventMessage.Id,
                eventMessage.EventMessage.UtcEventTime.ToDateTime(),
                eventMessage.EventMessage.Priority.ToAdapterEventPriority(),
                eventMessage.EventMessage.Category,
                eventMessage.EventMessage.Message,
                eventMessage.EventMessage.Properties,
                eventMessage.CursorPosition
            );
        }


        public static Models.EventPriority ToAdapterEventPriority(this EventPriority eventPriority) {
            switch (eventPriority) {
                case EventPriority.Low:
                    return Models.EventPriority.Low;
                case EventPriority.Medium:
                    return Models.EventPriority.Medium;
                case EventPriority.High:
                    return Models.EventPriority.High;
                case EventPriority.Critical:
                    return Models.EventPriority.Critical;
                case EventPriority.Unknown:
                default:
                    return Models.EventPriority.Unknown;
            }
        }


        public static EventReadDirection ToGrpcReadDirection(this Models.EventReadDirection readDirection) {
            switch (readDirection) {
                case Models.EventReadDirection.Backwards:
                    return EventReadDirection.Backwards;
                case Models.EventReadDirection.Forwards:
                default:
                    return EventReadDirection.Forwards;
            }
        }


        public static WriteEventMessageRequest ToGrpcWriteEventMessageItem(this Models.WriteEventMessageItem item, string adapterId) {
            if (item == null) {
                return null;
            }

            return new WriteEventMessageRequest() {
                AdapterId = adapterId,
                CorrelationId = item.CorrelationId ?? string.Empty,
                Message = item.EventMessage.ToGrpcEventMessage()
            };
        }


        public static Models.WriteEventMessageResult ToAdapterWriteEventMessageResult(this WriteEventMessageResult result) {
            if (result == null) {
                return null;
            }

            return Models.WriteEventMessageResult.Create(
                result.CorrelationId,
                result.WriteStatus.ToAdapterWriteStatus(),
                result.Notes,
                result.Properties
            );
        }


        /// <summary>
        /// Converts from an adapter event message to its gRPC equivalent.
        /// </summary>
        /// <param name="message">
        ///   The adapter event message.
        /// </param>
        /// <returns>
        ///   The gRPC event message.
        /// </returns>
        public static EventMessage ToGrpcEventMessage(this Models.EventMessageBase message) {
            if (message == null) {
                return null;
            }

            var result = new EventMessage() {
                Category = message.Category ?? string.Empty,
                Id = message.Id ?? string.Empty,
                Message = message.Message ?? string.Empty,
                Priority = message.Priority.ToGrpcEventPriority(),
                UtcEventTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(message.UtcEventTime)
            };

            if (message.Properties != null) {
                foreach (var item in message.Properties) {
                    result.Properties.Add(item.Key, item.Value);
                }
            }

            return result;
        }


        /// <summary>
        /// Converts from an adapter event message priority to its gRPC equivalent.
        /// </summary>
        /// <param name="priority">
        ///   The adapter event message priority.
        /// </param>
        /// <returns>
        ///   The gRPC event message priority.
        /// </returns>
        public static EventPriority ToGrpcEventPriority(this Models.EventPriority priority) {
            switch (priority) {
                case Models.EventPriority.Low:
                    return EventPriority.Low;
                case Models.EventPriority.Medium:
                    return EventPriority.Medium;
                case Models.EventPriority.High:
                    return EventPriority.High;
                case Models.EventPriority.Critical:
                    return EventPriority.Critical;
                case Models.EventPriority.Unknown:
                default:
                    return EventPriority.Unknown;
            }
        }

    }
}
