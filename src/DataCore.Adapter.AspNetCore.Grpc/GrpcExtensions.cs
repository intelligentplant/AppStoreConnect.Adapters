// ##################################################################################################
// # IMPORTANT!                                                                                     #
// #                                                                                                #
// # This file is shared between DataCore.Adapter.Grpc.Client and DataCore.Adapter.AspNetCore.Grpc. # 
// # Be careful when making changes!                                                                #
// ##################################################################################################

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Json;

namespace DataCore.Adapter {

    /// <summary>
    /// Extension methods for converting from common adapter types to their gRPC-equivalents and 
    /// vice versa.
    /// </summary>
    public static class GrpcExtensions {

        #region [ Helpers ]

        /// <summary>
        /// Gets JSON serializaton options.
        /// </summary>
        /// <returns>
        ///   JSON serialization options.
        /// </returns>
        private static System.Text.Json.JsonSerializerOptions GetJsonSerializerOptions() {
            var result = new System.Text.Json.JsonSerializerOptions();
            result.Converters.AddDataCoreAdapterConverters();
            return result;
        }


        /// <summary>
        /// Deserializes the specified JSON bytes.
        /// </summary>
        /// <typeparam name="T">
        ///   The target type.
        /// </typeparam>
        /// <param name="bytes">
        ///   The JSON bytes.
        /// </param>
        /// <returns>
        ///   The deserialized object.
        /// </returns>
        private static T? ReadJsonValue<T>(byte[] bytes) {
            return System.Text.Json.JsonSerializer.Deserialize<T>(System.Text.Encoding.UTF8.GetString(bytes), GetJsonSerializerOptions());
        }


        /// <summary>
        /// Serializes the specified value to JSON.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   The serialized value.
        /// </returns>
        private static byte[] WriteJsonValue<T>(T value) {
            return System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(value, GetJsonSerializerOptions()));
        }


        /// <summary>
        /// Reads an N-dimensional array from the specified JSON bytes.
        /// </summary>
        /// <typeparam name="T">
        ///   The array element type.
        /// </typeparam>
        /// <param name="bytes">
        ///   The JSON bytes.
        /// </param>
        /// <param name="dimensions">
        ///   The array dimensions.
        /// </param>
        /// <returns>
        ///   The <see cref="Array"/> object that was deserialized from the JSON bytes.
        /// </returns>
        private static Array ReadJsonArray<T>(byte[] bytes, IEnumerable<int> dimensions) {
            return VariantConverter.ReadArray<T>(System.Text.Encoding.UTF8.GetString(bytes), dimensions.ToArray(), GetJsonSerializerOptions());
        }


        /// <summary>
        /// Serializes an N-dimensional array to JSON.
        /// </summary>
        /// <param name="array">
        ///   The array.
        /// </param>
        /// <returns>
        ///   The serialized JSON bytes.
        /// </returns>
        private static byte[] WriteJsonArray(Array array) {
            return VariantConverter.WriteArray(array);
        }

        #endregion

        #region [ Asset Model ]

        /// <summary>
        /// Converts a gRPC asset model node type to its adapter equivalent.
        /// </summary>
        /// <param name="nodeType">
        ///   The gRPC node type.
        /// </param>
        /// <returns>
        ///   The adapter node type.
        /// </returns>
        public static AssetModel.NodeType ToAdapterAssetModelNodeType(this Grpc.AssetModelNodeType nodeType) {
            switch (nodeType) {
                case Grpc.AssetModelNodeType.Object:
                    return AssetModel.NodeType.Object;
                case Grpc.AssetModelNodeType.Variable:
                    return AssetModel.NodeType.Variable;
                case Grpc.AssetModelNodeType.ObjectType:
                    return AssetModel.NodeType.ObjectType;
                case Grpc.AssetModelNodeType.VariableType:
                    return AssetModel.NodeType.VariableType;
                case Grpc.AssetModelNodeType.Other:
                    return AssetModel.NodeType.Other;
                default:
                    return AssetModel.NodeType.Unknown;
            }
        }


