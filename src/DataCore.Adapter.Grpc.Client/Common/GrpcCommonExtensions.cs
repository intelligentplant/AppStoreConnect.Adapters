using System;
using System.Linq;
using DataCore.Adapter.Grpc;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Extension methods for converting from gRPC types to their adapter equivalents.
    /// </summary>
    public static class GrpcCommonExtensions {

        public static HostInfo ToAdapterHostInfo(this Grpc.HostInfo hostInfo) {
            if (hostInfo == null) {
                return null;
            }

            return HostInfo.Create(
                hostInfo.Name,
                hostInfo.Description,
                hostInfo.Version,
                hostInfo.VendorInfo == null 
                    ? null 
                    : VendorInfo.Create(hostInfo.VendorInfo.Name, hostInfo.VendorInfo.Url),
                hostInfo.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


        public static AdapterDescriptor ToAdapterDescriptor(this Grpc.AdapterDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return AdapterDescriptor.Create(descriptor.Id, descriptor.Name, descriptor.Description);
        }


        public static AdapterDescriptorExtended ToExtendedAdapterDescriptor(this Grpc.ExtendedAdapterDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return AdapterDescriptorExtended.Create(
                descriptor.AdapterDescriptor?.Id,
                descriptor.AdapterDescriptor?.Name,
                descriptor.AdapterDescriptor?.Description,
                descriptor.Features,
                descriptor.Extensions,
                descriptor.Properties.Select(x => x.ToAdapterProperty()).ToArray()
            );
        }


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

            return Variant.FromValue(
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


        public static AdapterProperty ToAdapterProperty(this Grpc.AdapterProperty property) {
            if (property == null) {
                return null;
            }

            return AdapterProperty.Create(
                property.Name,
                property.Value.ToAdapterVariant()
            );
        }


        public static Grpc.AdapterProperty ToGrpcProperty(this AdapterProperty property) {
            if (property == null) {
                return null;
            }

            var result = new Grpc.AdapterProperty() { 
                Name = property.Name ?? string.Empty,
                Value = property.Value.ToGrpcVariant()
            };

            return result;
        }


        public static WriteStatus ToAdapterWriteStatus(this Grpc.WriteOperationStatus status) {
            switch (status) {
                case WriteOperationStatus.Success:
                    return WriteStatus.Success;
                case WriteOperationStatus.Fail:
                    return WriteStatus.Fail;
                case WriteOperationStatus.Pending:
                    return WriteStatus.Pending;
                case WriteOperationStatus.Unknown:
                default:
                    return WriteStatus.Unknown;
            }
        }

    }
}
