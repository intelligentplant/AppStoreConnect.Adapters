using DataCore.Adapter.Grpc.Proxy.Common;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData {

    /// <summary>
    /// Extensions for converting from gRPC real-time data types to adapter equivalents.
    /// </summary>
    internal static class RealTimeDataExtensions {

        internal static Adapter.RealTimeData.Models.TagDefinition ToAdapterTagDefinition(this TagDefinition tagDefinition) {
            if (tagDefinition == null) {
                return null;
            }

            return new Adapter.RealTimeData.Models.TagDefinition(
                tagDefinition.Id,
                tagDefinition.Name,
                tagDefinition.Category,
                tagDefinition.Description,
                tagDefinition.Units,
                tagDefinition.DataType.ToAdapterTagDataType(),
                tagDefinition.States,
                tagDefinition.Properties
            );
        }


        internal static Adapter.RealTimeData.Models.TagDataType ToAdapterTagDataType(this TagDataType tagDataType) {
            switch (tagDataType) {
                case TagDataType.State:
                    return Adapter.RealTimeData.Models.TagDataType.State;
                case TagDataType.Text:
                    return Adapter.RealTimeData.Models.TagDataType.Text;
                case TagDataType.Numeric:
                default:
                    return Adapter.RealTimeData.Models.TagDataType.Numeric;
            }
        }


        internal static Adapter.RealTimeData.Models.TagValue ToAdapterTagValue(this TagValue tagValue) {
            if (tagValue == null) {
                return null;
            }

            return new Adapter.RealTimeData.Models.TagValue(
                tagValue.UtcSampleTime.ToDateTime(),
                tagValue.NumericValue,
                tagValue.TextValue,
                tagValue.Status.ToAdapterTagValueStatus(),
                tagValue.Units,
                tagValue.Notes,
                tagValue.Error,
                tagValue.Properties
            );
        }


        internal static Adapter.RealTimeData.Models.TagValueStatus ToAdapterTagValueStatus(this TagValueStatus status) {
            switch (status) {
                case TagValueStatus.Good:
                    return Adapter.RealTimeData.Models.TagValueStatus.Good;
                case TagValueStatus.Bad:
                    return Adapter.RealTimeData.Models.TagValueStatus.Bad;
                case TagValueStatus.Unknown:
                default:
                    return Adapter.RealTimeData.Models.TagValueStatus.Unknown;
            }
        }


        internal static TagValueStatus ToGrpcTagValueStatus(this Adapter.RealTimeData.Models.TagValueStatus status) {
            switch (status) {
                case Adapter.RealTimeData.Models.TagValueStatus.Bad:
                    return TagValueStatus.Bad;
                case Adapter.RealTimeData.Models.TagValueStatus.Good:
                    return TagValueStatus.Good;
                case Adapter.RealTimeData.Models.TagValueStatus.Unknown:
                default:
                    return TagValueStatus.Unknown;
            }
        }


        internal static Adapter.RealTimeData.Models.TagValueQueryResult ToAdapterTagValueQueryResult(this TagValueQueryResult result) {
            if (result == null) {
                return null;
            }

            return new Adapter.RealTimeData.Models.TagValueQueryResult(
                result.TagId,
                result.TagName,
                result.Value.ToAdapterTagValue()
            );
        }


        internal static Adapter.RealTimeData.Models.ProcessedTagValueQueryResult ToAdapterTagValueQueryResult(this ProcessedTagValueQueryResult result) {
            if (result == null) {
                return null;
            }

            return new Adapter.RealTimeData.Models.ProcessedTagValueQueryResult(
                result.TagId,
                result.TagName,
                result.Value.ToAdapterTagValue(),
                result.DataFunction
            );
        }


        internal static Adapter.RealTimeData.Models.DataFunctionDescriptor ToAdapterDataFunctionDescriptor(this DataFunctionDescriptor func) {
            if (func == null) {
                return null;
            }

            return new Adapter.RealTimeData.Models.DataFunctionDescriptor(
                func.Name,
                func.Description
            );
        }


        internal static RawDataBoundaryType ToGrpcRawDataBoundaryType(this Adapter.RealTimeData.Models.RawDataBoundaryType boundaryType) {
            switch (boundaryType) {
                case Adapter.RealTimeData.Models.RawDataBoundaryType.Outside:
                    return RawDataBoundaryType.Outside;
                case Adapter.RealTimeData.Models.RawDataBoundaryType.Inside:
                default:
                    return RawDataBoundaryType.Inside;
            }
        }


        internal static WriteTagValueRequest ToGrpcWriteTagValueRequest(this Adapter.RealTimeData.Models.WriteTagValueItem item, string adapterId) {
            return new WriteTagValueRequest() {
                AdapterId = adapterId,
                CorrelationId = item.CorrelationId,
                TagId = item.TagId,
                UtcSampleTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(item.Value.UtcSampleTime),
                NumericValue = item.Value.NumericValue,
                TextValue = item.Value.TextValue,
                Status = item.Value.Status.ToGrpcTagValueStatus(),
                Units = item.Value.Units ?? string.Empty
            };
        }


        internal static Adapter.RealTimeData.Models.WriteTagValueResult ToAdapterWriteTagValueResult(this WriteTagValueResult result) {
            return new Adapter.RealTimeData.Models.WriteTagValueResult(
                result.CorrelationId,
                result.TagId,
                result.WriteStatus.ToAdapterWriteStatus(),
                result.Notes,
                result.Properties
            );
        }


        internal static Adapter.RealTimeData.Models.TagValueAnnotation ToAdapterTagValueAnnotation(this TagValueAnnotation annotation) {
            if (annotation == null) {
                return null;
            }

            return new Adapter.RealTimeData.Models.TagValueAnnotation(
                annotation.Id,
                annotation.Annotation.AnnotationType.ToAdapterAnnotationType(),
                annotation.Annotation.UtcStartTime.ToDateTime(),
                annotation.Annotation.UtcEndTime?.ToDateTime(),
                annotation.Annotation.Value,
                annotation.Annotation.Description,
                annotation.Annotation.Properties
            );
        }


        internal static Adapter.RealTimeData.Models.AnnotationType ToAdapterAnnotationType(this AnnotationType annotationType) {
            switch (annotationType) {
                case AnnotationType.TimeRange:
                    return Adapter.RealTimeData.Models.AnnotationType.TimeRange;
                case AnnotationType.Instantaneous:
                default:
                    return Adapter.RealTimeData.Models.AnnotationType.Instantaneous;
            }
        }


        internal static Adapter.RealTimeData.Models.TagValueAnnotationQueryResult ToAdapterAnnotationQueryResult(this TagValueAnnotationQueryResult result) {
            if (result == null) {
                return null;
            }

            return new Adapter.RealTimeData.Models.TagValueAnnotationQueryResult(
                result.TagId,
                result.TagName,
                result.Annotation.ToAdapterTagValueAnnotation()
            );
        }


        internal static AnnotationType ToGrpcAnnotationType(this Adapter.RealTimeData.Models.AnnotationType annotationType) {
            switch (annotationType) {
                case Adapter.RealTimeData.Models.AnnotationType.TimeRange:
                    return AnnotationType.TimeRange;
                case Adapter.RealTimeData.Models.AnnotationType.Instantaneous:
                default:
                    return AnnotationType.Instantaneous;
            }
        }


        internal static TagValueAnnotationBase ToGrpcTagValueAnnotationBase(this Adapter.RealTimeData.Models.TagValueAnnotationBase annotation) {
            if (annotation == null) {
                return null;
            }

            var result = new TagValueAnnotationBase() {
                AnnotationType = annotation.AnnotationType.ToGrpcAnnotationType(),
                UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annotation.UtcStartTime),
                HasUtcEndTime = annotation.UtcEndTime.HasValue,
                Value = annotation.Value,
                Description = annotation.Description
            };

            if (result.HasUtcEndTime) {
                result.UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annotation.UtcEndTime.Value);
            }

            if (annotation.Properties.Count > 0) {
                foreach (var item in annotation.Properties) {
                    result.Properties.Add(item.Key, item.Value);
                }
            }

            return result;
        }


        internal static Adapter.RealTimeData.Models.WriteTagValueAnnotationResult ToAdapterWriteTagValueAnnotationResult(this WriteTagValueAnnotationResult result) {
            return new Adapter.RealTimeData.Models.WriteTagValueAnnotationResult(
                result.TagId,
                result.AnnotationId,
                result.WriteStatus.ToAdapterWriteStatus(),
                result.Notes,
                result.Properties
            );
        }

    }
}
