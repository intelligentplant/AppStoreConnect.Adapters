
using System.Linq;
using DataCore.Adapter.Common;
using DataCore.Adapter.Grpc;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Extension methods for converting from gRPC types to their adapter equivalents, and vice versa.
    /// </summary>
    public static class GrpcEventsExtensions {

        public static EventMessage ToAdapterEventMessage(this Grpc.EventMessage eventMessage) {
            if (eventMessage == null) {
                return null;
            }

            return EventMessage.Create(
                eventMessage.Id,
                eventMessage.UtcEventTime.ToDateTime(),
                eventMessage.Priority.ToAdapterEventPriority(),
                eventMessage.Category,
                eventMessage.Message,
                eventMessage.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


        public static EventMessageWithCursorPosition ToAdapterEventMessage(this Grpc.EventMessageWithCursorPosition eventMessage) {
            if (eventMessage == null) {
                return null;
            }

            return EventMessageWithCursorPosition.Create(
                eventMessage.EventMessage.Id,
                eventMessage.EventMessage.UtcEventTime.ToDateTime(),
                eventMessage.EventMessage.Priority.ToAdapterEventPriority(),
                eventMessage.EventMessage.Category,
                eventMessage.EventMessage.Message,
                eventMessage.EventMessage.Properties.Select(x => x.ToAdapterProperty()).ToArray(),
                eventMessage.CursorPosition
            );
        }


        public static EventPriority ToAdapterEventPriority(this Grpc.EventPriority eventPriority) {
            switch (eventPriority) {
                case Grpc.EventPriority.Low:
                    return EventPriority.Low;
                case Grpc.EventPriority.Medium:
                    return EventPriority.Medium;
                case Grpc.EventPriority.High:
                    return EventPriority.High;
                case Grpc.EventPriority.Critical:
                    return EventPriority.Critical;
                case Grpc.EventPriority.Unknown:
                default:
                    return EventPriority.Unknown;
            }
        }


        public static Grpc.EventReadDirection ToGrpcReadDirection(this EventReadDirection readDirection) {
            switch (readDirection) {
                case EventReadDirection.Backwards:
                    return Grpc.EventReadDirection.Backwards;
                case EventReadDirection.Forwards:
                default:
                    return Grpc.EventReadDirection.Forwards;
            }
        }


        public static Grpc.WriteEventMessageRequest ToGrpcWriteEventMessageItem(this WriteEventMessageItem item, string adapterId) {
            if (item == null) {
                return null;
            }

            return new WriteEventMessageRequest() {
                AdapterId = adapterId,
                CorrelationId = item.CorrelationId ?? string.Empty,
                Message = item.EventMessage.ToGrpcEventMessage()
            };
        }


        public static WriteEventMessageResult ToAdapterWriteEventMessageResult(this Grpc.WriteEventMessageResult result) {
            if (result == null) {
                return null;
            }

            return WriteEventMessageResult.Create(
                result.CorrelationId,
                result.WriteStatus.ToAdapterWriteStatus(),
                result.Notes,
                result.Properties.Select(x => x.ToAdapterProperty()).ToArray()
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
        public static Grpc.EventMessage ToGrpcEventMessage(this EventMessageBase message) {
            if (message == null) {
                return null;
            }

            var result = new Grpc.EventMessage() {
                Category = message.Category ?? string.Empty,
                Id = message.Id ?? string.Empty,
                Message = message.Message ?? string.Empty,
                Priority = message.Priority.ToGrpcEventPriority(),
                UtcEventTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(message.UtcEventTime)
            };

            if (message.Properties != null) {
                foreach (var item in message.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcProperty());
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
        public static Grpc.EventPriority ToGrpcEventPriority(this EventPriority priority) {
            switch (priority) {
                case EventPriority.Low:
                    return Grpc.EventPriority.Low;
                case EventPriority.Medium:
                    return Grpc.EventPriority.Medium;
                case EventPriority.High:
                    return Grpc.EventPriority.High;
                case EventPriority.Critical:
                    return Grpc.EventPriority.Critical;
                case EventPriority.Unknown:
                default:
                    return Grpc.EventPriority.Unknown;
            }
        }

    }
}
