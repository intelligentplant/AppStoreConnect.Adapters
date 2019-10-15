
using DataCore.Adapter.Common;
using DataCore.Adapter.Grpc;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Extension methods for converting from gRPC types to their adapter equivalents, and vice versa.
    /// </summary>
    public static class GrpcRealTimeDataExtensions {

        public static TagDefinition ToAdapterTagDefinition(this Grpc.TagDefinition tagDefinition) {
            if (tagDefinition == null) {
                return null;
            }

            return TagDefinition.Create(
                tagDefinition.Id,
                tagDefinition.Name,
                tagDefinition.Description,
                tagDefinition.Units,
                tagDefinition.DataType.ToAdapterTagDataType(),
                tagDefinition.States,
                tagDefinition.Properties,
                tagDefinition.Labels
            );
        }


        public static TagDataType ToAdapterTagDataType(this Grpc.TagDataType tagDataType) {
            switch (tagDataType) {
                case Grpc.TagDataType.State:
                    return TagDataType.State;
                case Grpc.TagDataType.Text:
                    return TagDataType.Text;
                case Grpc.TagDataType.Numeric:
                default:
                    return TagDataType.Numeric;
            }
        }


        public static TagValue ToAdapterTagValue(this Grpc.TagValue tagValue) {
            if (tagValue == null) {
                return null;
            }

            return TagValue.Create(
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


        public static TagValueStatus ToAdapterTagValueStatus(this Grpc.TagValueStatus status) {
            switch (status) {
                case Grpc.TagValueStatus.Good:
                    return TagValueStatus.Good;
                case Grpc.TagValueStatus.Bad:
                    return TagValueStatus.Bad;
                case Grpc.TagValueStatus.Unknown:
                default:
                    return TagValueStatus.Unknown;
            }
        }


        public static Grpc.TagValueStatus ToGrpcTagValueStatus(this TagValueStatus status) {
            switch (status) {
                case TagValueStatus.Bad:
                    return Grpc.TagValueStatus.Bad;
                case TagValueStatus.Good:
                    return Grpc.TagValueStatus.Good;
                case TagValueStatus.Unknown:
                default:
                    return Grpc.TagValueStatus.Unknown;
            }
        }


        public static TagValueQueryResult ToAdapterTagValueQueryResult(this Grpc.TagValueQueryResult result) {
            if (result == null) {
                return null;
            }

            return TagValueQueryResult.Create(
                result.TagId,
                result.TagName,
                result.Value.ToAdapterTagValue()
            );
        }


        public static ProcessedTagValueQueryResult ToAdapterTagValueQueryResult(this Grpc.ProcessedTagValueQueryResult result) {
            if (result == null) {
                return null;
            }

            return ProcessedTagValueQueryResult.Create(
                result.TagId,
                result.TagName,
                result.Value.ToAdapterTagValue(),
                result.DataFunction
            );
        }


        public static DataFunctionDescriptor ToAdapterDataFunctionDescriptor(this Grpc.DataFunctionDescriptor func) {
            if (func == null) {
                return null;
            }

            return DataFunctionDescriptor.Create(
                func.Name,
                func.Description
            );
        }


        public static Grpc.RawDataBoundaryType ToGrpcRawDataBoundaryType(this RawDataBoundaryType boundaryType) {
            switch (boundaryType) {
                case RawDataBoundaryType.Outside:
                    return Grpc.RawDataBoundaryType.Outside;
                case RawDataBoundaryType.Inside:
                default:
                    return Grpc.RawDataBoundaryType.Inside;
            }
        }


        public static Grpc.WriteTagValueRequest ToGrpcWriteTagValueRequest(this WriteTagValueItem item, string adapterId) {
            if (item == null) {
                return null;
            }
            
            return new WriteTagValueRequest() {
                AdapterId = adapterId,
                CorrelationId = item.CorrelationId ?? string.Empty,
                TagId = item.TagId ?? string.Empty,
                UtcSampleTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(item.Value.UtcSampleTime),
                NumericValue = item.Value.NumericValue,
                TextValue = item.Value.TextValue ?? string.Empty,
                Status = item.Value.Status.ToGrpcTagValueStatus(),
                Units = item.Value.Units ?? string.Empty
            };
        }


        public static WriteTagValueResult ToAdapterWriteTagValueResult(this Grpc.WriteTagValueResult result) {
            if (result == null) {
                return null;
            }
            
            return WriteTagValueResult.Create(
                result.CorrelationId,
                result.TagId,
                result.WriteStatus.ToAdapterWriteStatus(),
                result.Notes,
                result.Properties
            );
        }


        public static TagValueAnnotation ToAdapterTagValueAnnotation(this Grpc.TagValueAnnotation annotation) {
            if (annotation == null) {
                return null;
            }

            return TagValueAnnotation.Create(
                annotation.Id,
                annotation.Annotation.AnnotationType.ToAdapterAnnotationType(),
                annotation.Annotation.UtcStartTime.ToDateTime(),
                annotation.Annotation.UtcEndTime?.ToDateTime(),
                annotation.Annotation.Value,
                annotation.Annotation.Description,
                annotation.Annotation.Properties
            );
        }


        public static AnnotationType ToAdapterAnnotationType(this Grpc.AnnotationType annotationType) {
            switch (annotationType) {
                case Grpc.AnnotationType.TimeRange:
                    return AnnotationType.TimeRange;
                case Grpc.AnnotationType.Instantaneous:
                default:
                    return AnnotationType.Instantaneous;
            }
        }


        public static TagValueAnnotationQueryResult ToAdapterAnnotationQueryResult(this Grpc.TagValueAnnotationQueryResult result) {
            if (result == null) {
                return null;
            }

            return TagValueAnnotationQueryResult.Create(
                result.TagId,
                result.TagName,
                result.Annotation.ToAdapterTagValueAnnotation()
            );
        }


        public static Grpc.AnnotationType ToGrpcAnnotationType(this AnnotationType annotationType) {
            switch (annotationType) {
                case AnnotationType.TimeRange:
                    return Grpc.AnnotationType.TimeRange;
                case AnnotationType.Instantaneous:
                default:
                    return Grpc.AnnotationType.Instantaneous;
            }
        }


        public static Grpc.TagValueAnnotationBase ToGrpcTagValueAnnotationBase(this TagValueAnnotationBase annotation) {
            if (annotation == null) {
                return null;
            }

            var result = new Grpc.TagValueAnnotationBase() {
                AnnotationType = annotation.AnnotationType.ToGrpcAnnotationType(),
                UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annotation.UtcStartTime),
                HasUtcEndTime = annotation.UtcEndTime.HasValue,
                Value = annotation.Value ?? string.Empty,
                Description = annotation.Description ?? string.Empty
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


        public static WriteTagValueAnnotationResult ToAdapterWriteTagValueAnnotationResult(this Grpc.WriteTagValueAnnotationResult result) {
            if (result == null) {
                return null;
            }
            
            return WriteTagValueAnnotationResult.Create(
                result.TagId,
                result.AnnotationId,
                result.WriteStatus.ToAdapterWriteStatus(),
                result.Notes,
                result.Properties
            );
        }

    }
}
