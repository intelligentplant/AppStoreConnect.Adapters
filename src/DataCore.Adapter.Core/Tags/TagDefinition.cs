using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Describes a tag definition.
    /// </summary>
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
        [JsonConstructor]
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

}
