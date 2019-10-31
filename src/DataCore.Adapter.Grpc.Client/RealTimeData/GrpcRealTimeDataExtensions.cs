
using System.Linq;
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
                tagDefinition.States.Select(x => x.ToAdapterDigitalState()).ToArray(),
                tagDefinition.Properties.Select(x => x.ToAdapterProperty()).ToArray(),
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
                    return TagDataType.Numeric;
                default:
                    return TagDataType.Unknown;
            }
        }


        public static DigitalState ToAdapterDigitalState(this Grpc.DigitalState state) {
            if (state  == null) {
                return null;
            }

            return DigitalState.Create(state.Name, state.Value);
        }


        public static TagValueExtended ToAdapterTagValue(this Grpc.TagValue tagValue) {
            if (tagValue == null) {
                return null;
            }

            return TagValueExtended.Create(
                tagValue.UtcSampleTime.ToDateTime(),
                tagValue.Value.ToAdapterVariant(),
                tagValue.Status.ToAdapterTagValueStatus(),
                tagValue.Units,
                tagValue.Notes,
                tagValue.Error,
                tagValue.Properties.Select(x => x.ToAdapterProperty()).ToArray()
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
                func.Id,
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
                Value = item.Value.Value.ToGrpcVariant(),
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
                result.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


        public static TagValueAnnotationExtended ToAdapterTagValueAnnotation(this Grpc.TagValueAnnotation annotation) {
            if (annotation == null) {
                return null;
            }

            return TagValueAnnotationExtended.Create(
                annotation.Id,
                annotation.Annotation.AnnotationType.ToAdapterAnnotationType(),
                annotation.Annotation.UtcStartTime.ToDateTime(),
                annotation.Annotation.UtcEndTime?.ToDateTime(),
                annotation.Annotation.Value,
                annotation.Annotation.Description,
                annotation.Annotation.Properties.Select(x => x.ToAdapterProperty()).ToArray()
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


        public static Grpc.TagValueAnnotationBase ToGrpcTagValueAnnotationBase(this TagValueAnnotation annotation) {
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

            if (annotation.Properties != null) {
                foreach (var item in annotation.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcProperty());
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
                result.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }

    }
}
