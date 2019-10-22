using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a tag definition.
    /// </summary>
    public class TagDefinition : TagSummary {

        /// <summary>
        /// The tag's data type.
        /// </summary>
        public TagDataType DataType { get; set; }

        /// <summary>
        /// The discrete states for the tag. If <see cref="DataType"/> is not <see cref="TagDataType.State"/>, 
        /// this property will be <see langword="null"/>.
        /// </summary>
        public IEnumerable<DigitalState> States { get; set; }

        /// <summary>
        /// Bespoke tag properties.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; set; }

        /// <summary>
        /// Labels associated with the tag.
        /// </summary>
        public IEnumerable<string> Labels { get; set; }


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
        ///   <see cref="TagDataType.State"/>.
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
        public static TagDefinition Create(string id, string name, string description, string units, TagDataType dataType, IEnumerable<DigitalState> states, IEnumerable<AdapterProperty> properties, IEnumerable<string> labels) {
            return new TagDefinition() {
                Id = id ?? throw new ArgumentNullException(nameof(id)),
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                Description = description ?? string.Empty,
                Units = units ?? string.Empty,
                DataType = dataType,
                States = dataType != TagDataType.State
                    ? null
                    : states?.ToArray() ?? Array.Empty<DigitalState>(),
                Labels = labels?.ToArray() ?? Array.Empty<string>()
            };
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

            return Create(
                tag.Id,
                tag.Name,
                tag.Description,
                tag.Units,
                tag.DataType,
                tag.States,
                tag.Properties,
                tag.Labels
            );
        }

    }
}
