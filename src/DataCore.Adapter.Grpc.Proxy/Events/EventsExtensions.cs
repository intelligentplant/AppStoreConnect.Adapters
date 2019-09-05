using DataCore.Adapter.Grpc.Proxy.Common;

namespace DataCore.Adapter.Grpc.Proxy.Events {
    internal static class EventsExtensions {

        internal static Adapter.Events.Models.EventMessage ToAdapterEventMessage(this EventMessage eventMessage) {
            if (eventMessage == null) {
                return null;
            }

            return new Adapter.Events.Models.EventMessage(
                eventMessage.Id,
                eventMessage.UtcEventTime.ToDateTime(),
                eventMessage.Priority.ToAdapterEventPriority(),
                eventMessage.Category,
                eventMessage.Message,
                eventMessage.Properties
            );
        }


        internal static Adapter.Events.Models.EventMessageWithCursorPosition ToAdapterEventMessage(this EventMessageWithCursorPosition eventMessage) {
            if (eventMessage == null) {
                return null;
            }

            return new Adapter.Events.Models.EventMessageWithCursorPosition(
                eventMessage.EventMessage.Id,
                eventMessage.EventMessage.UtcEventTime.ToDateTime(),
                eventMessage.EventMessage.Priority.ToAdapterEventPriority(),
                eventMessage.EventMessage.Category,
                eventMessage.EventMessage.Message,
                eventMessage.EventMessage.Properties,
                eventMessage.CursorPosition
            );
        }


        internal static Adapter.Events.Models.EventPriority ToAdapterEventPriority(this EventPriority eventPriority) {
            switch (eventPriority) {
                case EventPriority.Low:
                    return Adapter.Events.Models.EventPriority.Low;
                case EventPriority.Medium:
                    return Adapter.Events.Models.EventPriority.Medium;
                case EventPriority.High:
                    return Adapter.Events.Models.EventPriority.High;
                case EventPriority.Critical:
                    return Adapter.Events.Models.EventPriority.Critical;
                case EventPriority.Unknown:
                default:
                    return Adapter.Events.Models.EventPriority.Unknown;
            }
        }


        internal static EventReadDirection ToGrpcReadDirection(this Adapter.Events.Models.EventReadDirection readDirection) {
            switch (readDirection) {
                case Adapter.Events.Models.EventReadDirection.Backwards:
                    return EventReadDirection.Backwards;
                case Adapter.Events.Models.EventReadDirection.Forwards:
                default:
                    return EventReadDirection.Forwards;
            }
        }


        internal static WriteEventMessageRequest ToGrpcWriteEventMessageItem(this Adapter.Events.Models.WriteEventMessageItem item, string adapterId) {
            if (item == null) {
                return null;
            }

            return new WriteEventMessageRequest() {
                AdapterId = adapterId,
                CorrelationId = item.CorrelationId,
                Message = item.EventMessage.ToGrpcEventMessage()
            };
        }


        internal static Adapter.Events.Models.WriteEventMessageResult ToAdapterWriteEventMessageResult(this WriteEventMessageResult result) {
            if (result == null) {
                return null;
            }

            return new Adapter.Events.Models.WriteEventMessageResult(
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
        internal static EventMessage ToGrpcEventMessage(this Adapter.Events.Models.EventMessageBase message) {
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
        internal static EventPriority ToGrpcEventPriority(this Adapter.Events.Models.EventPriority priority) {
            switch (priority) {
                case Adapter.Events.Models.EventPriority.Low:
                    return EventPriority.Low;
                case Adapter.Events.Models.EventPriority.Medium:
                    return EventPriority.Medium;
                case Adapter.Events.Models.EventPriority.High:
                    return EventPriority.High;
                case Adapter.Events.Models.EventPriority.Critical:
                    return EventPriority.Critical;
                case Adapter.Events.Models.EventPriority.Unknown:
                default:
                    return EventPriority.Unknown;
            }
        }

    }
}
