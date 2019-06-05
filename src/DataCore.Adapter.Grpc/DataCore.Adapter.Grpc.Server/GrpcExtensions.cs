using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Grpc.Server {

    /// <summary>
    /// Extension methods for converting from common adapter types to their gRPC-equivalents.
    /// </summary>
    internal static class GrpcExtensions {

        /// <summary>
        /// Converts from an adapter tag definition into its gRPC equivalent.
        /// </summary>
        /// <param name="tag">
        ///   The adapter tag definition.
        /// </param>
        /// <returns>
        ///   The gRPC tag definition.
        /// </returns>
        internal static TagDefinition ToGrpcTagDefinition(this RealTimeData.Models.TagDefinition tag) {
            var result = new TagDefinition() {
                DataType = tag.DataType.ToGrpcTagDataType(),
                Description = tag.Description ?? string.Empty,
                Id = tag.Id ?? string.Empty,
                Name = tag.Name ?? string.Empty,
                Units = tag.Units ?? string.Empty
            };

            if (tag.Properties != null) {
                foreach (var item in tag.Properties) {
                    result.Properties.Add(item.Key, item.Value);
                }
            }

            if (tag.States != null) {
                foreach (var item in tag.States) {
                    result.States.Add(item.Key, item.Value);
                }
            }

            return result;
        }


        /// <summary>
        /// Converts from an adapter tag data type to its gRPC equivalent.
        /// </summary>
        /// <param name="tagDataType">
        ///   The adapter tag data type.
        /// </param>
        /// <returns>
        ///   The gRPC tag data type.
        /// </returns>
        internal static TagDataType ToGrpcTagDataType(this RealTimeData.Models.TagDataType tagDataType) {
            switch (tagDataType) {
                case RealTimeData.Models.TagDataType.Numeric:
                    return TagDataType.Numeric;
                case RealTimeData.Models.TagDataType.State:
                    return TagDataType.State;
                case RealTimeData.Models.TagDataType.Text:
                    return TagDataType.Text;
                default:
                    return TagDataType.Numeric;
            }
        }


        /// <summary>
        /// Converts from an adapter tag value to its gRPC equivalent.
        /// </summary>
        /// <param name="value">
        ///   The adapter tag value.
        /// </param>
        /// <returns>
        ///   The gRPC tag value.
        /// </returns>
        internal static TagValueQueryResult ToGrpcTagValue(this RealTimeData.Models.TagValue value, string tagId, string tagName, TagValueQueryType queryType) {
            var result = new TagValueQueryResult() {
                TagId = tagId ?? string.Empty,
                TagName = tagName ?? string.Empty,
                QueryType = queryType,
                Value = new TagValue() {
                    Error = value.Error ?? string.Empty,
                    Notes = value.Notes ?? string.Empty,
                    NumericValue = value.NumericValue,
                    Status = value.Status.ToGrpcTagValueStatus(),
                    TextValue = value.TextValue ?? string.Empty,
                    Units = value.Units ?? string.Empty,
                    UtcSampleTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(value.UtcSampleTime)
                }
            };

            if (value.Properties != null) {
                foreach (var item in value.Properties) {
                    result.Value.Properties.Add(item.Key, item.Value);
                }
            }

            return result;
        }


        /// <summary>
        /// Converts from an adapter tag value to its gRPC equivalent.
        /// </summary>
        /// <param name="value">
        ///   The adapter tag value.
        /// </param>
        /// <returns>
        ///   The gRPC tag value.
        /// </returns>
        internal static ProcessedTagValueQueryResult ToGrpcProcessedTagValue(this RealTimeData.Models.TagValue value, string tagId, string tagName, string dataFunction, TagValueQueryType queryType) {
            var result = new ProcessedTagValueQueryResult() {
                TagId = tagId ?? string.Empty,
                TagName = tagName ?? string.Empty,
                DataFunction = dataFunction ?? string.Empty,
                QueryType = queryType,
                Value = new TagValue() {
                    Error = value.Error ?? string.Empty,
                    Notes = value.Notes ?? string.Empty,
                    NumericValue = value.NumericValue,
                    Status = value.Status.ToGrpcTagValueStatus(),
                    TextValue = value.TextValue ?? string.Empty,
                    Units = value.Units ?? string.Empty,
                    UtcSampleTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(value.UtcSampleTime)
                }
            };

            if (value.Properties != null) {
                foreach (var item in value.Properties) {
                    result.Value.Properties.Add(item.Key, item.Value);
                }
            }

            return result;
        }


        /// <summary>
        /// Converts from an adapter tag value status to its gRPC equivalent.
        /// </summary>
        /// <param name="status">
        ///   The adapter tag value status.
        /// </param>
        /// <returns>
        ///   The gRPC tag value status.
        /// </returns>
        internal static TagValueStatus ToGrpcTagValueStatus(this RealTimeData.Models.TagValueStatus status) {
            switch (status) {
                case RealTimeData.Models.TagValueStatus.Bad:
                    return TagValueStatus.Bad;
                case RealTimeData.Models.TagValueStatus.Good:
                    return TagValueStatus.Good;
                case RealTimeData.Models.TagValueStatus.Unknown:
                default:
                    return TagValueStatus.Unknown;
            }
        }


        /// <summary>
        /// Converts from a gRPC tag value status to its adapter equivalent.
        /// </summary>
        /// <param name="status">
        ///   The gRPC tag value status.
        /// </param>
        /// <returns>
        ///   The adapter tag value status.
        /// </returns>
        internal static RealTimeData.Models.TagValueStatus ToAdapterTagValueStatus(this TagValueStatus status) {
            switch (status) {
                case TagValueStatus.Bad:
                    return RealTimeData.Models.TagValueStatus.Bad;
                case TagValueStatus.Good:
                    return RealTimeData.Models.TagValueStatus.Good;
                case TagValueStatus.Unknown:
                default:
                    return RealTimeData.Models.TagValueStatus.Unknown;
            }
        }


        /// <summary>
        /// Converts from a gRPC raw data boundary type to its adapter equivalent.
        /// </summary>
        /// <param name="boundaryType">
        ///   The gRPC boundary type.
        /// </param>
        /// <returns>
        ///   The adapter boundary type.
        /// </returns>
        internal static RealTimeData.Models.RawDataBoundaryType FromGrpcRawDataBoundaryType(this RawDataBoundaryType boundaryType) {
            switch (boundaryType) {
                case RawDataBoundaryType.Outside:
                    return RealTimeData.Models.RawDataBoundaryType.Outside;
                case RawDataBoundaryType.Inside:
                default:
                    return RealTimeData.Models.RawDataBoundaryType.Inside;
            }
        }


        /// <summary>
        /// Converts from an adapter data function descriptor to its gRPC equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The adapter data function descriptor.
        /// </param>
        /// <returns>
        ///   The gRPC data function descriptor.
        /// </returns>
        internal static DataFunctionDescriptor ToGrpcDataFunctionDescriptor(this RealTimeData.Models.DataFunctionDescriptor descriptor) {
            return new DataFunctionDescriptor() {
                Name = descriptor.Name ?? string.Empty,
                Description = descriptor.Description ?? string.Empty
            };
        }


        /// <summary>
        /// Converts from a gRPC write tag value item to its adapter equivalent.
        /// </summary>
        /// <param name="writeRequest">
        ///   The gRPC tag value write item.
        /// </param>
        /// <returns>
        ///   The adapter tag value write item.
        /// </returns>
        internal static WriteTagValueItem ToAdapterWriteTagValueItem(this WriteTagValueRequest writeRequest) {
            return new WriteTagValueItem() {
                CorrelationId = writeRequest.CorrelationId,
                TagId = writeRequest.TagId,
                Value = new TagValueBase(
                    writeRequest.UtcSampleTime.ToDateTime(),
                    writeRequest.NumericValue,
                    writeRequest.TextValue,
                    writeRequest.Status.ToAdapterTagValueStatus()
                )
            };
        }


        /// <summary>
        /// Converts from an adapter write tag value result to its gRPC equivalent.
        /// </summary>
        /// <param name="adapterResult">
        ///   The adapter write tag value result.
        /// </param>
        /// <returns>
        ///   The gRPC write tag value result.
        /// </returns>
        internal static WriteTagValueResult ToGrpcWriteTagValueResult(this RealTimeData.Models.WriteTagValueResult adapterResult, string adapterId) {
            var result = new WriteTagValueResult() {
                AdapterId = adapterId ?? string.Empty,
                CorrelationId = adapterResult.CorrelationId ?? string.Empty,
                Notes = adapterResult.Notes ?? string.Empty,
                TagId = adapterResult.TagId ?? string.Empty,
                WriteStatus = adapterResult.Status.ToGrpcTagValueWriteStatus()
            };

            if (adapterResult.Properties != null) {
                foreach (var item in adapterResult.Properties) {
                    result.Properties.Add(item.Key, item.Value);
                }
            }

            return result;
        }


        /// <summary>
        /// Converts from an adapter write tag value status to its gRPC equivalent.
        /// </summary>
        /// <param name="status">
        ///   The adapter write status.
        /// </param>
        /// <returns>
        ///   The gRPC write tag value status.
        /// </returns>
        internal static TagValueWriteStatus ToGrpcTagValueWriteStatus(this Common.Models.WriteStatus status) {
            switch (status) {
                case Common.Models.WriteStatus.Fail:
                    return TagValueWriteStatus.Fail;
                case Common.Models.WriteStatus.Pending:
                    return TagValueWriteStatus.Pending;
                case Common.Models.WriteStatus.Success:
                    return TagValueWriteStatus.Success;
                case Common.Models.WriteStatus.Unknown:
                default:
                    return TagValueWriteStatus.Unknown;
            }
        }


        /// <summary>
        /// Converts from an adapter tag value annotation to its gRPC equivalent.
        /// </summary>
        /// <param name="annotation">
        ///   The adapter tag value annotation.
        /// </param>
        /// <returns>
        ///   The gRPC tag value annotation.
        /// </returns>
        internal static TagValueAnnotationQueryResult ToGrpcTagValueAnnotation(this RealTimeData.Models.TagValueAnnotationQueryResult annotation) {
            var result = new TagValueAnnotationQueryResult() {
                TagId = annotation.TagId ?? string.Empty,
                TagName = annotation.TagName ?? string.Empty,
                Annotation = new TagValueAnnotation() {
                    Description = annotation.Annotation.Description ?? string.Empty,
                    HasUtcEndTime = annotation.Annotation.UtcEndTime.HasValue,
                    Id = annotation.Annotation.Id,
                    UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annotation.Annotation.UtcStartTime),
                    Value = annotation.Annotation.Value
                }
            };

            if (result.Annotation.HasUtcEndTime) {
                result.Annotation.UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annotation.Annotation.UtcEndTime.Value);
            }

            if (annotation.Annotation.Properties != null) {
                foreach (var item in annotation.Annotation.Properties) {
                    result.Annotation.Properties.Add(item.Key, item.Value);
                }
            }

            return result;
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
        internal static EventMessage ToGrpcEventMessage(this Events.Models.EventMessage message) {
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
        internal static EventPriority ToGrpcEventPriority(this Events.Models.EventPriority priority) {
            switch (priority) {
                case Events.Models.EventPriority.Low:
                    return EventPriority.Low;
                case Events.Models.EventPriority.Medium:
                    return EventPriority.Medium;
                case Events.Models.EventPriority.High:
                    return EventPriority.High;
                case Events.Models.EventPriority.Critical:
                    return EventPriority.Critical;
                case Events.Models.EventPriority.Unknown:
                default:
                    return EventPriority.Unknown;
            }
        }

    }
}
