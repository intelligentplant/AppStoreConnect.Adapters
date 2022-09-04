﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Json;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// An extended descriptor for an adapter, that includes information about the features that the 
    /// adapter has implemented.
    /// </summary>
    [JsonConverter(typeof(AdapterDescriptorExtendedConverter))]
    public class AdapterDescriptorExtended : AdapterDescriptor {

        /// <summary>
        /// The adapter type descriptor.
        /// </summary>
        public AdapterTypeDescriptor? TypeDescriptor { get; }

        /// <summary>
        /// The names of the implemented standard adapter features.
        /// </summary>
        public IEnumerable<string> Features { get; }

        /// <summary>
        /// The names of the implemented extension adapter features.
        /// </summary>
        public IEnumerable<string> Extensions { get; }

        /// <summary>
        /// Additional adapter properties.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptorExtended"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="name">
        ///   The adapter name.
        /// </param>
        /// <param name="description">
        ///   The adapter description.
        /// </param>
        /// <param name="features">
        ///   The standard features implemented by the adapter, typically the simple name of the 
        ///   feature type.
        /// </param>
        /// <param name="extensions">
        ///   The extension features implemented by the adapter, typically the namespace-qualified name 
        ///   of the feature type.
        /// </param>
        /// <param name="properties">
        ///   Additional adapter properties.
        /// </param>
        /// <param name="typeDescriptor">
        ///   The adapter type descriptor.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        public AdapterDescriptorExtended(
            string id, 
            string name, 
            string? description, 
            IEnumerable<string>? features, 
            IEnumerable<string>? extensions, 
            IEnumerable<AdapterProperty>? properties,
            AdapterTypeDescriptor? typeDescriptor
        ) : base(id, name, description) {
            
            Features = features?.ToArray() ?? Array.Empty<string>();
            Extensions = extensions?.ToArray() ?? Array.Empty<string>();
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
            TypeDescriptor = typeDescriptor;
        }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptorExtended"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="name">
        ///   The adapter name.
        /// </param>
        /// <param name="description">
        ///   The adapter description.
        /// </param>
        /// <param name="features">
        ///   The standard features implemented by the adapter, typically the simple name of the 
        ///   feature type.
        /// </param>
        /// <param name="extensions">
        ///   The extension features implemented by the adapter, typically the namespace-qualified name 
        ///   of the feature type.
        /// </param>
        /// <param name="properties">
        ///   Additional adapter properties.
        /// </param>
        /// <param name="typeDescriptor">
        ///   The adapter type descriptor.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        public static AdapterDescriptorExtended Create(string id, string name, string? description, IEnumerable<string>? features, IEnumerable<string>? extensions, IEnumerable<AdapterProperty>? properties, AdapterTypeDescriptor? typeDescriptor) {
            return new AdapterDescriptorExtended(id, name, description, features, extensions, properties, typeDescriptor);
        }

    }


    /// <summary>
    /// JSON converter for <see cref="AdapterDescriptorExtended"/>.
    /// </summary>
    internal class AdapterDescriptorExtendedConverter : AdapterJsonConverter<AdapterDescriptorExtended> {

        /// <inheritdoc/>
        public override AdapterDescriptorExtended Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null!;
            string name = null!;
            string description = null!;
            string[] features = null!;
            string[] extensions = null!;
            AdapterProperty[] properties = null!;
            AdapterTypeDescriptor typeDescriptor = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.Features), StringComparison.OrdinalIgnoreCase)) {
                    features = JsonSerializer.Deserialize<string[]>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.Extensions), StringComparison.OrdinalIgnoreCase)) {
                    extensions = JsonSerializer.Deserialize<string[]>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(AdapterDescriptorExtended.TypeDescriptor), StringComparison.OrdinalIgnoreCase)) {
                    typeDescriptor = JsonSerializer.Deserialize<AdapterTypeDescriptor>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return AdapterDescriptorExtended.Create(id, name, description, features, extensions, properties, typeDescriptor);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, AdapterDescriptorExtended value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.Id), value.Id, options);
            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.Name), value.Name, options);
            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.Description), value.Description, options);
            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.Features), value.Features, options);
            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.Extensions), value.Extensions, options);
            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.Properties), value.Properties, options);
            WritePropertyValue(writer, nameof(AdapterDescriptorExtended.TypeDescriptor), value.TypeDescriptor, options);

            writer.WriteEndObject();
        }

    }

}
