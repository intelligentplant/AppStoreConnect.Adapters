
using DataCore.Adapter.Common;
using DataCore.Adapter.Grpc;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Extension methods for converting from gRPC types to their adapter equivalents, and vice versa.
    /// </summary>
    public static class GrpcRealTimeDataExtensions {

        public static Models.TagDefinition ToAdapterTagDefinition(this TagDefinition tagDefinition) {
            if (tagDefinition == null) {
                return null;
            }

            return Models.TagDefinition.Create(
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


        public static Models.TagDataType ToAdapterTagDataType(this TagDataType tagDataType) {
            switch (tagDataType) {
                case TagDataType.State:
                    return Models.TagDataType.State;
                case TagDataType.Text:
                    return Models.TagDataType.Text;
                case TagDataType.Numeric:
                default:
                    return Models.TagDataType.Numeric;
            }
        }


        public static Models.TagValue ToAdapterTagValue(this TagValue tagValue) {
            if (tagValue == null) {
                return null;
            }

            return Models.TagValue.Create(
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


        public static Models.TagValueStatus ToAdapterTagValueStatus(this TagValueStatus status) {
            switch (status) {
                case TagValueStatus.Good:
                    return Models.TagValueStatus.Good;
                case TagValueStatus.Bad:
                    return Models.TagValueStatus.Bad;
                case TagValueStatus.Unknown:
                default:
                    return Models.TagValueStatus.Unknown;
            }
        }


        public static TagValueStatus ToGrpcTagValueStatus(this Models.TagValueStatus status) {
            switch (status) {
                case Models.TagValueStatus.Bad:
                    return TagValueStatus.Bad;
                case Models.TagValueStatus.Good:
                    return TagValueStatus.Good;
                case Models.TagValueStatus.Unknown:
                default:
                    return TagValueStatus.Unknown;
            }
        }


        public static Models.TagValueQueryResult ToAdapterTagValueQueryResult(this TagValueQueryResult result) {
            if (result == null) {
                return null;
            }

            return Models.TagValueQueryResult.Create(
                result.TagId,
                result.TagName,
                result.Value.ToAdapterTagValue()
            );
        }


        public static Models.ProcessedTagValueQueryResult ToAdapterTagValueQueryResult(this ProcessedTagValueQueryResult result) {
            if (result == null) {
                return null;
            }

            return Models.ProcessedTagValueQueryResult.Create(
                result.TagId,
                result.TagName,
                result.Value.ToAdapterTagValue(),
                result.DataFunction
            );
        }


        public static Models.DataFunctionDescriptor ToAdapterDataFunctionDescriptor(this DataFunctionDescriptor func) {
            if (func == null) {
                return null;
            }

            return Models.DataFunctionDescriptor.Create(
                func.Name,
                func.Description
            );
        }


        public static RawDataBoundaryType ToGrpcRawDataBoundaryType(this Models.RawDataBoundaryType boundaryType) {
            switch (boundaryType) {
                case Models.RawDataBoundaryType.Outside:
                    return RawDataBoundaryType.Outside;
                case Models.RawDataBoundaryType.Inside:
                default:
                    return RawDataBoundaryType.Inside;
            }
        }


        public static WriteTagValueRequest ToGrpcWriteTagValueRequest(this Models.WriteTagValueItem item, string adapterId) {
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


        public static Models.WriteTagValueResult ToAdapterWriteTagValueResult(this WriteTagValueResult result) {
            if (result == null) {
                return null;
            }
            
            return Models.WriteTagValueResult.Create(
                result.CorrelationId,
                result.TagId,
                result.WriteStatus.ToAdapterWriteStatus(),
                result.Notes,
                result.Properties
            );
        }


        public static Models.TagValueAnnotation ToAdapterTagValueAnnotation(this TagValueAnnotation annotation) {
            if (annotation == null) {
                return null;
            }

            return Models.TagValueAnnotation.Create(
                annotation.Id,
                annotation.Annotation.AnnotationType.ToAdapterAnnotationType(),
                annotation.Annotation.UtcStartTime.ToDateTime(),
                annotation.Annotation.UtcEndTime?.ToDateTime(),
                annotation.Annotation.Value,
                annotation.Annotation.Description,
                annotation.Annotation.Properties
            );
        }


        public static Models.AnnotationType ToAdapterAnnotationType(this AnnotationType annotationType) {
            switch (annotationType) {
                case AnnotationType.TimeRange:
                    return Models.AnnotationType.TimeRange;
                case AnnotationType.Instantaneous:
                default:
                    return Models.AnnotationType.Instantaneous;
            }
        }


        public static Models.TagValueAnnotationQueryResult ToAdapterAnnotationQueryResult(this TagValueAnnotationQueryResult result) {
            if (result == null) {
                return null;
            }

            return Models.TagValueAnnotationQueryResult.Create(
                result.TagId,
                result.TagName,
                result.Annotation.ToAdapterTagValueAnnotation()
            );
        }


        public static AnnotationType ToGrpcAnnotationType(this Models.AnnotationType annotationType) {
            switch (annotationType) {
                case Models.AnnotationType.TimeRange:
                    return AnnotationType.TimeRange;
                case Models.AnnotationType.Instantaneous:
                default:
                    return AnnotationType.Instantaneous;
            }
        }


        public static TagValueAnnotationBase ToGrpcTagValueAnnotationBase(this Models.TagValueAnnotationBase annotation) {
            if (annotation == null) {
                return null;
            }

            var result = new TagValueAnnotationBase() {
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


        public static Models.WriteTagValueAnnotationResult ToAdapterWriteTagValueAnnotationResult(this WriteTagValueAnnotationResult result) {
            if (result == null) {
                return null;
            }
            
            return Models.WriteTagValueAnnotationResult.Create(
                result.TagId,
                result.AnnotationId,
                result.WriteStatus.ToAdapterWriteStatus(),
                result.Notes,
                result.Properties
            );
        }

    }
}