        /// <summary>
        /// Converts an adapter asset model node type to its gRPC equivalent.
        /// </summary>
        /// <param name="nodeType">
        ///   The adapter node type.
        /// </param>
        /// <returns>
        ///   The gRPC node type.
        /// </returns>
        public static Grpc.AssetModelNodeType ToGrpcAssetModelNodeType(this AssetModel.NodeType nodeType) {
            switch (nodeType) {
                case AssetModel.NodeType.Object:
                    return Grpc.AssetModelNodeType.Object;
                case AssetModel.NodeType.Variable:
                    return Grpc.AssetModelNodeType.Variable;
                case AssetModel.NodeType.ObjectType:
                    return Grpc.AssetModelNodeType.ObjectType;
                case AssetModel.NodeType.VariableType:
                    return Grpc.AssetModelNodeType.VariableType;
                case AssetModel.NodeType.Other:
                    return Grpc.AssetModelNodeType.Other;
                default:
                    return Grpc.AssetModelNodeType.Unknown;
            }
        }


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
                throw new ArgumentNullException(nameof(node));
            }

            return new AssetModel.AssetModelNode(
                node.Id,
                node.Name,
                node.NodeType.ToAdapterAssetModelNodeType(),
                node.NodeSubType,
                node.Description,
                string.IsNullOrWhiteSpace(node.Parent)
                    ? null
                    : node.Parent,
                node.HasChildren,
                node.HasDataReference
                    ? new AssetModel.DataReference(
                        node.DataReference.AdapterId, 
                        node.DataReference.TagNameOrId
                    )
                    : null,
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
                throw new ArgumentNullException(nameof(node));
            }

            var result = new Grpc.AssetModelNode() {
                Id = node.Id ?? string.Empty,
                Name = node.Name ?? string.Empty,
                NodeType = node.NodeType.ToGrpcAssetModelNodeType(),
                NodeSubType = node.NodeSubType ?? string.Empty,
                Description = node.Description ?? string.Empty,
                Parent = node.Parent ?? string.Empty,
                HasChildren = node.HasChildren,
                HasDataReference = node.DataReference != null,
                DataReference = new Grpc.AssetModelDataReference() {
                    AdapterId = node.DataReference?.AdapterId ?? string.Empty,
                    TagNameOrId = node.DataReference?.Tag ?? string.Empty
                }
            };

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
                    return VariantType.ExtensionObject;
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
                case Common.VariantType.ExtensionObject:
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

            var isArray = variant.ArrayDimensions.Count > 0;
            object value;

            switch (variant.Type) {
                case Grpc.VariantType.Boolean:
                    value = isArray 
                        ? (object) ReadJsonArray<bool>(bytes, variant.ArrayDimensions) 
                        : BitConverter.ToBoolean(bytes, 0);
                    break;
                case Grpc.VariantType.Byte:
                    value = isArray
                        ? (object) ReadJsonArray<byte>(bytes, variant.ArrayDimensions)
                        : bytes.FirstOrDefault();
                    break;
                case Grpc.VariantType.Datetime:
                    value = isArray
                        ? (object) ReadJsonArray<DateTime>(bytes, variant.ArrayDimensions)
                        : DateTime.TryParse(System.Text.Encoding.UTF8.GetString(bytes), null, DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt)
                            ? dt
                            : default;
                    break;
                case Grpc.VariantType.Double:
                    value = isArray
                        ? (object) ReadJsonArray<double>(bytes, variant.ArrayDimensions)
                        : BitConverter.ToDouble(bytes, 0);
                    break;
                case Grpc.VariantType.Float:
                    value = isArray
                        ? (object) ReadJsonArray<float>(bytes, variant.ArrayDimensions)
                        : BitConverter.ToSingle(bytes, 0);
                    break;
                case Grpc.VariantType.Int16:
                    value = isArray
                        ? (object) ReadJsonArray<short>(bytes, variant.ArrayDimensions)
                        : BitConverter.ToInt16(bytes, 0);
                    break;
                case Grpc.VariantType.Int32:
                    value = isArray
                        ? (object) ReadJsonArray<int>(bytes, variant.ArrayDimensions)
                        : BitConverter.ToInt32(bytes, 0);
                    break;
                case Grpc.VariantType.Int64:
                    value = isArray
                        ? (object) ReadJsonArray<long>(bytes, variant.ArrayDimensions)
                        : BitConverter.ToInt64(bytes, 0);
                    break;
                case Grpc.VariantType.Null:
                    value = null!;
                    break;
                case Grpc.VariantType.Object:
                    value = isArray
                        ? (object) ReadJsonArray<EncodedObject>(bytes, variant.ArrayDimensions)
                        : ReadJsonValue<EncodedObject>(bytes)!;
                    break;
                case Grpc.VariantType.Sbyte:
                    value = isArray
                        ? (object) ReadJsonArray<sbyte>(bytes, variant.ArrayDimensions)
                        : (sbyte) bytes.FirstOrDefault();
                    break;
                case Grpc.VariantType.String:
                    value = isArray
                        ? (object) ReadJsonArray<string>(bytes, variant.ArrayDimensions)
                        : System.Text.Encoding.UTF8.GetString(bytes);
                    break;
                case Grpc.VariantType.Timespan:
                    value = isArray
                        ? (object) ReadJsonArray<TimeSpan>(bytes, variant.ArrayDimensions)
                        : TimeSpan.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out var ts)
                            ? ts
                            : default;
                    break;
                case Grpc.VariantType.Uint16:
                    value = isArray
                        ? (object) ReadJsonArray<ushort>(bytes, variant.ArrayDimensions)
                        : BitConverter.ToUInt16(bytes, 0);
                    break;
                case Grpc.VariantType.Uint32:
                    value = isArray
                        ? (object) ReadJsonArray<uint>(bytes, variant.ArrayDimensions)
                        : BitConverter.ToUInt32(bytes, 0);
                    break;
                case Grpc.VariantType.Uint64:
                    value = isArray
                        ? (object) ReadJsonArray<ulong>(bytes, variant.ArrayDimensions)
                        : BitConverter.ToUInt64(bytes, 0);
                    break;
                case Grpc.VariantType.Url:
                    value = isArray
                        ? (object) ReadJsonArray<Uri>(bytes, variant.ArrayDimensions)
                        : Uri.TryCreate(System.Text.Encoding.UTF8.GetString(bytes), UriKind.RelativeOrAbsolute, out var url)
                            ? url
                            : default!;
                    break;
                case Grpc.VariantType.Unknown:
                default:
                    value = null!;
                    break;
            }

            return Common.Variant.FromValue(value);
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

            var isArray = variant.ArrayDimensions?.Length > 0;

            byte[] bytes;

            if (isArray) {
                bytes = WriteJsonArray((Array) variant.Value);
            }
            else {
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
                    case Common.VariantType.ExtensionObject:
                        bytes = WriteJsonValue(variant.GetValueOrDefault<EncodedObject>());
                        break;
                    case Common.VariantType.SByte:
                        bytes = new[] { (byte) variant.GetValueOrDefault<sbyte>() };
                        break;
                    case Common.VariantType.String:
                        bytes = System.Text.Encoding.UTF8.GetBytes(variant.GetValueOrDefault(string.Empty)!);
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
            }

            var result = new Grpc.Variant() {
                Value = bytes.Length > 0
                    ? Google.Protobuf.ByteString.CopyFrom(bytes)
                    : Google.Protobuf.ByteString.Empty,
                Type = variant.Type.ToGrpcVariantType()
            };

            if (isArray) {
                result.ArrayDimensions.AddRange(variant.ArrayDimensions!);
            }

            return result;
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
                throw new ArgumentNullException(nameof(property));
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
                throw new ArgumentNullException(nameof(property));
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
        /// <param name="changeType">
        ///   The gRPC configuration change type.
        /// </param>
        /// <returns>
        ///   The adapter configuration change type.
        /// </returns>
        public static ConfigurationChangeType ToAdapterConfigurationChangeType(this Grpc.ConfigurationChangeType changeType) {
            switch (changeType) {
                case Grpc.ConfigurationChangeType.Created:
                    return ConfigurationChangeType.Created;
                case Grpc.ConfigurationChangeType.Updated:
                    return ConfigurationChangeType.Updated;
                case Grpc.ConfigurationChangeType.Deleted:
                    return ConfigurationChangeType.Deleted;
                default:
                    return ConfigurationChangeType.Unknown;
            }
        }


        /// <summary>
        /// Converts the value to its gRPC equivalent.
        /// </summary>
        /// <param name="changeType">
        ///   The adapter configuration change type.
        /// </param>
        /// <returns>
        ///   The gRPC configuration change type.
        /// </returns>
        public static Grpc.ConfigurationChangeType ToGrpcConfigurationChangeType(this ConfigurationChangeType changeType) {
            switch (changeType) {
                case ConfigurationChangeType.Created:
                    return Grpc.ConfigurationChangeType.Created;
                case ConfigurationChangeType.Updated:
                    return Grpc.ConfigurationChangeType.Updated;
                case ConfigurationChangeType.Deleted:
                    return Grpc.ConfigurationChangeType.Deleted;
                default:
                    return Grpc.ConfigurationChangeType.Unknown;
            }
        }


        /// <summary>
        /// Converts the value to its adapter equivalent.
        /// </summary>
        /// <param name="change">
        ///   The gRPC configuration change.
        /// </param>
        /// <returns>
        ///   The adapter configuration change.
        /// </returns>
        public static Diagnostics.ConfigurationChange ToAdapterConfigurationChange(this Grpc.ConfigurationChange change) {
            if (change == null) {
                throw new ArgumentNullException(nameof(change));
            }

            return new Diagnostics.ConfigurationChange(
                change.ItemType,
                change.ItemId,
                change.ItemName,
                change.ChangeType.ToAdapterConfigurationChangeType(),
                change.Properties.Select(x => x.ToAdapterProperty())
            );
        }


        /// <summary>
        /// Converts the value to its gRPC equivalent.
        /// </summary>
        /// <param name="change">
        ///   The adapter configuration change.
        /// </param>
        /// <returns>
        ///   The gRPC configuration change.
        /// </returns>
        public static Grpc.ConfigurationChange ToGrpcConfigurationChange(this Diagnostics.ConfigurationChange change) {
            if (change == null) {
                throw new ArgumentNullException(nameof(change));
            }

            var result = new Grpc.ConfigurationChange() {
                ItemType = change.ItemType ?? string.Empty,
                ItemId = change.ItemId ?? string.Empty,
                ItemName = change.ItemName ?? string.Empty,
                ChangeType = change.ChangeType.ToGrpcConfigurationChangeType()
            };

            if (change.Properties?.Any() ?? false) {
                foreach (var prop in change.Properties) {
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
                throw new ArgumentNullException(nameof(hostInfo));
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
                throw new ArgumentNullException(nameof(hostInfo));
            }

            var result = new Grpc.HostInfo() { 
                Name = hostInfo.Name ?? string.Empty,
                Description = hostInfo.Description ?? string.Empty,
                Version = hostInfo.Version ?? string.Empty,
                VendorInfo = hostInfo.Vendor?.ToGrpcVendorInfo() ?? new Grpc.VendorInfo()
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
                throw new ArgumentNullException(nameof(vendorInfo));
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
                throw new ArgumentNullException(nameof(vendorInfo));
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
                throw new ArgumentNullException(nameof(descriptor));
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
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new Grpc.AdapterDescriptor() { 
                Id = descriptor.Id ?? string.Empty,
                Name = descriptor.Name ?? string.Empty,
                Description = descriptor.Description ?? string.Empty
            };
        }


        /// <summary>
        /// Converts a gRPC adapter type descriptor to its adapter equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <returns>
        ///   The converted descriptor
        /// </returns>
        public static Common.AdapterTypeDescriptor? ToAdapterTypeDescriptor(this Grpc.AdapterTypeDescriptor descriptor) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrWhiteSpace(descriptor.Id)) {
                return null;
            }

            return new AdapterTypeDescriptor(
                new Uri(descriptor.Id, UriKind.Absolute),
                descriptor.Name,
                descriptor.Description,
                descriptor.Version,
                descriptor.VendorInfo?.ToAdapterVendorInfo(),
                descriptor.HelpUrl
            );
        }


        /// <summary>
        /// Converts an adapter type descriptor to its gRPC equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <returns>
        ///   The converted descriptor
        /// </returns>
        public static Grpc.AdapterTypeDescriptor ToGrpcAdapterTypeDescriptor(this Common.AdapterTypeDescriptor? descriptor) {
            if (descriptor == null) {
                return new Grpc.AdapterTypeDescriptor();
            }

            return new Grpc.AdapterTypeDescriptor() { 
                Id = descriptor.Id.ToString(),
                Name = descriptor.Name ?? string.Empty,
                Description = descriptor.Description ?? string.Empty,
                Version = descriptor.Version ?? string.Empty,
                VendorInfo = descriptor.Vendor == null
                    ? new Grpc.VendorInfo() { Name = string.Empty, Url = string.Empty }
                    : descriptor.Vendor.ToGrpcVendorInfo(),
                HelpUrl = descriptor.HelpUrl ?? string.Empty
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
                throw new ArgumentNullException(nameof(descriptor));
            }

            return Common.AdapterDescriptorExtended.Create(
                descriptor.AdapterDescriptor.Id,
                descriptor.AdapterDescriptor.Name,
                descriptor.AdapterDescriptor?.Description,
                descriptor.Features,
                descriptor.Extensions,
                descriptor.Properties.Select(x => x.ToAdapterProperty()).ToArray(),
                descriptor.TypeDescriptor.ToAdapterTypeDescriptor()
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
                throw new ArgumentNullException(nameof(descriptor));
            }

            var result = new Grpc.ExtendedAdapterDescriptor() { 
                AdapterDescriptor = ToGrpcAdapterDescriptor(descriptor),
                TypeDescriptor = descriptor.TypeDescriptor.ToGrpcAdapterTypeDescriptor()
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


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="obj">
        ///   The <see cref="EncodedObject"/>.
        /// </param>
        /// <returns>
        ///   The equivalent <see cref="Grpc.EncodedObject"/>.
        /// </returns>
        public static Grpc.EncodedObject ToGrpcEncodedObject(this EncodedObject obj) {
            if (obj == null) {
                throw new ArgumentNullException(nameof(obj));
            }

            return new Grpc.EncodedObject() { 
                TypeId = obj.TypeId.ToString(),
                Encoding = obj.Encoding,
                EncodedBody = Google.Protobuf.ByteString.CopyFrom(obj.ToByteArray())
            };
        }


        /// <summary>
        /// Converts the object to its adapter equivalent.
        /// </summary>
        /// <param name="obj">
        ///   The <see cref="Grpc.EncodedObject"/>.
        /// </param>
        /// <returns>
        ///   The equivalent <see cref="EncodedObject"/>.
        /// </returns>
        public static EncodedObject ToAdapterEncodedObject(this Grpc.EncodedObject obj) {
            if (obj == null) {
                throw new ArgumentNullException(nameof(obj));
            }

            return EncodedObject.Create(new Uri(obj.TypeId), obj.Encoding, obj.EncodedBody.ToByteArray());
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
                throw new ArgumentNullException(nameof(message));
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
                throw new ArgumentNullException(nameof(message));
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
                throw new ArgumentNullException(nameof(message));
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
                throw new ArgumentNullException(nameof(message));
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
        public static Events.WriteEventMessageItem ToAdapterWriteEventMessageItem(this Grpc.WriteEventMessageItem writeRequest) {
            if (writeRequest == null) {
                throw new ArgumentNullException(nameof(writeRequest));
            } 

            return new Events.WriteEventMessageItem() {
                CorrelationId = writeRequest.CorrelationId,
                EventMessage = Events.EventMessage.Create(
                    writeRequest.Message?.Id ?? Guid.NewGuid().ToString(),
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
        /// <returns>
        ///   The adapter event message write item.
        /// </returns>
        public static Grpc.WriteEventMessageItem ToGrpcWriteEventMessageItem(this Events.WriteEventMessageItem item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            return new Grpc.WriteEventMessageItem() {
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
                throw new ArgumentNullException(nameof(result));
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
        /// <returns>
        ///   The gRPC write event message result.
        /// </returns>
        public static Grpc.WriteEventMessageResult ToGrpcWriteEventMessageResult(this Events.WriteEventMessageResult adapterResult) {
            if (adapterResult == null) {
                throw new ArgumentNullException(nameof(adapterResult));
            }
            
            var result = new Grpc.WriteEventMessageResult() {
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

        /// <summary>
        /// Converts a gRPC feature descriptor to its adapter equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <returns>
        ///   The converted descriptor.
        /// </returns>
        public static Common.FeatureDescriptor ToAdapterFeatureDescriptor(this Grpc.FeatureDescriptor descriptor) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new Common.FeatureDescriptor() { 
                Uri = Uri.TryCreate(descriptor.FeatureUri, UriKind.Absolute, out var uri)
                    ? uri
                    : null!,
                Category = descriptor.Category,
                DisplayName = descriptor.DisplayName,
                Description = descriptor.Description
            };
        }


        /// <summary>
        /// Converts an adapter feature descriptor to its gRPC equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <returns>
        ///   The converted descriptor.
        /// </returns>
        public static Grpc.FeatureDescriptor ToGrpcFeatureDescriptor(this Common.FeatureDescriptor descriptor) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new Grpc.FeatureDescriptor() {
                FeatureUri = descriptor.Uri?.ToString() ?? string.Empty,
                Category = descriptor.Category ?? string.Empty,
                DisplayName = descriptor.DisplayName ?? string.Empty,
                Description = descriptor.Description ?? string.Empty
            };
        }


        /// <summary>
        /// Converts a gRPC extension operation descriptor to its adapter equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <returns>
        ///   The converted descriptor.
        /// </returns>
        public static Extensions.ExtensionFeatureOperationDescriptor ToAdapterExtensionOperatorDescriptor(this Grpc.ExtensionFeatureOperationDescriptor descriptor) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new Extensions.ExtensionFeatureOperationDescriptor() { 
                OperationId = Uri.TryCreate(descriptor.OperationId, UriKind.Absolute, out var uri)
                    ? uri
                    : null!,
                OperationType = descriptor.OperationType.ToAdapterExtensionFeatureOperationType(),
                Name = descriptor.Name,
                Description = descriptor.Description,
                Inputs = descriptor.Inputs.Select(x => x.ToAdapterExtensionFeatureParameterDescriptor()).ToArray(),
                Outputs = descriptor.Outputs.Select(x => x.ToAdapterExtensionFeatureParameterDescriptor()).ToArray()
            };
        }


        /// <summary>
        /// Converts an adapter extension operation descriptor to its gRPC equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <returns>
        ///   The converted descriptor.
        /// </returns>
        public static Grpc.ExtensionFeatureOperationDescriptor ToGrpcExtensionOperatorDescriptor(this Extensions.ExtensionFeatureOperationDescriptor descriptor) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var result = new Grpc.ExtensionFeatureOperationDescriptor() {
                OperationId = descriptor.OperationId?.ToString() ?? string.Empty,
                OperationType = descriptor.OperationType.ToGrpcExtensionFeatureOperationType(),
                Name = descriptor.Name ?? string.Empty,
                Description = descriptor.Description ?? string.Empty
            };

            if (descriptor.Inputs != null) {
                foreach (var item in descriptor.Inputs) {
                    if (item == null) {
                        continue;
                    }
                    result.Inputs.Add(item.ToGrpcExtensionFeatureParameterDescriptor());
                }
            }

            if (descriptor.Outputs != null) {
                foreach (var item in descriptor.Outputs) {
                    if (item == null) {
                        continue;
                    }
                    result.Outputs.Add(item.ToGrpcExtensionFeatureParameterDescriptor());
                }
            }

            return result;
        }


        /// <summary>
        /// Converts a gRPC extension operation parameter descriptor to its adapter equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <returns>
        ///   The converted descriptor.
        /// </returns>
        public static Extensions.ExtensionFeatureOperationParameterDescriptor ToAdapterExtensionFeatureParameterDescriptor(this Grpc.ExtensionFeatureOperationParameterDescriptor descriptor) {
            if (descriptor == null) {
                return new Extensions.ExtensionFeatureOperationParameterDescriptor();
            }

            return new Extensions.ExtensionFeatureOperationParameterDescriptor() {
                Ordinal = descriptor.Ordinal,
                VariantType = descriptor.VariantType.ToAdapterVariantType(),
                ArrayRank = descriptor.ArrayRank,
                TypeId = Uri.TryCreate(descriptor.TypeId, UriKind.Absolute, out var uri) ? uri : null,
                Description = descriptor.Description
            };
        }


        /// <summary>
        /// Converts an adapter extension operation descriptor to its gRPC equivalent.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <returns>
        ///   The converted descriptor.
        /// </returns>
        public static Grpc.ExtensionFeatureOperationParameterDescriptor ToGrpcExtensionFeatureParameterDescriptor(this Extensions.ExtensionFeatureOperationParameterDescriptor descriptor) {
            if (descriptor == null) {
                return new Grpc.ExtensionFeatureOperationParameterDescriptor() { 
                    TypeId = string.Empty,
                    Description = string.Empty
                };
            }

            return new Grpc.ExtensionFeatureOperationParameterDescriptor() {
                Ordinal = descriptor.Ordinal,
                VariantType = descriptor.VariantType.ToGrpcVariantType(),
                ArrayRank = descriptor.ArrayRank,
                TypeId = descriptor.TypeId?.ToString() ?? string.Empty,
                Description = descriptor.Description ?? string.Empty
            };
        }


        /// <summary>
        /// Converts a gRPC extension feature operation type to its adapter equivalent.
        /// </summary>
        /// <param name="operationType">
        ///   The operation type.
        /// </param>
        /// <returns>
        ///   The converted operation type.
        /// </returns>
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


        /// <summary>
        /// Converts a gRPC extension feature operation type to its adapter equivalent.
        /// </summary>
        /// <param name="operationType">
        ///   The operation type.
        /// </param>
        /// <returns>
        ///   The converted operation type.
        /// </returns>
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
        public static Tags.TagDefinition ToAdapterTagDefinition(this Grpc.TagDefinition tagDefinition) {
            if (tagDefinition == null) {
                throw new ArgumentNullException(nameof(tagDefinition));
            }

            return new Tags.TagDefinition(
                tagDefinition.Id,
                tagDefinition.Name,
                tagDefinition.Description,
                tagDefinition.Units,
                tagDefinition.DataType.ToAdapterVariantType(),
                tagDefinition.States.Select(x => x.ToAdapterDigitalState()).ToArray(),
                tagDefinition.SupportedFeatures.Select(x => Uri.TryCreate(x, UriKind.Absolute, out var uri) ? uri : null).Where(x => x != null).ToArray()!,
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
        public static Grpc.TagDefinition ToGrpcTagDefinition(this Tags.TagDefinition tag) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
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

            if (tag.SupportedFeatures != null) {
                foreach (var item in tag.SupportedFeatures) {
                    if (item == null) {
                        continue;
                    }
                    result.SupportedFeatures.Add(item.ToString());
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
        public static Tags.DigitalState ToAdapterDigitalState(this Grpc.DigitalState state) {
            if (state == null) {
                throw new ArgumentNullException(nameof(state));
            }

            return Tags.DigitalState.Create(state.Name, state.Value);
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
        public static Grpc.DigitalState ToGrpcDigitalState(this Tags.DigitalState state) {
            if (state == null) {
                throw new ArgumentNullException(nameof(state));
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
        public static Tags.DigitalStateSet ToAdapterDigitalStateSet(this Grpc.DigitalStateSet set) {
            if (set == null) {
                throw new ArgumentNullException(nameof(set));
            }

            return Tags.DigitalStateSet.Create(set.Id, set.Name, set.States.Select(x => x.ToAdapterDigitalState()));
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
        public static Grpc.DigitalStateSet ToGrpcDigitalStateSet(this Tags.DigitalStateSet set) {
            if (set == null) {
                throw new ArgumentNullException(nameof(set));
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
                throw new ArgumentNullException(nameof(tagValue));
            }

            return new RealTimeData.TagValueExtended(
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
                throw new ArgumentNullException(nameof(tagValue));
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
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="tagValue">
        ///   The adapter tag value.
        /// </param>
        /// <returns>
        ///   The gRPC tag value.
        /// </returns>
        public static Grpc.TagValue ToGrpcTagValue(this RealTimeData.TagValue tagValue) {
            if (tagValue == null) {
                throw new ArgumentNullException(nameof(tagValue));
            }

            var result = new Grpc.TagValue() {
                Error = string.Empty,
                Notes = string.Empty,
                Status = tagValue.Status.ToGrpcTagValueStatus(),
                Units = tagValue.Units ?? string.Empty,
                UtcSampleTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(tagValue.UtcSampleTime),
                Value = tagValue.Value.ToGrpcVariant()
            };

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
                throw new ArgumentNullException(nameof(result));
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
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            return ToGrpcTagValueQueryResult(value.Value, value.TagId, value.TagName, queryType);
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
                throw new ArgumentNullException(nameof(value));
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
                throw new ArgumentNullException(nameof(result));
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
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            
            var result = new Grpc.ProcessedTagValueQueryResult() {
                TagId = value.TagId ?? string.Empty,
                TagName = value.TagName ?? string.Empty,
                DataFunction = value.DataFunction ?? string.Empty,
                QueryType = queryType,
                Value = value.Value.ToGrpcTagValue()
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
                throw new ArgumentNullException(nameof(descriptor));
            }

            return RealTimeData.DataFunctionDescriptor.Create(
                descriptor.Id,
                descriptor.Name,
                descriptor.Description,
                descriptor.SampleTimeType.ToAdapterDataFunctionSampleTimeType(),
                descriptor.StatusType.ToAdapterDataFunctionStatusType(),
                descriptor.Properties.Select(x => x.ToAdapterProperty()).ToArray(),
                descriptor.Aliases.ToArray()
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
                throw new ArgumentNullException(nameof(descriptor));
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

            if (descriptor.Aliases != null) {
                result.Aliases.AddRange(descriptor.Aliases.Select(x => x ?? string.Empty));
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
        public static RealTimeData.WriteTagValueItem ToAdapterWriteTagValueItem(this Grpc.WriteTagValueItem writeRequest) {
            if (writeRequest == null) {
                throw new ArgumentNullException(nameof(writeRequest));
            }

            return new RealTimeData.WriteTagValueItem() {
                CorrelationId = writeRequest.CorrelationId,
                TagId = writeRequest.TagId,
                Value = writeRequest.Value.ToAdapterTagValue()
            };
        }


        /// <summary>
        /// Converts the object to its gRPC equivalent.
        /// </summary>
        /// <param name="item">
        ///   The adapter tag value write item.
        /// </param>
        /// <returns>
        ///   The gRPC tag value write item.
        /// </returns>
        public static Grpc.WriteTagValueItem ToGrpcWriteTagValueItem(this RealTimeData.WriteTagValueItem item) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            return new Grpc.WriteTagValueItem() {
                CorrelationId = item.CorrelationId ?? string.Empty,
                TagId = item.TagId ?? string.Empty,
                Value = item.Value.ToGrpcTagValue()
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
                throw new ArgumentNullException(nameof(result));
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
        /// <returns>
        ///   The gRPC tag value write result.
        /// </returns>
        public static Grpc.WriteTagValueResult ToGrpcWriteTagValueResult(this RealTimeData.WriteTagValueResult adapterResult) {
            if (adapterResult == null) {
                throw new ArgumentNullException(nameof(adapterResult));
            }

            var result = new Grpc.WriteTagValueResult() {
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
                throw new ArgumentNullException(nameof(annotation));
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
                throw new ArgumentNullException(nameof(annotation));
            }

            var result = new Grpc.TagValueAnnotationBase() {
                AnnotationType = annotation.AnnotationType.ToGrpcAnnotationType(),
                Description = annotation.Description ?? string.Empty,
                HasUtcEndTime = annotation.UtcEndTime.HasValue,
                UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annotation.UtcStartTime),
                Value = annotation.Value
            };

            if (result.HasUtcEndTime) {
                result.UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annotation.UtcEndTime!.Value);
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
                throw new ArgumentNullException(nameof(annotation));
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
                throw new ArgumentNullException(nameof(annotation));
            }

            var result = new Grpc.TagValueAnnotation() {
                Id = annotation.Id,
                Annotation = annotation.ToGrpcTagValueAnnotationBase()
            };

            if (result.Annotation.HasUtcEndTime) {
                result.Annotation.UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(annotation.UtcEndTime!.Value);
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
                throw new ArgumentNullException(nameof(result));
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
                throw new ArgumentNullException(nameof(annotation));
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
                throw new ArgumentNullException(nameof(result));
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
                throw new ArgumentNullException(nameof(adapterResult));
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
