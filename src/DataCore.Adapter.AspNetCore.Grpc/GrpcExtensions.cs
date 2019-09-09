using System;
using System.Linq;
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
                Category = tag.Category,
                DataType = tag.DataType.ToGrpcTagDataType(),
                Description = tag.Description ?? string.Empty,
                Id = tag.Id ?? string.Empty,
                Name = tag.Name ?? string.Empty,
                Units = tag.Units ?? string.Empty
            };

            if (tag.Labels != null) {
                foreach (var item in tag.Labels) {
                    if (string.IsNullOrEmpty(item)) {
                        continue;
                    }
                    result.Labels.Add(item);
                }
            }

            if (tag.Properties != null) {
                foreach (var item in tag.Properties) {
                    result.Properties.Add(item.Key, item.Value ?? string.Empty);
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
        /// Converts from an adapter asset mode node to its gRPC equivalent.
        /// </summary>
        /// <param name="node">
        ///   The adapter asset model node.
        /// </param>
        /// <returns>
        ///   The gRPC asset model node.
        /// </returns>
        internal static AssetModelNode ToGrpcAssetModelNode(this AssetModel.Models.AssetModelNode node) {
            var result = new AssetModelNode() {
                Id = node.Id ?? string.Empty,
                Name = node.Name ?? string.Empty,
                Description = node.Description ?? string.Empty,
                Parent = node.Parent ?? string.Empty
            };

            if (node.Children.Any()) {
                result.Children.AddRange(node.Children);
            }
            if (node.Measurements.Any()) {
                foreach (var item in node.Measurements) {
                    result.Measurements.Add(new AssetModelNodeMeasurement() {
                        Name = item.Name ?? string.Empty,
                        AdapterId = item.AdapterId ?? string.Empty,
                        Tag = new TagIdentifier() {
                            Id = item.Tag?.Id ?? string.Empty,
                            Name = item.Tag?.Name ?? string.Empty
                        }
                    });
                }
            }
            if (node.Properties?.Count > 0) {
                foreach (var item in node.Properties) {
                    result.Properties.Add(item.Key, item.Value ?? string.Empty);
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
        /// <param name="tagId">
        ///   The ID of the tag.
        /// </param>
        /// <param name="tagName">
        ///   The name of the tag.
        /// </param>
        /// <param name="queryType">
        ///   The type of query that was used to retrieve the value.
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
                    result.Value.Properties.Add(item.Key, item.Value ?? string.Empty);
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
        /// <param name="tagId">
        ///   The ID of the tag.
        /// </param>
        /// <param name="tagName">
        ///   The name of the tag.
        /// </param>
        /// <param name="dataFunction">
        ///   The data function that was used to calculate the value.
        /// </param>
        /// <param name="queryType">
        ///   The type of query that was used to retrieve the value.
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
                    result.Value.Properties.Add(item.Key, item.Value ?? string.Empty);
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
                    writeRequest.Status.ToAdapterTagValueStatus(),
                    writeRequest.Units
                )
            };
        }


        /// <summary>
        /// Converts from an adapter write tag value result to its gRPC equivalent.
        /// </summary>
        /// <param name="adapterResult">
        ///   The adapter write tag value result.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter ID that the event message was written to.
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
                WriteStatus = adapterResult.Status.ToGrpcWriteOperationStatus()
            };

            if (adapterResult.Properties != null) {
                foreach (var item in adapterResult.Properties) {
                    result.Properties.Add(item.Key, item.Value ?? string.Empty);
                }
            }

            return result;
        }


        /// <summary>
        /// Converts from an adapter write status to its gRPC equivalent.
        /// </summary>
        /// <param name="status">
        ///   The adapter write status.
        /// </param>
        /// <returns>
        ///   The gRPC write status.
        /// </returns>
        internal static WriteOperationStatus ToGrpcWriteOperationStatus(this Common.Models.WriteStatus status) {
            switch (status) {
                case Common.Models.WriteStatus.Fail:
                    return WriteOperationStatus.Fail;
                case Common.Models.WriteStatus.Pending:
                    return WriteOperationStatus.Pending;
                case Common.Models.WriteStatus.Success:
                    return WriteOperationStatus.Success;
                case Common.Models.WriteStatus.Unknown:
                default:
                    return WriteOperationStatus.Unknown;
            }
        }


        /// <summary>
        /// Converts from an adapter tag value annotation query result to its gRPC equivalent.
        /// </summary>
        /// <param name="annotation">
        ///   The adapter tag value annotation query result.
        /// </param>
        /// <returns>
        ///   The gRPC tag value annotation query result.
        /// </returns>
        internal static TagValueAnnotationQueryResult ToGrpcTagValueAnnotationQueryResult(this RealTimeData.Models.TagValueAnnotationQueryResult annotation) {
            var result = new TagValueAnnotationQueryResult() {
                TagId = annotation.TagId ?? string.Empty,
                TagName = annotation.TagName ?? string.Empty,
                Annotation = annotation.Annotation.ToGrpcTagValueAnnotation()
            };

            return result;
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
        internal static TagValueAnnotation ToGrpcTagValueAnnotation(this RealTimeData.Models.TagValueAnnotation annotation) {
            if (annotation == null) {
                return null;
            }

            var result = new TagValueAnnotation() {
                Id = annotation.Id,
                Annotation = new TagValueAnnotationBase() {
                    Description = annotation.Description ?? string.Empty,
                    HasUtcEndTime = annotation.UtcEndTime.HasValue,
                    UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annotation.UtcStartTime),
                    Value = annotation.Value
                }
            };

            if (result.Annotation.HasUtcEndTime) {
                result.Annotation.UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annotation.UtcEndTime.Value);
            }

            if (annotation.Properties != null) {
                foreach (var item in annotation.Properties) {
                    result.Annotation.Properties.Add(item.Key, item.Value ?? string.Empty);
                }
            }

            return result;
        }


        /// <summary>
        /// Converts from a gRPC tag value annotation to its adapter equivalent.
        /// </summary>
        /// <param name="annotation">
        ///   The gRPC tag value annotation.
        /// </param>
        /// <returns>
        ///   The adapter tag value annotation.
        /// </returns>
        internal static RealTimeData.Models.TagValueAnnotationBase ToAdapterTagValueAnnotation(this TagValueAnnotationBase annotation) {
            if (annotation == null) {
                return null;
            }

            return new RealTimeData.Models.TagValueAnnotationBase(
                annotation.AnnotationType.ToAdapterAnnotationType(),
                annotation.UtcStartTime.ToDateTime(),
                annotation.HasUtcEndTime
                    ? annotation.UtcEndTime.ToDateTime()
                    : (DateTime?) null,
                annotation.Value,
                annotation.Description,
                annotation.Properties
            );
        }


        /// <summary>
        /// Converts from a gRPC tag value annotation type to its gRPC equivalent.
        /// </summary>
        /// <param name="annotationType">
        ///   The gRPC tag value annotation type.
        /// </param>
        /// <returns>
        ///   The adapter tag value annotation type.
        /// </returns>
        internal static RealTimeData.Models.AnnotationType ToAdapterAnnotationType(this AnnotationType annotationType) {
            switch (annotationType) {
                case AnnotationType.TimeRange:
                    return RealTimeData.Models.AnnotationType.TimeRange;
                case AnnotationType.Instantaneous:
                default:
                    return RealTimeData.Models.AnnotationType.Instantaneous;
            }
        }


        /// <summary>
        /// Converts from an adapter tag value annotation write result to its gRPC equivalent.
        /// </summary>
        /// <param name="adapterResult">
        ///   The adapter tag value annotation write result.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <returns>
        ///   The gRPC tag value annotation write result.
        /// </returns>
        internal static WriteTagValueAnnotationResult ToGrpcWriteTagValueAnnotationResult(this RealTimeData.Models.WriteTagValueAnnotationResult adapterResult, string adapterId) {
            if (adapterResult == null) {
                return null;
            }

            var result = new WriteTagValueAnnotationResult() {
                AdapterId = adapterId ?? string.Empty,
                TagId = adapterResult.TagId ?? string.Empty,
                AnnotationId = adapterResult.AnnotationId ?? string.Empty,
                WriteStatus = adapterResult.Status.ToGrpcWriteOperationStatus(),
                Notes = adapterResult.Notes ?? string.Empty
            };

            if (adapterResult.Properties.Count > 0) {
                foreach (var item in adapterResult.Properties) {
                    result.Properties.Add(item.Key, item.Value ?? string.Empty);
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
        internal static EventMessage ToGrpcEventMessage(this Events.Models.EventMessageBase message) {
            var result = new EventMessage() {
                Category = message.Category ?? string.Empty,
                Id = message.Id ?? string.Empty,
                Message = message.Message ?? string.Empty,
                Priority = message.Priority.ToGrpcEventPriority(),
                UtcEventTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(message.UtcEventTime)
            };

            if (message.Properties != null) {
                foreach (var item in message.Properties) {
                    result.Properties.Add(item.Key, item.Value ?? string.Empty);
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


        /// <summary>
        /// Converts from a gRPC event message priority to its adapter equivalent.
        /// </summary>
        /// <param name="priority">
        ///   The gRPC event message priority.
        /// </param>
        /// <returns>
        ///   The adapter event message priority.
        /// </returns>
        internal static Events.Models.EventPriority ToAdapterEventPriority(this EventPriority priority) {
            switch (priority) {
                case EventPriority.Low:
                    return Events.Models.EventPriority.Low;
                case EventPriority.Medium:
                    return Events.Models.EventPriority.Medium;
                case EventPriority.High:
                    return Events.Models.EventPriority.High;
                case EventPriority.Critical:
                    return Events.Models.EventPriority.Critical;
                case EventPriority.Unknown:
                default:
                    return Events.Models.EventPriority.Unknown;
            }
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
        internal static Events.Models.WriteEventMessageItem ToAdapterWriteEventMessageItem(this WriteEventMessageRequest writeRequest) {
            return new Events.Models.WriteEventMessageItem() {
                CorrelationId = writeRequest.CorrelationId,
                EventMessage = new Events.Models.EventMessage(
                    writeRequest.Message?.Id,
                    writeRequest.Message?.UtcEventTime?.ToDateTime() ?? DateTime.MinValue,
                    writeRequest.Message?.Priority.ToAdapterEventPriority() ?? Events.Models.EventPriority.Unknown,
                    writeRequest.Message?.Category,
                    writeRequest.Message?.Message,
                    writeRequest.Message?.Properties
                )
            };
        }


        /// <summary>
        /// Converts from an adapter write event message result to its gRPC equivalent.
        /// </summary>
        /// <param name="adapterResult">
        ///   The adapter write event message result.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter ID that the event message was written to.
        /// </param>
        /// <returns>
        ///   The gRPC write event message result.
        /// </returns>
        internal static WriteEventMessageResult ToGrpcWriteEventMessageResult(this Events.Models.WriteEventMessageResult adapterResult, string adapterId) {
            var result = new WriteEventMessageResult() {
                AdapterId = adapterId ?? string.Empty,
                CorrelationId = adapterResult.CorrelationId ?? string.Empty,
                Notes = adapterResult.Notes ?? string.Empty,
                WriteStatus = adapterResult.Status.ToGrpcWriteOperationStatus()
            };

            if (adapterResult.Properties != null) {
                foreach (var item in adapterResult.Properties) {
                    result.Properties.Add(item.Key, item.Value ?? string.Empty);
                }
            }

            return result;
        }

    }
}
