using System;
using System.Linq;
using DataCore.Adapter.Common;
using DataCore.Adapter.Json;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Server {

    /// <summary>
    /// Extension methods for converting from common adapter types to their gRPC-equivalents.
    /// </summary>
    internal static class GrpcExtensions {

        public static Common.VariantType ToAdapterVariantType(this Grpc.VariantType type) {
            switch (type) {
                case Grpc.VariantType.Boolean:
                    return Common.VariantType.Boolean;
                case Grpc.VariantType.Byte:
                    return Common.VariantType.Byte;
                case Grpc.VariantType.Datetime:
                    return Common.VariantType.DateTime;
                case Grpc.VariantType.Double:
                    return Common.VariantType.Double;
                case Grpc.VariantType.Float:
                    return Common.VariantType.Float;
                case Grpc.VariantType.Int16:
                    return Common.VariantType.Int16;
                case Grpc.VariantType.Int32:
                    return Common.VariantType.Int32;
                case Grpc.VariantType.Int64:
                    return Common.VariantType.Int64;
                case Grpc.VariantType.Null:
                    return Common.VariantType.Null;
                case Grpc.VariantType.Object:
                    return Common.VariantType.Object;
                case Grpc.VariantType.Sbyte:
                    return Common.VariantType.SByte;
                case Grpc.VariantType.String:
                    return Common.VariantType.String;
                case Grpc.VariantType.Timespan:
                    return Common.VariantType.TimeSpan;
                case Grpc.VariantType.Uint16:
                    return Common.VariantType.UInt16;
                case Grpc.VariantType.Uint32:
                    return Common.VariantType.UInt32;
                case Grpc.VariantType.Uint64:
                    return Common.VariantType.UInt64;
                case Grpc.VariantType.Unknown:
                default:
                    return Common.VariantType.Unknown;
            }
        }


        public static Common.Variant ToAdapterVariant(this Grpc.Variant variant) {
            if (variant == null) {
                return Common.Variant.Null;
            }

            var bytes = variant.Value.ToByteArray();
            object value;

            switch (variant.Type) {
                case Grpc.VariantType.Boolean:
                    value = BitConverter.ToBoolean(bytes, 0);
                    break;
                case Grpc.VariantType.Byte:
                    value = bytes?.FirstOrDefault();
                    break;
                case Grpc.VariantType.Datetime:
                    value = DateTime.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out var dt)
                        ? dt
                        : default;
                    break;
                case Grpc.VariantType.Double:
                    value = BitConverter.ToDouble(bytes, 0);
                    break;
                case Grpc.VariantType.Float:
                    value = BitConverter.ToSingle(bytes, 0);
                    break;
                case Grpc.VariantType.Int16:
                    value = BitConverter.ToInt16(bytes, 0);
                    break;
                case Grpc.VariantType.Int32:
                    value = BitConverter.ToInt32(bytes, 0);
                    break;
                case Grpc.VariantType.Int64:
                    value = BitConverter.ToInt64(bytes, 0);
                    break;
                case Grpc.VariantType.Null:
                    value = null;
                    break;
                case Grpc.VariantType.Object:
                    var serializerOptions = new System.Text.Json.JsonSerializerOptions();
                    serializerOptions.Converters.AddAdapterConverters();
                    value = System.Text.Json.JsonSerializer.Deserialize(System.Text.Encoding.UTF8.GetString(bytes), typeof(object), serializerOptions);
                    break;
                case Grpc.VariantType.Sbyte:
                    value = (sbyte) bytes?.FirstOrDefault();
                    break;
                case Grpc.VariantType.String:
                    value = System.Text.Encoding.UTF8.GetString(bytes);
                    break;
                case Grpc.VariantType.Timespan:
                    value = TimeSpan.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out var ts)
                        ? ts
                        : default;
                    break;
                case Grpc.VariantType.Uint16:
                    value = BitConverter.ToUInt16(bytes, 0);
                    break;
                case Grpc.VariantType.Uint32:
                    value = BitConverter.ToUInt32(bytes, 0);
                    break;
                case Grpc.VariantType.Uint64:
                    value = BitConverter.ToUInt64(bytes, 0);
                    break;
                case Grpc.VariantType.Unknown:
                default:
                    value = null;
                    break;
            }

            return Common.Variant.FromValue(
                value,
                variant.Type.ToAdapterVariantType()
            );
        }


        public static Grpc.VariantType ToGrpcVariantType(this Common.VariantType type) {
            switch (type) {
                case Common.VariantType.Boolean:
                    return Grpc.VariantType.Boolean;
                case Common.VariantType.Byte:
                    return Grpc.VariantType.Byte;
                case Common.VariantType.DateTime:
                    return Grpc.VariantType.Datetime;
                case Common.VariantType.Double:
                    return Grpc.VariantType.Double;
                case Common.VariantType.Float:
                    return Grpc.VariantType.Float;
                case Common.VariantType.Int16:
                    return Grpc.VariantType.Int16;
                case Common.VariantType.Int32:
                    return Grpc.VariantType.Int32;
                case Common.VariantType.Int64:
                    return Grpc.VariantType.Int64;
                case Common.VariantType.Null:
                    return Grpc.VariantType.Null;
                case Common.VariantType.Object:
                    return Grpc.VariantType.Object;
                case Common.VariantType.SByte:
                    return Grpc.VariantType.Sbyte;
                case Common.VariantType.String:
                    return Grpc.VariantType.String;
                case Common.VariantType.TimeSpan:
                    return Grpc.VariantType.Timespan;
                case Common.VariantType.UInt16:
                    return Grpc.VariantType.Uint16;
                case Common.VariantType.UInt32:
                    return Grpc.VariantType.Uint32;
                case Common.VariantType.UInt64:
                    return Grpc.VariantType.Uint64;
                case Common.VariantType.Unknown:
                default:
                    return Grpc.VariantType.Unknown;
            }
        }


        public static Grpc.Variant ToGrpcVariant(this Common.Variant variant) {
            if (variant.Value == null) {
                return new Grpc.Variant() {
                    Value = Google.Protobuf.ByteString.Empty,
                    Type = Grpc.VariantType.Null
                };
            }

            byte[] bytes;

            switch (variant.Type) {
                case Common.VariantType.Boolean:
                    bytes = BitConverter.GetBytes(variant.GetValueOrDefault<bool>());
                    break;
                case Common.VariantType.Byte:
                    bytes = new[] { variant.GetValueOrDefault<byte>() };
                    break;
                case Common.VariantType.DateTime:
                    bytes = System.Text.Encoding.UTF8.GetBytes(variant.GetValueOrDefault<DateTime>().ToUniversalTime().ToString("yyyy-mm-ddTHH:mm:ss.fffffffZ"));
                    break;
                case Common.VariantType.Double:
                    bytes = BitConverter.GetBytes(variant.GetValueOrDefault<double>());
                    break;
                case Common.VariantType.Float:
                    bytes = BitConverter.GetBytes(variant.GetValueOrDefault<float>());
                    break;
                case Common.VariantType.Int16:
                    bytes = BitConverter.GetBytes(variant.GetValueOrDefault<short>());
                    break;
                case Common.VariantType.Int32:
                    bytes = BitConverter.GetBytes(variant.GetValueOrDefault<int>());
                    break;
                case Common.VariantType.Int64:
                    bytes = BitConverter.GetBytes(variant.GetValueOrDefault<long>());
                    break;
                case Common.VariantType.Null:
                    bytes = Array.Empty<byte>();
                    break;
                case Common.VariantType.Object:
                    var serializerOptions = new System.Text.Json.JsonSerializerOptions();
                    serializerOptions.Converters.AddAdapterConverters();
                    bytes = System.Text.Encoding.UTF8.GetBytes(
                        System.Text.Json.JsonSerializer.Serialize(variant.Value, variant.Value?.GetType() ?? typeof(object), serializerOptions)
                    );
                    break;
                case Common.VariantType.SByte:
                    bytes = new[] { (byte) variant.GetValueOrDefault<sbyte>() };
                    break;
                case Common.VariantType.String:
                    bytes = System.Text.Encoding.UTF8.GetBytes(variant.GetValueOrDefault(string.Empty));
                    break;
                case Common.VariantType.TimeSpan:
                    bytes = System.Text.Encoding.UTF8.GetBytes(variant.GetValueOrDefault<TimeSpan>().ToString());
                    break;
                case Common.VariantType.UInt16:
                    bytes = BitConverter.GetBytes(variant.GetValueOrDefault<ushort>());
                    break;
                case Common.VariantType.UInt32:
                    bytes = BitConverter.GetBytes(variant.GetValueOrDefault<uint>());
                    break;
                case Common.VariantType.UInt64:
                    bytes = BitConverter.GetBytes(variant.GetValueOrDefault<ulong>());
                    break;
                case Common.VariantType.Unknown:
                default:
                    bytes = Array.Empty<byte>();
                    break;
            }

            return new Grpc.Variant() {
                Value = bytes.Length > 0
                    ? Google.Protobuf.ByteString.CopyFrom(bytes)
                    : Google.Protobuf.ByteString.Empty,
                Type = variant.Type.ToGrpcVariantType()
            };
        }


        internal static Common.AdapterProperty ToAdapterProperty(this Grpc.AdapterProperty property) {
            if (property == null) {
                return null;
            }

            return Common.AdapterProperty.Create(
                property.Name,
                property.Value.ToAdapterVariant()
            );
        }


        internal static Grpc.AdapterProperty ToGrpcProperty(this Common.AdapterProperty property) {
            if (property == null) {
                return null;
            }

            var result = new Grpc.AdapterProperty() {
                Name = property.Name ?? string.Empty,
                Value = property.Value.ToGrpcVariant()
            };

            return result;
        }


        /// <summary>
        /// Converts from an adapter tag definition into its gRPC equivalent.
        /// </summary>
        /// <param name="tag">
        ///   The adapter tag definition.
        /// </param>
        /// <returns>
        ///   The gRPC tag definition.
        /// </returns>
        internal static TagDefinition ToGrpcTagDefinition(this RealTimeData.TagDefinition tag) {
            var result = new TagDefinition() {
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
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcProperty());
                }
            }

            if (tag.States != null) {
                foreach (var item in tag.States) {
                    if (item == null) {
                        continue;
                    }
                    result.States.Add(item.ToGrpcDigitalState());
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
        internal static TagDataType ToGrpcTagDataType(this RealTimeData.TagDataType tagDataType) {
            switch (tagDataType) {
                case RealTimeData.TagDataType.Numeric:
                    return TagDataType.Numeric;
                case RealTimeData.TagDataType.State:
                    return TagDataType.State;
                case RealTimeData.TagDataType.Text:
                    return TagDataType.Text;
                default:
                    return TagDataType.Numeric;
            }
        }


        /// <summary>
        /// Converts from an adapter digital state to its gRPC equivalent.
        /// </summary>
        /// <param name="state">
        ///   The adapter digital state.
        /// </param>
        /// <returns>
        ///   The gRPC digital state.
        /// </returns>
        internal static DigitalState ToGrpcDigitalState(this RealTimeData.DigitalState state) {
            if (state == null) {
                return null;
            }

            return new DigitalState() { 
                Name = state.Name ?? string.Empty,
                Value = state.Value
            };
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
        internal static AssetModelNode ToGrpcAssetModelNode(this AssetModel.AssetModelNode node) {
            var result = new AssetModelNode() {
                Id = node.Id ?? string.Empty,
                Name = node.Name ?? string.Empty,
                Description = node.Description ?? string.Empty,
                Parent = node.Parent ?? string.Empty
            };

            if (node.Children != null && node.Children.Any()) {
                result.Children.AddRange(node.Children);
            }
            if (node.Measurements != null && node.Measurements.Any()) {
                foreach (var item in node.Measurements) {
                    result.Measurements.Add(new AssetModelNodeMeasurement() {
                        Name = item.Name ?? string.Empty,
                        AdapterId = item.AdapterId ?? string.Empty,
                        Tag = new TagSummary() {
                            Id = item.Tag?.Id ?? string.Empty,
                            Name = item.Tag?.Name ?? string.Empty,
                            Description = item.Tag?.Description ?? string.Empty,
                            Units = item.Tag?.Units ?? string.Empty
                        }
                    });
                }
            }
            if (node.Properties != null) {
                foreach (var item in node.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcProperty());
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
        internal static TagValueQueryResult ToGrpcTagValue(this RealTimeData.TagValue value, string tagId, string tagName, TagValueQueryType queryType) {
            var result = new TagValueQueryResult() {
                TagId = tagId ?? string.Empty,
                TagName = tagName ?? string.Empty,
                QueryType = queryType,
                Value = new TagValue() {
                    Error = value.Error ?? string.Empty,
                    Notes = value.Notes ?? string.Empty,
                    Status = value.Status.ToGrpcTagValueStatus(),
                    Units = value.Units ?? string.Empty,
                    UtcSampleTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(value.UtcSampleTime),
                    Value = value.Value.ToGrpcVariant()
                }
            };

            if (value.Properties != null) {
                foreach (var item in value.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Value.Properties.Add(item.ToGrpcProperty());
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
        internal static ProcessedTagValueQueryResult ToGrpcProcessedTagValue(this RealTimeData.TagValue value, string tagId, string tagName, string dataFunction, TagValueQueryType queryType) {
            var result = new ProcessedTagValueQueryResult() {
                TagId = tagId ?? string.Empty,
                TagName = tagName ?? string.Empty,
                DataFunction = dataFunction ?? string.Empty,
                QueryType = queryType,
                Value = new TagValue() {
                    Error = value.Error ?? string.Empty,
                    Notes = value.Notes ?? string.Empty,
                    Status = value.Status.ToGrpcTagValueStatus(),
                    Units = value.Units ?? string.Empty,
                    UtcSampleTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(value.UtcSampleTime),
                    Value = value.Value.ToGrpcVariant()
                }
            };

            if (value.Properties != null) {
                foreach (var item in value.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Value.Properties.Add(item.ToGrpcProperty());
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
        internal static TagValueStatus ToGrpcTagValueStatus(this RealTimeData.TagValueStatus status) {
            switch (status) {
                case RealTimeData.TagValueStatus.Bad:
                    return TagValueStatus.Bad;
                case RealTimeData.TagValueStatus.Good:
                    return TagValueStatus.Good;
                case RealTimeData.TagValueStatus.Unknown:
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
        internal static RealTimeData.TagValueStatus ToAdapterTagValueStatus(this TagValueStatus status) {
            switch (status) {
                case TagValueStatus.Bad:
                    return RealTimeData.TagValueStatus.Bad;
                case TagValueStatus.Good:
                    return RealTimeData.TagValueStatus.Good;
                case TagValueStatus.Unknown:
                default:
                    return RealTimeData.TagValueStatus.Unknown;
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
        internal static RealTimeData.RawDataBoundaryType FromGrpcRawDataBoundaryType(this RawDataBoundaryType boundaryType) {
            switch (boundaryType) {
                case RawDataBoundaryType.Outside:
                    return RealTimeData.RawDataBoundaryType.Outside;
                case RawDataBoundaryType.Inside:
                default:
                    return RealTimeData.RawDataBoundaryType.Inside;
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
        internal static DataFunctionDescriptor ToGrpcDataFunctionDescriptor(this RealTimeData.DataFunctionDescriptor descriptor) {
            return new DataFunctionDescriptor() {
                Id = descriptor.Id ?? string.Empty,
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
                Value = TagValueBase.Create(
                    writeRequest.UtcSampleTime.ToDateTime(),
                    writeRequest.Value.ToAdapterVariant(),
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
        internal static WriteTagValueResult ToGrpcWriteTagValueResult(this RealTimeData.WriteTagValueResult adapterResult, string adapterId) {
            var result = new WriteTagValueResult() {
                AdapterId = adapterId ?? string.Empty,
                CorrelationId = adapterResult.CorrelationId ?? string.Empty,
                Notes = adapterResult.Notes ?? string.Empty,
                TagId = adapterResult.TagId ?? string.Empty,
                WriteStatus = adapterResult.Status.ToGrpcWriteOperationStatus()
            };

            if (adapterResult.Properties != null) {
                foreach (var item in adapterResult.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcProperty());
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
        internal static WriteOperationStatus ToGrpcWriteOperationStatus(this Common.WriteStatus status) {
            switch (status) {
                case Common.WriteStatus.Fail:
                    return WriteOperationStatus.Fail;
                case Common.WriteStatus.Pending:
                    return WriteOperationStatus.Pending;
                case Common.WriteStatus.Success:
                    return WriteOperationStatus.Success;
                case Common.WriteStatus.Unknown:
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
        internal static TagValueAnnotationQueryResult ToGrpcTagValueAnnotationQueryResult(this RealTimeData.TagValueAnnotationQueryResult annotation) {
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
        internal static TagValueAnnotation ToGrpcTagValueAnnotation(this RealTimeData.TagValueAnnotation annotation) {
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
                    if (item == null) {
                        continue;
                    }
                    result.Annotation.Properties.Add(item.ToGrpcProperty());
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
        internal static RealTimeData.TagValueAnnotationBase ToAdapterTagValueAnnotation(this TagValueAnnotationBase annotation) {
            if (annotation == null) {
                return null;
            }

            return RealTimeData.TagValueAnnotationBase.Create(
                annotation.AnnotationType.ToAdapterAnnotationType(),
                annotation.UtcStartTime.ToDateTime(),
                annotation.HasUtcEndTime
                    ? annotation.UtcEndTime.ToDateTime()
                    : (DateTime?) null,
                annotation.Value,
                annotation.Description,
                annotation.Properties.Select(x => x.ToAdapterProperty()).ToArray()
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
        internal static RealTimeData.AnnotationType ToAdapterAnnotationType(this AnnotationType annotationType) {
            switch (annotationType) {
                case AnnotationType.TimeRange:
                    return RealTimeData.AnnotationType.TimeRange;
                case AnnotationType.Instantaneous:
                default:
                    return RealTimeData.AnnotationType.Instantaneous;
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
        internal static WriteTagValueAnnotationResult ToGrpcWriteTagValueAnnotationResult(this RealTimeData.WriteTagValueAnnotationResult adapterResult, string adapterId) {
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

            if (adapterResult.Properties != null) {
                foreach (var item in adapterResult.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcProperty());
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
        internal static EventMessage ToGrpcEventMessage(this Events.EventMessageBase message) {
            var result = new EventMessage() {
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
        internal static EventPriority ToGrpcEventPriority(this Events.EventPriority priority) {
            switch (priority) {
                case Events.EventPriority.Low:
                    return EventPriority.Low;
                case Events.EventPriority.Medium:
                    return EventPriority.Medium;
                case Events.EventPriority.High:
                    return EventPriority.High;
                case Events.EventPriority.Critical:
                    return EventPriority.Critical;
                case Events.EventPriority.Unknown:
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
        internal static Events.EventPriority ToAdapterEventPriority(this EventPriority priority) {
            switch (priority) {
                case EventPriority.Low:
                    return Events.EventPriority.Low;
                case EventPriority.Medium:
                    return Events.EventPriority.Medium;
                case EventPriority.High:
                    return Events.EventPriority.High;
                case EventPriority.Critical:
                    return Events.EventPriority.Critical;
                case EventPriority.Unknown:
                default:
                    return Events.EventPriority.Unknown;
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
        internal static Events.WriteEventMessageItem ToAdapterWriteEventMessageItem(this Grpc.WriteEventMessageRequest writeRequest) {
            return new Events.WriteEventMessageItem() {
                CorrelationId = writeRequest.CorrelationId,
                EventMessage = Events.EventMessage.Create(
                    writeRequest.Message?.Id,
                    writeRequest.Message?.UtcEventTime?.ToDateTime() ?? DateTime.MinValue,
                    writeRequest.Message?.Priority.ToAdapterEventPriority() ?? Events.EventPriority.Unknown,
                    writeRequest.Message?.Category,
                    writeRequest.Message?.Message,
                    writeRequest.Message?.Properties.Select(x => x.ToAdapterProperty())
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
        internal static WriteEventMessageResult ToGrpcWriteEventMessageResult(this Events.WriteEventMessageResult adapterResult, string adapterId) {
            var result = new WriteEventMessageResult() {
                AdapterId = adapterId ?? string.Empty,
                CorrelationId = adapterResult.CorrelationId ?? string.Empty,
                Notes = adapterResult.Notes ?? string.Empty,
                WriteStatus = adapterResult.Status.ToGrpcWriteOperationStatus()
            };

            if (adapterResult.Properties != null) {
                foreach (var item in adapterResult.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcProperty());
                }
            }

            return result;
        }

    }
}
