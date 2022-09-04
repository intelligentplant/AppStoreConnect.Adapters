using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Json;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes an adapter type.
    /// </summary>
    [JsonConverter(typeof(AdapterTypeDescriptorConverter))]
    public class AdapterTypeDescriptor {

        /// <summary>
        /// The URI for the adapter type.
        /// </summary>
        public Uri Id { get; }

        /// <summary>
        /// The display name for the adapter type.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// The description for the adapter type.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// The adapter type version number, expressed as a Semantic Versioning v2 version.
        /// </summary>
        public string? Version { get; }

        /// <summary>
        /// The vendor information for the adapter type.
        /// </summary>
        public VendorInfo? Vendor { get; }

        /// <summary>
        /// The help URL for the adapter type.
        /// </summary>
        public string? HelpUrl { get; }


        /// <summary>
        /// Creates a new <see cref="AdapterTypeDescriptor"/> object.
        /// </summary>
        /// <param name="id">
        ///   The URI for the adapter type.
        /// </param>
        /// <param name="name">
        ///   The adapter type display name.
        /// </param>
        /// <param name="description">
        ///   The adapter type description.
        /// </param>
        /// <param name="version">
        ///   The adapter type version. This will be parsed and converted to a Semantic Versioning 
        ///   v2 version (https://semver.org/spec/v2.0.0.html).
        /// </param>
        /// <param name="vendor">
        ///   The adapter type vendor information.
        /// </param>
        /// <param name="helpUrl">
        ///   The help URL for the adapter type.
        /// </param>
        public AdapterTypeDescriptor(Uri id, string? name, string? description, string? version, VendorInfo? vendor, string? helpUrl) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name;
            Description = description;
            Version = GetNormalisedVersion(version);
            Vendor = vendor;
            HelpUrl = helpUrl;
        }


        /// <summary>
        /// Converts the specified version string into a normalised format.
        /// </summary>
        /// <param name="version">
        ///   The version string.
        /// </param>
        /// <param name="style">
        ///   The <see cref="Semver.SemVersionStyles"/> to use when converting <paramref name="version"/> 
        ///   into a <see cref="Semver.SemVersion"/> instance.
        /// </param>
        /// <returns></returns>
        /// <remarks>
        ///   <see cref="GetNormalisedVersion"/> will attempt to convert <paramref name="version"/> 
        ///   into a <see cref="Semver.SemVersion"/> instance and return a <see cref="string"/> 
        ///   representation of the version.
        /// </remarks>
        internal static string? GetNormalisedVersion(string? version, Semver.SemVersionStyles style = Semver.SemVersionStyles.Strict) {
            if (version == null) {
                return null;
            }

            return Semver.SemVersion.TryParse(version, style, out var semVer)
                ? semVer.ToString()
                : System.Version.TryParse(version, out var v)
                    ? Semver.SemVersion.FromVersion(v).ToString()
                    : null;
        }

    }


    /// <summary>
    /// JSON converter for <see cref="AdapterTypeDescriptor"/>.
    /// </summary>
    internal class AdapterTypeDescriptorConverter : AdapterJsonConverter<AdapterTypeDescriptor> {

        /// <inheritdoc/>
        public override AdapterTypeDescriptor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            Uri id = null!;
            string? name = null!;
            string? description = null!;
            string? version = null!;
            VendorInfo? vendor = null!;
            string? helpUrl = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(AdapterTypeDescriptor.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<Uri>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterTypeDescriptor.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AdapterTypeDescriptor.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AdapterTypeDescriptor.Version), StringComparison.OrdinalIgnoreCase)) {
                    version = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AdapterTypeDescriptor.Vendor), StringComparison.OrdinalIgnoreCase)) {
                    vendor = JsonSerializer.Deserialize<VendorInfo>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AdapterTypeDescriptor.HelpUrl), StringComparison.OrdinalIgnoreCase)) {
                    helpUrl = JsonSerializer.Deserialize<string>(ref reader, options);
                }
                else {
                    reader.Skip();
                }
            }

            return new AdapterTypeDescriptor(id, name, description, version, vendor, helpUrl);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, AdapterTypeDescriptor value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(AdapterTypeDescriptor.Id), value.Id.ToString(), options);
            WritePropertyValue(writer, nameof(AdapterTypeDescriptor.Name), value.Name, options);
            WritePropertyValue(writer, nameof(AdapterTypeDescriptor.Description), value.Description, options);
            WritePropertyValue(writer, nameof(AdapterTypeDescriptor.Version), value.Version, options);
            WritePropertyValue(writer, nameof(AdapterTypeDescriptor.Vendor), value.Vendor, options);
            WritePropertyValue(writer, nameof(AdapterTypeDescriptor.HelpUrl), value.HelpUrl, options);

            writer.WriteEndObject();
        }

    }

}
