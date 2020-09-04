// ##################################################################################################
// # IMPORTANT!                                                                                     #
// #                                                                                                #
// # This file is shared between DataCore.Adapter.Grpc.Client and DataCore.Adapter.AspNetCore.Grpc. # 
// # Be careful when making changes!                                                                #
// ##################################################################################################

using System;
using System.Globalization;
using System.Linq;
using DataCore.Adapter.Common;
using DataCore.Adapter.Json;

namespace DataCore.Adapter {

    /// <summary>
    /// Extension methods for converting from common adapter types to their gRPC-equivalents and 
    /// vice versa.
    /// </summary>
    public static class GrpcExtensions {

        #region [ Asset Model ]

        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="node">
        ///   The gRPC asset model node.
        /// </param>
        /// <returns>
        ///   The adapter asset model node.
        /// </returns>
        public static AssetModel.AssetModelNode ToAdapterAssetModelNode(this Grpc.AssetModelNode node) {
            if (node == null) {
                return null;
            }

            return AssetModel.AssetModelNode.Create(
                node.Id,
                node.Name,
                node.Description,
                string.IsNullOrWhiteSpace(node.Parent)
                    ? null
                    : node.Parent,
                node.HasChildren,
                node.Measurements.Select(x => AssetModel.AssetModelNodeMeasurement.Create(x.Name, x.AdapterId, RealTimeData.TagSummary.Create(x.Tag.Id, x.Tag.Name, x.Tag.Description, x.Tag.Units, x.Tag.DataType.ToAdapterVariantType()))).ToArray(),
                node.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


        /// <summary>
        /// Converts the object to its gRPC.
        /// </summary>
        /// <param name="node">
        ///   The adapter asset model node.
        /// </param>
        /// <returns>
        ///   The gRPC asset model node.
        /// </returns>
        public static Grpc.AssetModelNode ToGrpcAssetModelNode(this AssetModel.AssetModelNode node) {
            if (node == null) {
                return null;
            }

            var result = new Grpc.AssetModelNode() {
                Id = node.Id ?? string.Empty,
                Name = node.Name ?? string.Empty,
                Description = node.Description ?? string.Empty,
                Parent = node.Parent ?? string.Empty,
                HasChildren = node.HasChildren
            };

            if (node.Measurements != null && node.Measurements.Any()) {
                foreach (var item in node.Measurements) {
                    result.Measurements.Add(new Grpc.AssetModelNodeMeasurement() {
                        Name = item.Name ?? string.Empty,
                        AdapterId = item.AdapterId ?? string.Empty,
                        Tag = new Grpc.TagSummary() {
                            Id = item.Tag?.Id ?? string.Empty,
                            Name = item.Tag?.Name ?? string.Empty,
                            Description = item.Tag?.Description ?? string.Empty,
                            Units = item.Tag?.Units ?? string.Empty,
                            DataType = item.Tag?.DataType.ToGrpcVariantType() ?? Grpc.VariantType.Unknown
                        }
                    });
                }
            }
            if (node.Properties != null) {
                foreach (var item in node.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcAdapterProperty());
                }
            }

            return result;
        }

        #endregion

        #region [ Common ]

        /// <summary>
        /// Converts the value to its adapter equivalent.
        /// </summary>
        /// <param name="type">
        ///   The gRPC variant type.
        /// </param>
        /// <returns>
        ///   The adapter variant type.
        /// </returns>
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
                case Grpc.VariantType.Url:
                    return Common.VariantType.Url;
                case Grpc.VariantType.Unknown:
                default:
                    return Common.VariantType.Unknown;
            }
        }


