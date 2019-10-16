using System.Linq;
using DataCore.Adapter.Grpc;

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
                return Common.Variant.Create(null);
            }

            return Common.Variant.Create(
                System.Text.Json.JsonSerializer.Deserialize(variant.Value.Span, typeof(object)),
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
            if (variant == null) {
                return new Grpc.Variant() { 
                    Value = Google.Protobuf.ByteString.Empty,
                    Type = Grpc.VariantType.Null
                };
            }

            return new Grpc.Variant() {
                Value = variant.Value == null
                    ? Google.Protobuf.ByteString.Empty
                    : Google.Protobuf.ByteString.CopyFromUtf8(System.Text.Json.JsonSerializer.Serialize(variant.Value)),
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
