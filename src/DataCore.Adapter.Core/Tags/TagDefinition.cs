using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Describes a tag definition.
    /// </summary>
    [JsonConverter(typeof(TagDefinitionConverter))]
    public class TagDefinition : TagSummary {

        /// <summary>
        /// The discrete states for the tag. If <see cref="TagSummary.DataType"/> is not 
        /// <see cref="VariantType.Int32"/>, this property will be <see langword="null"/>.
        /// </summary>
        public IEnumerable<DigitalState> States { get; }

        /// <summary>
        /// The adapter features that can be used to read data from or write data to this tag.
        /// </summary>
        public IEnumerable<Uri> SupportedFeatures { get; }

        /// <summary>
        /// Bespoke tag properties.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; }

        /// <summary>
        /// Labels associated with the tag.
        /// </summary>
        public IEnumerable<string> Labels { get; }


        /// <summary>
        /// Creates a new <see cref="TagDefinition"/> object.
        /// </summary>
        /// <param name="id">
        ///   The tag ID.
        /// </param>
        /// <param name="name">
        ///   The tag name.
        /// </param>
        /// <param name="description">
        ///   The tag description.
        /// </param>
        /// <param name="units">
        ///   The tag units.
        /// </param>
        /// <param name="dataType">
        ///   The data type for the tag.
        /// </param>
        /// <param name="states">
        ///   The discrete states for the tag. Ignored if <paramref name="dataType"/> is not 
        ///   <see cref="VariantType.Int32"/>.
        /// </param>
        /// <param name="supportedFeatures">
        ///   The adapter features that can be used to read data from or write data to this tag.
        /// </param>
        /// <param name="properties">
        ///   Additional tag properties.
        /// </param>
        /// <param name="labels">
        ///   Labels associated with the tag.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public TagDefinition(
            string id, 
            string name, 
            string? description, 
            string? units, 
            VariantType dataType, 
            IEnumerable<DigitalState>? states, 
            IEnumerable<Uri>? supportedFeatures,
            IEnumerable<AdapterProperty>? properties, 
            IEnumerable<string>? labels
        ) : base(id, name, description, units, dataType) {
            States = dataType != VariantType.Int32
                ? Array.Empty<DigitalState>()
                : states?.ToArray() ?? Array.Empty<DigitalState>();
            SupportedFeatures = supportedFeatures?.Where(x => x != null)?.ToArray() ?? Array.Empty<Uri>();
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
            Labels = labels?.ToArray() ?? Array.Empty<string>();
        }


        /// <summary>
        /// Creates a new <see cref="TagDefinition"/> object.
        /// </summary>
        /// <param name="id">
        ///   The tag ID.
        /// </param>
        /// <param name="name">
        ///   The tag name.
        /// </param>
        /// <param name="description">
        ///   The tag description.
        /// </param>
        /// <param name="units">
        ///   The tag units.
        /// </param>
        /// <param name="dataType">
        ///   The data type for the tag.
        /// </param>
        /// <param name="states">
        ///   The discrete states for the tag. Ignored if <paramref name="dataType"/> is not 
        ///   <see cref="VariantType.Int32"/>.
        /// </param>
        /// <param name="supportedFeatures">
        ///   The adapter features that can be used to read data from or write data to this tag.
        /// </param>
        /// <param name="properties">
        ///   Additional tag properties.
        /// </param>
        /// <param name="labels">
        ///   Labels associated with the tag.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        [Obsolete("This method will be removed in a future version. Use the constructor directly or use TagDefinitionBuilder in DataCore.Adapter.dll.", false)]
        public static TagDefinition Create(string id, string name, string? description, string? units, VariantType dataType, IEnumerable<DigitalState>? states, IEnumerable<Uri>? supportedFeatures, IEnumerable<AdapterProperty>? properties, IEnumerable<string>? labels) {
            return new TagDefinition(id, name, description, units, dataType, states, supportedFeatures, properties, labels);
        }


        /// <summary>
        /// Creates a new <see cref="TagDefinition"/> object from an existing instance.
        /// </summary>
        /// <param name="tag">
        ///   The tag to clone.
        /// </param>
        /// <returns>
        ///   A new <see cref="TagDefinition"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        public static TagDefinition FromExisting(TagDefinition tag) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            return new TagDefinition(
                tag.Id,
                tag.Name,
                tag.Description,
                tag.Units,
                tag.DataType,
                tag.States.Where(x => x != null).Select(x => new DigitalState(x.Name, x.Value)),
                tag.SupportedFeatures,
                tag.Properties.Where(x => x != null).Select(x => new AdapterProperty(x.Name, x.Value, x.Description)),
                tag.Labels
            );
        }

    }


    /// <summary>
    /// JSON converter for <see cref="TagDefinition"/>.
    /// </summary>
    internal class TagDefinitionConverter : AdapterJsonConverter<TagDefinition> {

        /// <inheritdoc/>
        public override TagDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null!;
            string name = null!;
            string description = null!;
            string units = null!;
            VariantType dataType = VariantType.Unknown;
            DigitalState[] states = null!;
            Uri[] supportedFeatures = null!;
            AdapterProperty[] properties = null!;
            string[] labels = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(TagDefinition.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.Description), StringComparison.OrdinalIgnoreCase)) {
                    description = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.Units), StringComparison.OrdinalIgnoreCase)) {
                    units = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.DataType), StringComparison.OrdinalIgnoreCase)) {
                    dataType = JsonSerializer.Deserialize<VariantType>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.States), StringComparison.OrdinalIgnoreCase)) {
                    states = JsonSerializer.Deserialize<DigitalState[]>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.SupportedFeatures), StringComparison.OrdinalIgnoreCase)) {
                    supportedFeatures = JsonSerializer.Deserialize<Uri[]>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.Properties), StringComparison.OrdinalIgnoreCase)) {
                    properties = JsonSerializer.Deserialize<AdapterProperty[]>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(TagDefinition.Labels), StringComparison.OrdinalIgnoreCase)) {
                    labels = JsonSerializer.Deserialize<string[]>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return new TagDefinition(id, name, description, units, dataType, states, supportedFeatures, properties, labels);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TagDefinition value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(TagDefinition.Id), value.Id, options);
            WritePropertyValue(writer, nameof(TagDefinition.Name), value.Name, options);
            WritePropertyValue(writer, nameof(TagDefinition.Description), value.Description, options);
            WritePropertyValue(writer, nameof(TagDefinition.Units), value.Units, options);
            WritePropertyValue(writer, nameof(TagDefinition.DataType), value.DataType, options);
            WritePropertyValue(writer, nameof(TagDefinition.States), value.States, options);
            WritePropertyValue(writer, nameof(TagDefinition.SupportedFeatures), value.SupportedFeatures, options);
            WritePropertyValue(writer, nameof(TagDefinition.Properties), value.Properties, options);
            WritePropertyValue(writer, nameof(TagDefinition.Labels), value.Labels, options);
            writer.WriteEndObject();
        }

    }

}
