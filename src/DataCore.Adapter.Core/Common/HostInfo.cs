using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Json;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes the hosting application.
    /// </summary>
    [JsonConverter(typeof(HostInfoConverter))]
    public class HostInfo {

        /// <summary>
        /// The application name.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// The application description.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// The Semantic Versioning v2 (https://semver.org/spec/v2.0.0.html) application version.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Information about the application vendor.
        /// </summary>
        public VendorInfo? Vendor { get; }

        /// <summary>
        /// Custom properties supplied by the hosting application.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; }


        /// <summary>
        /// The <see cref="HostInfo"/> to use when an instance is not provided by the hosting 
        /// application.
        /// </summary>
        public static HostInfo Unspecified { get; } = Create(SharedResources.HostInfo_Unspecified_Name, SharedResources.HostInfo_Unspecified_Description, null, null);


        /// <summary>
        /// Creates a new <see cref="HostInfo"/> object.
        /// </summary>
        /// <param name="name">
        ///   The host name.
        /// </param>
        /// <param name="description">
        ///   The host description.
        /// </param>
        /// <param name="version">
        ///   The host version. This will be parsed and converted to a Semantic Versioning v2 version 
        ///   (https://semver.org/spec/v2.0.0.html).
        /// </param>
        /// <param name="vendor">
        ///   The vendor information.
        /// </param>
        /// <param name="properties">
        ///   Additional host properties.
        /// </param>
        public HostInfo(string? name, string? description, string? version, VendorInfo? vendor, IEnumerable<AdapterProperty>? properties) {
            Name = name?.Trim();
            Description = description?.Trim();
            Version = AdapterTypeDescriptor.GetNormalisedVersion(version) ?? new Semver.SemVersion(0, 0, 0).ToString();
            Vendor = vendor;
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }


        /// <summary>
        /// Creates a new <see cref="HostInfo"/> object.
        /// </summary>
        /// <param name="name">
        ///   The host name.
        /// </param>
        /// <param name="description">
        ///   The host description.
        /// </param>
        /// <param name="version">
        ///   The host version. This will be parsed and converted to a Semantic Versioning v2 version 
        ///   (https://semver.org/spec/v2.0.0.html).
        /// </param>
        /// <param name="vendor">
        ///   The vendor information.
        /// </param>
        /// <param name="properties">
        ///   Additional host properties.
        /// </param>
        public static HostInfo Create(string? name, string? description, string? version, VendorInfo? vendor, params AdapterProperty[] properties) {
            return Create(name, description, version, vendor, (IEnumerable<AdapterProperty>) properties);
        }


        /// <summary>
        /// Creates a new <see cref="HostInfo"/> object.
        /// </summary>
        /// <param name="name">
        ///   The host name.
        /// </param>
        /// <param name="description">
        ///   The host description.
        /// </param>
        /// <param name="version">
        ///   The host version. This will be parsed and converted to a Semantic Versioning v2 version 
        ///   (https://semver.org/spec/v2.0.0.html).
        /// </param>
        /// <param name="vendor">
        ///   The vendor information.
        /// </param>
        /// <param name="properties">
        ///   Additional host properties.
        /// </param>
        public static HostInfo Create(string? name, string? description, string? version, VendorInfo? vendor, IEnumerable<AdapterProperty>? properties) {
            return new HostInfo(name, description, version, vendor, properties);
        }


        /// <summary>
        /// Creates a copy of a <see cref="HostInfo"/> object.
        /// </summary>
        /// <param name="hostInfo">
        ///   The object to copy.
        /// </param>
        /// <returns>
        ///   A new <see cref="HostInfo"/> object, with properties copied from the existing instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hostInfo"/> is <see langword="null"/>.
        /// </exception>
        public static HostInfo FromExisting(HostInfo hostInfo) {
            if (hostInfo == null) {
                throw new ArgumentNullException(nameof(hostInfo));
            }

            return Create(
                hostInfo.Name,
                hostInfo.Description,
                hostInfo.Version,
                hostInfo.Vendor == null
                    ? null
                    : VendorInfo.FromExisting(hostInfo.Vendor),
                hostInfo.Properties
            ); ;
        }

    }


    /// <summary>
    /// JSON converter for <see cref="HostInfo"/>.
    /// </summary>
    internal class HostInfoConverter : AdapterJsonConverter<HostInfo> {

        /// <inheritdoc/>
        public override HostInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string name = null!;
            string description = null!;
            string version = null!;
            VendorInfo vendor = null!;
            IEnumerable<AdapterProperty> properties = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(HostInfo.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = reader.GetString()!;
                }
                else if (string.Equals(propertyName, nameof(HostInfo.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = reader.GetString()!;
                }
                else if (string.Equals(propertyName, nameof(HostInfo.Version), StringComparison.OrdinalIgnoreCase)) {
                    version = reader.GetString()!;
                }
                else if (string.Equals(propertyName, nameof(HostInfo.Vendor), StringComparison.OrdinalIgnoreCase)) {
                    vendor = JsonSerializer.Deserialize<VendorInfo>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(HostInfo.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<IEnumerable<AdapterProperty>>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return HostInfo.Create(name, description, version, vendor, properties);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, HostInfo value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(HostInfo.Name), value.Name, options);
            WritePropertyValue(writer, nameof(HostInfo.Description), value.Description, options);
            WritePropertyValue(writer, nameof(HostInfo.Version), value.Version, options);
            WritePropertyValue(writer, nameof(HostInfo.Vendor), value.Vendor, options);
            WritePropertyValue(writer, nameof(HostInfo.Properties), value.Properties, options);

            writer.WriteEndObject();
        }
    }

}