        /// <summary>
        /// Converts the value to its gRPC equivalent.
        /// </summary>
        /// <param name="type">
        ///   The adapter variant type.
        /// </param>
        /// <returns>
        ///   The gRPC variant type.
        /// </returns>
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
                case Common.VariantType.Url:
                    return Grpc.VariantType.Url;
                case Common.VariantType.Unknown:
                default:
                    return Grpc.VariantType.Unknown;
            }
        }


        /// <summary>
        /// Converts the value to its adapter equivalent.
        /// </summary>
        /// <param name="variant">
        ///   The gRPC variant value.
        /// </param>
        /// <returns>
        ///   The adapter variant value.
        /// </returns>
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
                    value = DateTime.TryParse(System.Text.Encoding.UTF8.GetString(bytes), null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt)
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
                    serializerOptions.Converters.AddDataCoreAdapterConverters();
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
                case Grpc.VariantType.Url:
                    value = Uri.TryCreate(System.Text.Encoding.UTF8.GetString(bytes), UriKind.Absolute, out var url)
                        ? url
                        : default;
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


        /// <summary>
        /// Converts the value to its gRPC equivalent.
        /// </summary>
        /// <param name="variant">
        ///   The adapter variant value.
        /// </param>
        /// <returns>
        ///   The gRPC variant value.
        /// </returns>
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
                    bytes = System.Text.Encoding.UTF8.GetBytes(variant.GetValueOrDefault<DateTime>().ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture));
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
                    serializerOptions.Converters.AddDataCoreAdapterConverters();
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
                case Common.VariantType.Url:
                    bytes = System.Text.Encoding.UTF8.GetBytes(variant.GetValueOrDefault<Uri>()?.ToString() ?? string.Empty);
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


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="property">
        ///   The gRPC property.
        /// </param>
        /// <returns>
        ///   The adapter property.
        /// </returns>
        public static Common.AdapterProperty ToAdapterProperty(this Grpc.AdapterProperty property) {
            if (property == null) {
                return null;
            }

            return Common.AdapterProperty.Create(
                property.Name,
                property.Value.ToAdapterVariant(),
                property.Description
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="property">
        ///   The adapter property.
        /// </param>
        /// <returns>
        ///   The gRPC property.
        /// </returns>
        public static Grpc.AdapterProperty ToGrpcAdapterProperty(this Common.AdapterProperty property) {
            if (property == null) {
                return null;
            }

            var result = new Grpc.AdapterProperty() {
                Name = property.Name ?? string.Empty,
                Value = property.Value.ToGrpcVariant(),
                Description = property.Description ?? string.Empty
            };

            return result;
        }


        /// <summary>
        /// Converts the value to its adapter equivalent.
        /// </summary>
        /// <param name="status">
        ///   The gRPC write operation status.
        /// </param>
        /// <returns>
        ///   The adapter write operation status.
        /// </returns>
        public static Common.WriteStatus ToAdapterWriteStatus(this Grpc.WriteOperationStatus status) {
            switch (status) {
                case Grpc.WriteOperationStatus.Success:
                    return Common.WriteStatus.Success;
                case Grpc.WriteOperationStatus.Fail:
                    return Common.WriteStatus.Fail;
                case Grpc.WriteOperationStatus.Pending:
                    return Common.WriteStatus.Pending;
                case Grpc.WriteOperationStatus.Unknown:
                default:
                    return Common.WriteStatus.Unknown;
            }
        }


        /// <summary>
        /// Converts the value to its gRPC equivalent.
        /// </summary>
        /// <param name="status">
        ///   The adapter write operation status.
        /// </param>
        /// <returns>
        ///   The gRPC write operation status.
        /// </returns>
        public static Grpc.WriteOperationStatus ToGrpcWriteStatus(this Common.WriteStatus status) {
            switch (status) {
                case Common.WriteStatus.Success:
                    return Grpc.WriteOperationStatus.Success;
                case Common.WriteStatus.Fail:
                    return Grpc.WriteOperationStatus.Fail;
                case Common.WriteStatus.Pending:
                    return Grpc.WriteOperationStatus.Pending;
                case Common.WriteStatus.Unknown:
                default:
                    return Grpc.WriteOperationStatus.Unknown;
            }
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="hostInfo">
        ///   The gRPC host into.
        /// </param>
        /// <returns>
        ///   The adapter host info.
        /// </returns>
        public static Common.HostInfo ToAdapterHostInfo(this Grpc.HostInfo hostInfo) {
            if (hostInfo == null) {
                return null;
            }

            return Common.HostInfo.Create(
                hostInfo.Name,
                hostInfo.Description,
                hostInfo.Version,
                hostInfo.VendorInfo.ToAdapterVendorInfo(),
                hostInfo.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="hostInfo">
        ///   The adapter host into.
        /// </param>
        /// <returns>
        ///   The gRPC host info.
        /// </returns>
        public static Grpc.HostInfo ToGrpcHostInfo(this Common.HostInfo hostInfo) {
            if (hostInfo == null) {
                return null;
            }

            var result = new Grpc.HostInfo() { 
                Name = hostInfo.Name ?? string.Empty,
                Description = hostInfo.Description ?? string.Empty,
                Version = hostInfo.Version ?? string.Empty,
                VendorInfo = hostInfo.Vendor.ToGrpcVendorInfo()
            };

            if (hostInfo.Properties != null) {
                foreach (var item in hostInfo.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcAdapterProperty());
                }
            }

            return result;
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="vendorInfo">
        ///   The gRPC vendor into.
        /// </param>
        /// <returns>
        ///   The adapter vendor info.
        /// </returns>
        public static Common.VendorInfo ToAdapterVendorInfo(this Grpc.VendorInfo vendorInfo) {
            if (vendorInfo == null) {
                return null;
            }

            return Common.VendorInfo.Create(vendorInfo.Name, vendorInfo.Url);
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="vendorInfo">
        ///   The adapter vendor into.
        /// </param>
        /// <returns>
        ///   The gRPC vendor info.
        /// </returns>
        public static Grpc.VendorInfo ToGrpcVendorInfo(this Common.VendorInfo vendorInfo) {
            if (vendorInfo == null) {
                return null;
            }

            return new Grpc.VendorInfo() { 
                Name = vendorInfo.Name ?? string.Empty,
                Url = vendorInfo.Url ?? string.Empty
            };
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The gRPC adapter descriptor.
        /// </param>
        /// <returns>
        ///   The adapter descriptor.
        /// </returns>
        public static Common.AdapterDescriptor ToAdapterDescriptor(this Grpc.AdapterDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return Common.AdapterDescriptor.Create(descriptor.Id, descriptor.Name, descriptor.Description);
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The adapter descriptor.
        /// </param>
        /// <returns>
        ///   The gRPC adapter descriptor.
        /// </returns>
        public static Grpc.AdapterDescriptor ToGrpcAdapterDescriptor(this Common.AdapterDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return new Grpc.AdapterDescriptor() { 
                Id = descriptor.Id ?? string.Empty,
                Name = descriptor.Name ?? string.Empty,
                Description = descriptor.Description ?? string.Empty
            };
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The gRPC extended adapter descriptor.
        /// </param>
        /// <returns>
        ///   The extended adapter descriptor.
        /// </returns>
        public static Common.AdapterDescriptorExtended ToExtendedAdapterDescriptor(this Grpc.ExtendedAdapterDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return Common.AdapterDescriptorExtended.Create(
                descriptor.AdapterDescriptor?.Id,
                descriptor.AdapterDescriptor?.Name,
                descriptor.AdapterDescriptor?.Description,
                descriptor.Features,
                descriptor.Extensions,
                descriptor.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The extended adapter descriptor.
        /// </param>
        /// <returns>
        ///   The gRPC extended adapter descriptor.
        /// </returns>
        public static Grpc.ExtendedAdapterDescriptor ToGrpcExtendedAdapterDescriptor(this Common.AdapterDescriptorExtended descriptor) {
            if (descriptor == null) {
                return null;
            }

            var result = new Grpc.ExtendedAdapterDescriptor() { 
                AdapterDescriptor = ToGrpcAdapterDescriptor(descriptor)
            };

            if (descriptor.Features != null) {
                foreach (var item in descriptor.Features) {
                    if (item == null) {
                        continue;
                    }
                    result.Features.Add(item);
                }
            }

            if (descriptor.Extensions != null) {
                foreach (var item in descriptor.Extensions) {
                    if (item == null) {
                        continue;
                    }
                    result.Extensions.Add(item);
                }
            }

            if (descriptor.Properties != null) {
                foreach (var item in descriptor.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcAdapterProperty());
                }
            }

            return result;
        }

        #endregion

        #region [ Diagnostics ]

        /// <summary>
        /// Converts the value to its adapter equivalent.
        /// </summary>
        /// <param name="status">
        ///   The gRPC health status.
        /// </param>
        /// <returns>
        ///   The adapter health status.
        /// </returns>
        public static Diagnostics.HealthStatus ToAdapterHealthStatus(this Grpc.HealthStatus status) {
            switch (status) {
                case Grpc.HealthStatus.Healthy:
                    return Diagnostics.HealthStatus.Healthy;
                case Grpc.HealthStatus.Degraded:
                    return Diagnostics.HealthStatus.Degraded;
                case Grpc.HealthStatus.Unhealthy:
                default:
                    return Diagnostics.HealthStatus.Unhealthy;
            }
        }


        /// <summary>
        /// Converts the value to its gRPC equivalent.
        /// </summary>
        /// <param name="status">
        ///   The adapter health status.
        /// </param>
        /// <returns>
        ///   The gRPC health status.
        /// </returns>
        public static Grpc.HealthStatus ToGrpcHealthStatus(this Diagnostics.HealthStatus status) {
            switch (status) {
                case Diagnostics.HealthStatus.Healthy:
                    return Grpc.HealthStatus.Healthy;
                case Diagnostics.HealthStatus.Degraded:
                    return Grpc.HealthStatus.Degraded;
                case Diagnostics.HealthStatus.Unhealthy:
                default:
                    return Grpc.HealthStatus.Unhealthy;
            }
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="healthCheckResult">
        ///   The gRPC health check result.
        /// </param>
        /// <returns>
        ///   The adapter health check result.
        /// </returns>
        public static Diagnostics.HealthCheckResult ToAdapterHealthCheckResult(this Grpc.HealthCheckResult healthCheckResult) {
            if (healthCheckResult == null) {
                return Diagnostics.HealthCheckResult.Unhealthy(nameof(Diagnostics.HealthCheckResult));
            }

            return new Diagnostics.HealthCheckResult(
                healthCheckResult.DisplayName,
                healthCheckResult.Status.ToAdapterHealthStatus(),
                healthCheckResult.Description,
                healthCheckResult.Error,
                healthCheckResult.Data,
                healthCheckResult.InnerResults?.Select(x => x.ToAdapterHealthCheckResult()).ToArray()
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="healthCheckResult">
        ///   The adapter health check result.
        /// </param>
        /// <returns>
        ///   The gRPC health check result.
        /// </returns>
        public static Grpc.HealthCheckResult ToGrpcHealthCheckResult(this Diagnostics.HealthCheckResult healthCheckResult) {
            var result = new Grpc.HealthCheckResult() {
                DisplayName = healthCheckResult.DisplayName ?? string.Empty,
                Status = healthCheckResult.Status.ToGrpcHealthStatus(),
                Description = healthCheckResult.Description ?? string.Empty,
                Error = healthCheckResult.Error ?? string.Empty
            };

            if (healthCheckResult.Data != null) {
                result.Data.Add(healthCheckResult.Data);
            }

            if (healthCheckResult.InnerResults != null) {
                foreach (var item in healthCheckResult.InnerResults) {
                    result.InnerResults.Add(item.ToGrpcHealthCheckResult());
                }
            }

            return result;
        }

        #endregion

        #region [ Events ]

        /// <summary>
        /// Converts the value to its adapter equivalent.
        /// </summary>
        /// <param name="readDirection">
        ///   The gRPC event read direction.
        /// </param>
        /// <returns>
        ///   The adapter event read direction.
        /// </returns>
        public static Events.EventReadDirection ToAdapterEventReadDirection(this Grpc.EventReadDirection readDirection) {
            switch (readDirection) {
                case Grpc.EventReadDirection.Backwards:
                    return Events.EventReadDirection.Backwards;
                case Grpc.EventReadDirection.Forwards:
                default:
                    return Events.EventReadDirection.Forwards;
            }
        }


        /// <summary>
        /// Converts the value to its gRPC equivalent.
        /// </summary>
        /// <param name="readDirection">
        ///   The adapter event read direction.
        /// </param>
        /// <returns>
        ///   The gRPC event read direction.
        /// </returns>
        public static Grpc.EventReadDirection ToGrpcEventReadDirection(this Events.EventReadDirection readDirection) {
            switch (readDirection) {
                case Events.EventReadDirection.Backwards:
                    return Grpc.EventReadDirection.Backwards;
                case Events.EventReadDirection.Forwards:
                default:
                    return Grpc.EventReadDirection.Forwards;
            }
        }


        /// <summary>
        /// Converts the value to its adapter equivalent.
        /// </summary>
        /// <param name="priority">
        ///   The gRPC event priority.
        /// </param>
        /// <returns>
        ///   The adapter event priority.
        /// </returns>
        public static Events.EventPriority ToAdapterEventPriority(this Grpc.EventPriority priority) {
            switch (priority) {
                case Grpc.EventPriority.Low:
                    return Events.EventPriority.Low;
                case Grpc.EventPriority.Medium:
                    return Events.EventPriority.Medium;
                case Grpc.EventPriority.High:
                    return Events.EventPriority.High;
                case Grpc.EventPriority.Critical:
                    return Events.EventPriority.Critical;
                case Grpc.EventPriority.Unknown:
                default:
                    return Events.EventPriority.Unknown;
            }
        }


        /// <summary>
        /// Converts the value to its gRPC equivalent.
        /// </summary>
        /// <param name="priority">
        ///   The adapter event priority.
        /// </param>
        /// <returns>
        ///   The gRPC event priority.
        /// </returns>
        public static Grpc.EventPriority ToGrpcEventPriority(this Events.EventPriority priority) {
            switch (priority) {
                case Events.EventPriority.Low:
                    return Grpc.EventPriority.Low;
                case Events.EventPriority.Medium:
                    return Grpc.EventPriority.Medium;
                case Events.EventPriority.High:
                    return Grpc.EventPriority.High;
                case Events.EventPriority.Critical:
                    return Grpc.EventPriority.Critical;
                case Events.EventPriority.Unknown:
                default:
                    return Grpc.EventPriority.Unknown;
            }
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="message">
        ///   The gRPC event message.
        /// </param>
        /// <returns>
        ///   The adapter event message.
        /// </returns>
        public static Events.EventMessage ToAdapterEventMessage(this Grpc.EventMessage message) {
            if (message == null) {
                return null;
            }

            return Events.EventMessage.Create(
                message.Id,
                message.Topic,
                message.UtcEventTime.ToDateTime(),
                message.Priority.ToAdapterEventPriority(),
                message.Category,
                message.Message,
                message.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="message">
        ///   The adapter event message.
        /// </param>
        /// <returns>
        ///   The gRPC event message.
        /// </returns>
        public static Grpc.EventMessage ToGrpcEventMessage(this Events.EventMessageBase message) {
            if (message == null) {
                return null;
            }

            var result = new Grpc.EventMessage() {
                Category = message.Category ?? string.Empty,
                Id = message.Id ?? string.Empty,
                Topic = message.Topic ?? string.Empty,
                Message = message.Message ?? string.Empty,
                Priority = message.Priority.ToGrpcEventPriority(),
                UtcEventTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(message.UtcEventTime)
            };

            if (message.Properties != null) {
                foreach (var item in message.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcAdapterProperty());
                }
            }

            return result;
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="message">
        ///   The gRPC event message with cursor position.
        /// </param>
        /// <returns>
        ///   The adapter event message with cursor position.
        /// </returns>
        public static Events.EventMessageWithCursorPosition ToAdapterEventMessageWithCursorPosition(this Grpc.EventMessageWithCursorPosition message) {
            if (message == null) {
                return null;
            }

            return Events.EventMessageWithCursorPosition.Create(
                message.EventMessage.Id,
                message.EventMessage.Topic,
                message.EventMessage.UtcEventTime.ToDateTime(),
                message.EventMessage.Priority.ToAdapterEventPriority(),
                message.EventMessage.Category,
                message.EventMessage.Message,
                message.EventMessage.Properties.Select(x => x.ToAdapterProperty()).ToArray(),
                message.CursorPosition
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="message">
        ///   The adapter event message with cursor position.
        /// </param>
        /// <returns>
        ///   The adapter event message with cursor position.
        /// </returns>
        public static Grpc.EventMessageWithCursorPosition ToGrpcEventMessageWithCursorPosition(this Events.EventMessageWithCursorPosition message) {
            if (message == null) {
                return null;
            }

            var result = new Grpc.EventMessageWithCursorPosition() {
                CursorPosition = message.CursorPosition,
                EventMessage = ToGrpcEventMessage(message)
            };

            return result;
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="writeRequest">
        ///   The gRPC event message write item.
        /// </param>
        /// <returns>
        ///   The adapter event message write item.
        /// </returns>
        public static Events.WriteEventMessageItem ToAdapterWriteEventMessageItem(this Grpc.WriteEventMessageRequest writeRequest) {
            if (writeRequest == null) {
                return null;
            } 

            return new Events.WriteEventMessageItem() {
                CorrelationId = writeRequest.CorrelationId,
                EventMessage = Events.EventMessage.Create(
                    writeRequest.Message?.Id,
                    writeRequest.Message?.Topic,
                    writeRequest.Message?.UtcEventTime?.ToDateTime() ?? DateTime.MinValue,
                    writeRequest.Message?.Priority.ToAdapterEventPriority() ?? Events.EventPriority.Unknown,
                    writeRequest.Message?.Category,
                    writeRequest.Message?.Message,
                    writeRequest.Message?.Properties.Select(x => x.ToAdapterProperty())
                )
            };
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="item">
        ///   The adapter event message write item.
        /// </param>
        /// <param name="adapterId">
        ///   The target adapter ID.
        /// </param>
        /// <returns>
        ///   The adapter event message write item.
        /// </returns>
        public static Grpc.WriteEventMessageRequest ToGrpcWriteEventMessageItem(this Events.WriteEventMessageItem item, string adapterId) {
            if (item == null) {
                return null;
            }

            return new Grpc.WriteEventMessageRequest() {
                AdapterId = adapterId ?? string.Empty,
                CorrelationId = item.CorrelationId ?? string.Empty,
                Message = item.EventMessage.ToGrpcEventMessage()
            };
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="result">
        ///   The gRPC write event message result.
        /// </param>
        /// <returns>
        ///   The adapter write event message result.
        /// </returns>
        public static Events.WriteEventMessageResult ToAdapterWriteEventMessageResult(this Grpc.WriteEventMessageResult result) {
            if (result == null) {
                return null;
            }

            return Events.WriteEventMessageResult.Create(
                result.CorrelationId,
                result.WriteStatus.ToAdapterWriteStatus(),
                result.Notes,
                result.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="adapterResult">
        ///   The adapter write event message result.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <returns>
        ///   The gRPC write event message result.
        /// </returns>
        public static Grpc.WriteEventMessageResult ToGrpcWriteEventMessageResult(this Events.WriteEventMessageResult adapterResult, string adapterId) {
            if (adapterResult == null) {
                return null;
            }
            
            var result = new Grpc.WriteEventMessageResult() {
                AdapterId = adapterId ?? string.Empty,
                CorrelationId = adapterResult.CorrelationId ?? string.Empty,
                Notes = adapterResult.Notes ?? string.Empty,
                WriteStatus = adapterResult.Status.ToGrpcWriteStatus()
            };

            if (adapterResult.Properties != null) {
                foreach (var item in adapterResult.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcAdapterProperty());
                }
            }

            return result;
        }

        #endregion

        #region [ Extensions ]

        public static Common.FeatureDescriptor ToAdapterFeatureDescriptor(this Grpc.FeatureDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return new Common.FeatureDescriptor() { 
                Uri = Uri.TryCreate(descriptor.FeatureUri, UriKind.Absolute, out var uri)
                    ? uri
                    : null,
                DisplayName = descriptor.DisplayName,
                Description = descriptor.Description
            };
        }


        public static Grpc.FeatureDescriptor ToGrpcFeatureDescriptor(this Common.FeatureDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return new Grpc.FeatureDescriptor() {
                FeatureUri = descriptor.Uri?.ToString() ?? string.Empty,
                DisplayName = descriptor.DisplayName ?? string.Empty,
                Description = descriptor.Description ?? string.Empty
            };
        }



        public static Extensions.ExtensionFeatureOperationDescriptor ToAdapterExtensionOperatorDescriptor(this Grpc.ExtensionFeatureOperationDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return new Extensions.ExtensionFeatureOperationDescriptor() { 
                OperationId = Uri.TryCreate(descriptor.OperationId, UriKind.Absolute, out var uri)
                    ? uri
                    : null,
                OperationType = descriptor.OperationType.ToAdapterExtensionFeatureOperationType(),
                Name = descriptor.Name,
                Description = descriptor.Description,
                Input = descriptor.Input.ToAdapterExtensionFeatureParameterDescriptor(),
                Output = descriptor.Output.ToAdapterExtensionFeatureParameterDescriptor()
            };
        }


        public static Grpc.ExtensionFeatureOperationDescriptor ToGrpcExtensionOperatorDescriptor(this Extensions.ExtensionFeatureOperationDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return new Grpc.ExtensionFeatureOperationDescriptor() {
                OperationId = descriptor.OperationId?.ToString() ?? string.Empty,
                OperationType = descriptor.OperationType.ToGrpcExtensionFeatureOperationType(),
                Name = descriptor.Name ?? string.Empty,
                Description = descriptor.Description ?? string.Empty,
                Input = descriptor.Input.ToGrpcExtensionFeatureParameterDescriptor(),
                Output = descriptor.Output.ToGrpcExtensionFeatureParameterDescriptor()
            };
        }


        public static Extensions.ExtensionFeatureOperationParameterDescriptor ToAdapterExtensionFeatureParameterDescriptor(this Grpc.ExtensionFeatureOperationParameterDescriptor descriptor) {
            if (descriptor == null) {
                return new Extensions.ExtensionFeatureOperationParameterDescriptor();
            }

            return new Extensions.ExtensionFeatureOperationParameterDescriptor() {
                Description = descriptor.Description,
                ExampleValue = descriptor.ExampleValue
            };
        }


        public static Grpc.ExtensionFeatureOperationParameterDescriptor ToGrpcExtensionFeatureParameterDescriptor(this Extensions.ExtensionFeatureOperationParameterDescriptor descriptor) {
            if (descriptor == null) {
                return new Grpc.ExtensionFeatureOperationParameterDescriptor() { 
                    Description = string.Empty,
                    ExampleValue = string.Empty
                };
            }

            return new Grpc.ExtensionFeatureOperationParameterDescriptor() {
                Description = descriptor.Description ?? string.Empty,
                ExampleValue = descriptor.ExampleValue ?? string.Empty
            };
        }


        public static Extensions.ExtensionFeatureOperationType ToAdapterExtensionFeatureOperationType(this Grpc.ExtensionFeatureOperationType operationType) {
            switch (operationType) {
                case Grpc.ExtensionFeatureOperationType.Stream:
                    return Extensions.ExtensionFeatureOperationType.Stream;
                case Grpc.ExtensionFeatureOperationType.DuplexStream:
                    return Extensions.ExtensionFeatureOperationType.DuplexStream;
                default:
                    return Extensions.ExtensionFeatureOperationType.Invoke;
            }
        }


        public static Grpc.ExtensionFeatureOperationType ToGrpcExtensionFeatureOperationType(this Extensions.ExtensionFeatureOperationType operationType) {
            switch (operationType) {
                case Extensions.ExtensionFeatureOperationType.Stream:
                    return Grpc.ExtensionFeatureOperationType.Stream;
                case Extensions.ExtensionFeatureOperationType.DuplexStream:
                    return Grpc.ExtensionFeatureOperationType.DuplexStream;
                default:
                    return Grpc.ExtensionFeatureOperationType.Invoke;
            }
        }

        #endregion

        #region [ Real Time Data ]

        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="tagDefinition">
        ///   The gRPC tag definition.
        /// </param>
        /// <returns>
        ///   The adapter tag definition.
        /// </returns>
        public static RealTimeData.TagDefinition ToAdapterTagDefinition(this Grpc.TagDefinition tagDefinition) {
            if (tagDefinition == null) {
                return null;
            }

            return RealTimeData.TagDefinition.Create(
                tagDefinition.Id,
                tagDefinition.Name,
                tagDefinition.Description,
                tagDefinition.Units,
                tagDefinition.DataType.ToAdapterVariantType(),
                tagDefinition.States.Select(x => x.ToAdapterDigitalState()).ToArray(),
                tagDefinition.Properties.Select(x => x.ToAdapterProperty()).ToArray(),
                tagDefinition.Labels
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="tag">
        ///   The adapter tag definition.
        /// </param>
        /// <returns>
        ///   The gRPC tag definition.
        /// </returns>
        public static Grpc.TagDefinition ToGrpcTagDefinition(this RealTimeData.TagDefinition tag) {
            if (tag == null) {
                return null;
            }

            var result = new Grpc.TagDefinition() {
                DataType = tag.DataType.ToGrpcVariantType(),
                Description = tag.Description ?? string.Empty,
                Id = tag.Id ?? string.Empty,
                Name = tag.Name ?? string.Empty,
                Units = tag.Units ?? string.Empty
            };

            if (tag.States != null) {
                foreach (var item in tag.States) {
                    if (item == null) {
                        continue;
                    }
                    result.States.Add(item.ToGrpcDigitalState());
                }
            }

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
                    result.Properties.Add(item.ToGrpcAdapterProperty());
                }
            }

            return result;
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="state">
        ///   The gRPC digital state.
        /// </param>
        /// <returns>
        ///   The adapter digital state.
        /// </returns>
        public static RealTimeData.DigitalState ToAdapterDigitalState(this Grpc.DigitalState state) {
            if (state == null) {
                return null;
            }

            return RealTimeData.DigitalState.Create(state.Name, state.Value);
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="state">
        ///   The adapter digital state.
        /// </param>
        /// <returns>
        ///   The gRPC digital state.
        /// </returns>
        public static Grpc.DigitalState ToGrpcDigitalState(this RealTimeData.DigitalState state) {
            if (state == null) {
                return null;
            }

            return new Grpc.DigitalState() {
                Name = state.Name ?? string.Empty,
                Value = state.Value
            };
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="set">
        ///   The gRPC digital state set.
        /// </param>
        /// <returns>
        ///   The adapter digital state set.
        /// </returns>
        public static RealTimeData.DigitalStateSet ToAdapterDigitalStateSet(this Grpc.DigitalStateSet set) {
            if (set == null) {
                return null;
            }

            return RealTimeData.DigitalStateSet.Create(set.Id, set.Name, set.States.Select(x => x.ToAdapterDigitalState()));
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="set">
        ///   The adapter digital state set.
        /// </param>
        /// <returns>
        ///   The gRPC digital state set.
        /// </returns>
        public static Grpc.DigitalStateSet ToGrpcDigitalStateSet(this RealTimeData.DigitalStateSet set) {
            if (set == null) {
                return null;
            }

            var result = new Grpc.DigitalStateSet() {
                Id = set.Id ?? string.Empty,
                Name = set.Name ?? string.Empty
            };

            if (set.States != null) {
                foreach (var item in set.States) {
                    if (item == null) {
                        continue;
                    }

                    result.States.Add(item.ToGrpcDigitalState());
                }
            }

            return result;
        }


        /// <summary>
        /// Converts the value to its adapter equivalent.
        /// </summary>
        /// <param name="status">
        ///   The gRPC tag value status.
        /// </param>
        /// <returns>
        ///   The adapter tag value status.
        /// </returns>
        public static RealTimeData.TagValueStatus ToAdapterTagValueStatus(this Grpc.TagValueStatus status) {
            switch (status) {
                case Grpc.TagValueStatus.Bad:
                    return RealTimeData.TagValueStatus.Bad;
                case Grpc.TagValueStatus.Good:
                    return RealTimeData.TagValueStatus.Good;
                case Grpc.TagValueStatus.Uncertain:
                default:
                    return RealTimeData.TagValueStatus.Uncertain;
            }
        }


        /// <summary>
        /// Converts the value to its gRPC equivalent.
        /// </summary>
        /// <param name="status">
        ///   The adapter tag value status.
        /// </param>
        /// <returns>
        ///   The gRPC tag value status.
        /// </returns>
        public static Grpc.TagValueStatus ToGrpcTagValueStatus(this RealTimeData.TagValueStatus status) {
            switch (status) {
                case RealTimeData.TagValueStatus.Bad:
                    return Grpc.TagValueStatus.Bad;
                case RealTimeData.TagValueStatus.Good:
                    return Grpc.TagValueStatus.Good;
                case RealTimeData.TagValueStatus.Uncertain:
                default:
                    return Grpc.TagValueStatus.Uncertain;
            }
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="tagValue">
        ///   The gRPC tag value.
        /// </param>
        /// <returns>
        ///   The adapter tag value.
        /// </returns>
        public static RealTimeData.TagValueExtended ToAdapterTagValue(this Grpc.TagValue tagValue) {
            if (tagValue == null) {
                return null;
            }

            return RealTimeData.TagValueExtended.Create(
                tagValue.UtcSampleTime.ToDateTime(),
                tagValue.Value.ToAdapterVariant(),
                tagValue.Status.ToAdapterTagValueStatus(),
                tagValue.Units,
                tagValue.Notes,
                tagValue.Error,
                tagValue.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="tagValue">
        ///   The adapter tag value.
        /// </param>
        /// <returns>
        ///   The gRPC tag value.
        /// </returns>
        public static Grpc.TagValue ToGrpcTagValue(this RealTimeData.TagValueExtended tagValue) {
            if (tagValue == null) {
                return null;
            }

            var result = new Grpc.TagValue() {
                Error = tagValue.Error ?? string.Empty,
                Notes = tagValue.Notes ?? string.Empty,
                Status = tagValue.Status.ToGrpcTagValueStatus(),
                Units = tagValue.Units ?? string.Empty,
                UtcSampleTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(tagValue.UtcSampleTime),
                Value = tagValue.Value.ToGrpcVariant()
            };

            if (tagValue.Properties != null) {
                foreach (var item in tagValue.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcAdapterProperty());
                }
            }

            return result;
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="result">
        ///   The gRPC tag value query result.
        /// </param>
        /// <returns>
        ///   The adapter tag value query result.
        /// </returns>
        public static RealTimeData.TagValueQueryResult ToAdapterTagValueQueryResult(this Grpc.TagValueQueryResult result) {
            if (result == null) {
                return null;
            }

            return RealTimeData.TagValueQueryResult.Create(
                result.TagId,
                result.TagName,
                result.Value.ToAdapterTagValue()
            );
        }


        /// <summary>
        /// Converts the object to its gRPC.
        /// </summary>
        /// <param name="value">
        ///   The gRPC tag value query result.
        /// </param>
        /// <param name="queryType">
        ///   The type of query used to retrieve the result.
        /// </param>
        /// <returns>
        ///   The adapter tag value query result.
        /// </returns>
        public static Grpc.TagValueQueryResult ToGrpcTagValueQueryResult(this RealTimeData.TagValueQueryResult value, Grpc.TagValueQueryType queryType) {
            return ToGrpcTagValueQueryResult(value?.Value, value?.TagId, value?.TagName, queryType);
        }


        /// <summary>
        /// Converts the object to a gRPC tag value query result.
        /// </summary>
        /// <param name="value">
        ///   The adapter tag value.
        /// </param>
        /// <param name="tagId">
        ///   The tag ID for the value.
        /// </param>
        /// <param name="tagName">
        ///   The tag name for the value.
        /// </param>
        /// <param name="queryType">
        ///   The type of query that was used to retrieve the value.
        /// </param>
        /// <returns>
        ///   The gRPC tag value query result.
        /// </returns>
        public static Grpc.TagValueQueryResult ToGrpcTagValueQueryResult(this RealTimeData.TagValueExtended value, string tagId, string tagName, Grpc.TagValueQueryType queryType) {
            if (value == null) {
                return null;
            }
            
            var result = new Grpc.TagValueQueryResult() {
                TagId = tagId ?? string.Empty,
                TagName = tagName ?? string.Empty,
                QueryType = queryType,
                Value = value.ToGrpcTagValue()
            };

            return result;
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="result">
        ///   The gRPC processed tag value query result.
        /// </param>
        /// <returns>
        ///   The adapter processed tag value query result.
        /// </returns>
        public static RealTimeData.ProcessedTagValueQueryResult ToAdapterProcessedTagValueQueryResult(this Grpc.ProcessedTagValueQueryResult result) {
            if (result == null) {
                return null;
            }

            return RealTimeData.ProcessedTagValueQueryResult.Create(
                result.TagId,
                result.TagName,
                result.Value.ToAdapterTagValue(),
                result.DataFunction
            );
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="value">
        ///   The gRPC processed tag value query result.
        /// </param>
        /// <param name="queryType">
        ///   The type of query used to retrieve the value.
        /// </param>
        /// <returns>
        ///   The adapter processed tag value query result.
        /// </returns>
        public static Grpc.ProcessedTagValueQueryResult ToGrpcProcessedTagValueQueryResult(this RealTimeData.ProcessedTagValueQueryResult value, Grpc.TagValueQueryType queryType) {
            var result = new Grpc.ProcessedTagValueQueryResult() {
                TagId = value?.TagId ?? string.Empty,
                TagName = value?.TagName ?? string.Empty,
                DataFunction = value?.DataFunction ?? string.Empty,
                QueryType = queryType,
                Value = value?.Value.ToGrpcTagValue()
            };

            return result;
        }


        /// <summary>
        /// Converts the value to its adapter equivalent.
        /// </summary>
        /// <param name="sampleTime">
        ///   The gRPC data function sample time type.
        /// </param>
        /// <returns>
        ///   The adapter data function sample time type.
        /// </returns>
        public static RealTimeData.DataFunctionSampleTimeType ToAdapterDataFunctionSampleTimeType(this Grpc.DataFunctionSampleTime sampleTime) {
            switch (sampleTime) {
                case Grpc.DataFunctionSampleTime.StartTime:
                    return RealTimeData.DataFunctionSampleTimeType.StartTime;
                case Grpc.DataFunctionSampleTime.EndTime:
                    return RealTimeData.DataFunctionSampleTimeType.EndTime;
                case Grpc.DataFunctionSampleTime.Raw:
                    return RealTimeData.DataFunctionSampleTimeType.Raw;
                case Grpc.DataFunctionSampleTime.Custom:
                    return RealTimeData.DataFunctionSampleTimeType.Custom;
                case Grpc.DataFunctionSampleTime.Unspecified:
                default:
                    return RealTimeData.DataFunctionSampleTimeType.Unspecified;
            }
        }


        /// <summary>
        /// Converts the value to its gRPC equivalent.
        /// </summary>
        /// <param name="sampleTime">
        ///   The adapter data function sample time type.
        /// </param>
        /// <returns>
        ///   The gRPC data function sample time type.
        /// </returns>
        public static Grpc.DataFunctionSampleTime ToGrpcDataFunctionSampleTimeType(this RealTimeData.DataFunctionSampleTimeType sampleTime) {
            switch (sampleTime) {
                case RealTimeData.DataFunctionSampleTimeType.StartTime:
                    return Grpc.DataFunctionSampleTime.StartTime;
                case RealTimeData.DataFunctionSampleTimeType.EndTime:
                    return Grpc.DataFunctionSampleTime.EndTime;
                case RealTimeData.DataFunctionSampleTimeType.Raw:
                    return Grpc.DataFunctionSampleTime.Raw;
                case RealTimeData.DataFunctionSampleTimeType.Custom:
                    return Grpc.DataFunctionSampleTime.Custom;
                case RealTimeData.DataFunctionSampleTimeType.Unspecified:
                default:
                    return Grpc.DataFunctionSampleTime.Unspecified;
            }
        }


        /// <summary>
        /// Converts the value to its adapter equivalent.
        /// </summary>
        /// <param name="status">
        ///   The gRPC data function status type.
        /// </param>
        /// <returns>
        ///   The adapter data function status type.
        /// </returns>
        public static RealTimeData.DataFunctionStatusType ToAdapterDataFunctionStatusType(this Grpc.DataFunctionStatus status) {
            switch (status) {
                case Grpc.DataFunctionStatus.PercentTime:
                    return RealTimeData.DataFunctionStatusType.PercentTime;
                case Grpc.DataFunctionStatus.PercentValues:
                    return RealTimeData.DataFunctionStatusType.PercentValues;
                case Grpc.DataFunctionStatus.Custom:
                    return RealTimeData.DataFunctionStatusType.Custom;
                case Grpc.DataFunctionStatus.Unspecified:
                default:
                    return RealTimeData.DataFunctionStatusType.Unspecified;
            }
        }


        /// <summary>
        /// Converts the value to its gRPC equivalent.
        /// </summary>
        /// <param name="status">
        ///   The adapter data function status type.
        /// </param>
        /// <returns>
        ///   The gRPC data function status type.
        /// </returns>
        public static Grpc.DataFunctionStatus ToGrpcDataFunctionStatusType(this RealTimeData.DataFunctionStatusType status) {
            switch (status) {
                case RealTimeData.DataFunctionStatusType.PercentTime:
                    return Grpc.DataFunctionStatus.PercentTime;
                case RealTimeData.DataFunctionStatusType.PercentValues:
                    return Grpc.DataFunctionStatus.PercentValues;
                case RealTimeData.DataFunctionStatusType.Custom:
                    return Grpc.DataFunctionStatus.Custom;
                case RealTimeData.DataFunctionStatusType.Unspecified:
                default:
                    return Grpc.DataFunctionStatus.Unspecified;
            }
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The gRPC data function descriptor.
        /// </param>
        /// <returns>
        ///   The adapter data function descriptor.
        /// </returns>
        public static RealTimeData.DataFunctionDescriptor ToAdapterDataFunctionDescriptor(this Grpc.DataFunctionDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return RealTimeData.DataFunctionDescriptor.Create(
                descriptor.Id,
                descriptor.Name,
                descriptor.Description,
                descriptor.SampleTimeType.ToAdapterDataFunctionSampleTimeType(),
                descriptor.StatusType.ToAdapterDataFunctionStatusType(),
                descriptor.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The adapter data function descriptor.
        /// </param>
        /// <returns>
        ///   The gRPC data function descriptor.
        /// </returns>
        public static Grpc.DataFunctionDescriptor ToGrpcDataFunctionDescriptor(this RealTimeData.DataFunctionDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }
            
            var result = new Grpc.DataFunctionDescriptor() {
                Id = descriptor.Id ?? string.Empty,
                Name = descriptor.Name ?? string.Empty,
                Description = descriptor.Description ?? string.Empty,
                SampleTimeType = descriptor.SampleTime.ToGrpcDataFunctionSampleTimeType(),
                StatusType = descriptor.Status.ToGrpcDataFunctionStatusType()
            };

            if (descriptor.Properties != null && descriptor.Properties.Any()) {
                foreach (var prop in descriptor.Properties) {
                    if (prop == null) {
                        continue;
                    }
                    result.Properties.Add(prop.ToGrpcAdapterProperty());
                }
            }

            return result;
        }


        /// <summary>
        /// Converts the value to its adapter equivalent.
        /// </summary>
        /// <param name="boundaryType">
        ///   The gRPC raw data boundary type.
        /// </param>
        /// <returns>
        ///   The adapter raw data boundary type.
        /// </returns>
        public static RealTimeData.RawDataBoundaryType ToAdapterRawDataBoundaryType(this Grpc.RawDataBoundaryType boundaryType) {
            switch (boundaryType) {
                case Grpc.RawDataBoundaryType.Outside:
                    return RealTimeData.RawDataBoundaryType.Outside;
                case Grpc.RawDataBoundaryType.Inside:
                default:
                    return RealTimeData.RawDataBoundaryType.Inside;
            }
        }


        /// <summary>
        /// Converts the value to its gRPC equivalent.
        /// </summary>
        /// <param name="boundaryType">
        ///   The adapter raw data boundary type.
        /// </param>
        /// <returns>
        ///   The gRPC raw data boundary type.
        /// </returns>
        public static Grpc.RawDataBoundaryType ToGrpcRawDataBoundaryType(this RealTimeData.RawDataBoundaryType boundaryType) {
            switch (boundaryType) {
                case RealTimeData.RawDataBoundaryType.Outside:
                    return Grpc.RawDataBoundaryType.Outside;
                case RealTimeData.RawDataBoundaryType.Inside:
                default:
                    return Grpc.RawDataBoundaryType.Inside;
            }
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="writeRequest">
        ///   The gRPC tag value write item.
        /// </param>
        /// <returns>
        ///   The adapter tag value write item.
        /// </returns>
        public static RealTimeData.WriteTagValueItem ToAdapterWriteTagValueItem(this Grpc.WriteTagValueRequest writeRequest) {
            if (writeRequest == null) {
                return null;
            }

            return new RealTimeData.WriteTagValueItem() {
                CorrelationId = writeRequest.CorrelationId,
                TagId = writeRequest.TagId,
                Value = RealTimeData.TagValue.Create(
                    writeRequest.UtcSampleTime.ToDateTime(),
                    writeRequest.Value.ToAdapterVariant(),
                    writeRequest.Status.ToAdapterTagValueStatus(),
                    writeRequest.Units
                )
            };
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="item">
        ///   The adapter tag value write item.
        /// </param>
        /// <param name="adapterId">
        ///   The destination adapter ID.
        /// </param>
        /// <returns>
        ///   The gRPC tag value write item.
        /// </returns>
        public static Grpc.WriteTagValueRequest ToGrpcWriteTagValueItem(this RealTimeData.WriteTagValueItem item, string adapterId) {
            if (item == null) {
                return null;
            }

            return new Grpc.WriteTagValueRequest() {
                AdapterId = adapterId,
                CorrelationId = item.CorrelationId ?? string.Empty,
                TagId = item.TagId ?? string.Empty,
                UtcSampleTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(item.Value.UtcSampleTime),
                Value = item.Value.Value.ToGrpcVariant(),
                Status = item.Value.Status.ToGrpcTagValueStatus(),
                Units = item.Value.Units ?? string.Empty
            };
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="result">
        ///   The gRPC tag value write result.
        /// </param>
        /// <returns>
        ///   The adapter tag value write result.
        /// </returns>
        public static RealTimeData.WriteTagValueResult ToAdapterWriteTagValueResult(this Grpc.WriteTagValueResult result) {
            if (result == null) {
                return null;
            }

            return RealTimeData.WriteTagValueResult.Create(
                result.CorrelationId,
                result.TagId,
                result.WriteStatus.ToAdapterWriteStatus(),
                result.Notes,
                result.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="adapterResult">
        ///   The adapter tag value write result.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter ID that the value was written to.
        /// </param>
        /// <returns>
        ///   The gRPC tag value write result.
        /// </returns>
        public static Grpc.WriteTagValueResult ToGrpcWriteTagValueResult(this RealTimeData.WriteTagValueResult adapterResult, string adapterId) {
            if (adapterResult == null) {
                return null;
            }

            var result = new Grpc.WriteTagValueResult() {
                AdapterId = adapterId ?? string.Empty,
                CorrelationId = adapterResult.CorrelationId ?? string.Empty,
                Notes = adapterResult.Notes ?? string.Empty,
                TagId = adapterResult.TagId ?? string.Empty,
                WriteStatus = adapterResult.Status.ToGrpcWriteStatus()
            };

            if (adapterResult.Properties != null) {
                foreach (var item in adapterResult.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcAdapterProperty());
                }
            }

            return result;
        }


        /// <summary>
        /// Converts the value to its adapter equivalent.
        /// </summary>
        /// <param name="annotationType">
        ///   The gRPC annotation type.
        /// </param>
        /// <returns>
        ///   The adapter annotation type.
        /// </returns>
        public static RealTimeData.AnnotationType ToAdapterAnnotationType(this Grpc.AnnotationType annotationType) {
            switch (annotationType) {
                case Grpc.AnnotationType.TimeRange:
                    return RealTimeData.AnnotationType.TimeRange;
                case Grpc.AnnotationType.Instantaneous:
                default:
                    return RealTimeData.AnnotationType.Unknown;
            }
        }


        /// <summary>
        /// Converts the value to its gRPC equivalent.
        /// </summary>
        /// <param name="annotationType">
        ///   The adapter annotation type.
        /// </param>
        /// <returns>
        ///   The gRPC annotation type.
        /// </returns>
        public static Grpc.AnnotationType ToGrpcAnnotationType(this RealTimeData.AnnotationType annotationType) {
            switch (annotationType) {
                case RealTimeData.AnnotationType.TimeRange:
                    return Grpc.AnnotationType.TimeRange;
                case RealTimeData.AnnotationType.Instantaneous:
                default:
                    return Grpc.AnnotationType.Unknown;
            }
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="annotation">
        ///   The gRPC annotation.
        /// </param>
        /// <returns>
        ///   The adapter annotation.
        /// </returns>
        public static RealTimeData.TagValueAnnotation ToAdapterTagValueAnnotation(this Grpc.TagValueAnnotationBase annotation) {
            if (annotation == null) {
                return null;
            }

            return RealTimeData.TagValueAnnotation.Create(
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
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="annotation">
        ///   The adapter annotation.
        /// </param>
        /// <returns>
        ///   The gRPC annotation.
        /// </returns>
        public static Grpc.TagValueAnnotationBase ToGrpcTagValueAnnotationBase(this RealTimeData.TagValueAnnotation annotation) {
            if (annotation == null) {
                return null;
            }

            var result = new Grpc.TagValueAnnotationBase() {
                AnnotationType = annotation.AnnotationType.ToGrpcAnnotationType(),
                Description = annotation.Description ?? string.Empty,
                HasUtcEndTime = annotation.UtcEndTime.HasValue,
                UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annotation.UtcStartTime),
                Value = annotation.Value
            };

            if (result.HasUtcEndTime) {
                result.UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annotation.UtcEndTime.Value);
            }

            if (annotation.Properties != null) {
                foreach (var item in annotation.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcAdapterProperty());
                }
            }

            return result;
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="annotation">
        ///   The gRPC annotation.
        /// </param>
        /// <returns>
        ///   The adapter annotation.
        /// </returns>
        public static RealTimeData.TagValueAnnotationExtended ToAdapterTagValueAnnotation(this Grpc.TagValueAnnotation annotation) {
            if (annotation == null) {
                return null;
            }

            return RealTimeData.TagValueAnnotationExtended.Create(
                annotation.Id,
                annotation.Annotation?.AnnotationType.ToAdapterAnnotationType() ?? RealTimeData.AnnotationType.Unknown,
                annotation.Annotation?.UtcStartTime.ToDateTime() ?? DateTime.MinValue,
                annotation.Annotation?.HasUtcEndTime ?? false
                    ? annotation.Annotation?.UtcEndTime.ToDateTime()
                    : null,
                annotation.Annotation?.Value,
                annotation.Annotation?.Description,
                annotation.Annotation?.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="annotation">
        ///   The adapter annotation.
        /// </param>
        /// <returns>
        ///   The gRPC annotation.
        /// </returns>
        public static Grpc.TagValueAnnotation ToGrpcTagValueAnnotation(this RealTimeData.TagValueAnnotationExtended annotation) {
            if (annotation == null) {
                return null;
            }

            var result = new Grpc.TagValueAnnotation() {
                Id = annotation.Id,
                Annotation = annotation.ToGrpcTagValueAnnotationBase()
            };

            if (result.Annotation.HasUtcEndTime) {
                result.Annotation.UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annotation.UtcEndTime.Value);
            }

            if (annotation.Properties != null) {
                foreach (var item in annotation.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Annotation.Properties.Add(item.ToGrpcAdapterProperty());
                }
            }

            return result;
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="result">
        ///   The gRPC annotation query result.
        /// </param>
        /// <returns>
        ///   The adapter annotation query result.
        /// </returns>
        public static RealTimeData.TagValueAnnotationQueryResult ToAdapterTagValueAnnotationQueryResult(this Grpc.TagValueAnnotationQueryResult result) {
            if (result == null) {
                return null;
            }

            return RealTimeData.TagValueAnnotationQueryResult.Create(
                result.TagId,
                result.TagName,
                result.Annotation.ToAdapterTagValueAnnotation()
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="annotation">
        ///   The adapter annotation query result.
        /// </param>
        /// <returns>
        ///   The gRPC annotation query result.
        /// </returns>
        public static Grpc.TagValueAnnotationQueryResult ToGrpcTagValueAnnotationQueryResult(this RealTimeData.TagValueAnnotationQueryResult annotation) {
            if (annotation == null) {
                return null;
            }
            
            var result = new Grpc.TagValueAnnotationQueryResult() {
                TagId = annotation.TagId ?? string.Empty,
                TagName = annotation.TagName ?? string.Empty,
                Annotation = annotation.Annotation.ToGrpcTagValueAnnotation()
            };

            return result;
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="result">
        ///   The gRPC annotation write result.
        /// </param>
        /// <returns>
        ///   The adapter annotation write result.
        /// </returns>
        public static RealTimeData.WriteTagValueAnnotationResult ToAdapterWriteTagValueAnnotationResult(this Grpc.WriteTagValueAnnotationResult result) {
            if (result == null) {
                return null;
            }

            return RealTimeData.WriteTagValueAnnotationResult.Create(
                result.TagId,
                result.AnnotationId,
                result.WriteStatus.ToAdapterWriteStatus(),
                result.Notes,
                result.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="adapterResult">
        ///   The adapter annotation write result.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter ID that the annotation was written to.
        /// </param>
        /// <returns>
        ///   The gRPC annotation write result.
        /// </returns>
        public static Grpc.WriteTagValueAnnotationResult ToGrpcWriteTagValueAnnotationResult(this RealTimeData.WriteTagValueAnnotationResult adapterResult, string adapterId) {
            if (adapterResult == null) {
                return null;
            }

            var result = new Grpc.WriteTagValueAnnotationResult() {
                AdapterId = adapterId ?? string.Empty,
                TagId = adapterResult.TagId ?? string.Empty,
                AnnotationId = adapterResult.AnnotationId ?? string.Empty,
                WriteStatus = adapterResult.Status.ToGrpcWriteStatus(),
                Notes = adapterResult.Notes ?? string.Empty
            };

            if (adapterResult.Properties != null) {
                foreach (var item in adapterResult.Properties) {
                    if (item == null) {
                        continue;
                    }
                    result.Properties.Add(item.ToGrpcAdapterProperty());
                }
            }

            return result;
        }

        #endregion

    }
}
