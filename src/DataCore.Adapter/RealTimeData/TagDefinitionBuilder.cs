using System;
using System.Collections.Generic;
using System.Linq;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Helper class for constructing <see cref="TagDefinition"/> objects using a fluent interface.
    /// </summary>
    public class TagDefinitionBuilder {

        /// <summary>
        /// The tag ID.
        /// </summary>
        private string? _id;

        /// <summary>
        /// The tag name.
        /// </summary>
        private string? _name;

        /// <summary>
        /// The tag description.
        /// </summary>
        private string? _description;

        /// <summary>
        /// The tag units.
        /// </summary>
        private string? _units;

        /// <summary>
        /// The tag data type.
        /// </summary>
        private VariantType _dataType;

        /// <summary>
        /// The digital states for the tag.
        /// </summary>
        private readonly List<DigitalState> _states = new List<DigitalState>();

        /// <summary>
        /// The bespoke tag properties.
        /// </summary>
        private readonly List<AdapterProperty> _properties = new List<AdapterProperty>();

        /// <summary>
        /// The tag labels.
        /// </summary>
        private readonly List<string> _labels = new List<string>();


        /// <summary>
        /// Creates a new <see cref="TagDefinitionBuilder"/> object.
        /// </summary>
        private TagDefinitionBuilder() { }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionBuilder"/> object that is initialised from an 
        /// existing tag definition.
        /// </summary>
        /// <param name="existing">
        ///   The existing tag definition.
        /// </param>
        private TagDefinitionBuilder(TagDefinition existing) {
            WithId(existing.Id);
            WithName(existing.Name);
            WithDescription(existing.Description);
            WithUnits(existing.Units);
            WithDataType(existing.DataType);
            WithDigitalStates(existing.States);
            WithProperties(existing.Properties);
            WithLabels(existing.Labels);
        }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionBuilder"/> object.
        /// </summary>
        /// <returns>
        ///   A new <see cref="TagDefinitionBuilder"/> object.
        /// </returns>
        public static TagDefinitionBuilder Create() {
            return new TagDefinitionBuilder();
        }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionBuilder"/> object that is initialised from an 
        /// existing tag definition.
        /// </summary>
        /// <param name="existing">
        ///   The existing tag definition.
        /// </param>
        /// <returns>
        ///   A new <see cref="TagDefinitionBuilder"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="existing"/> is <see langword="null"/>.
        /// </exception>
        public static TagDefinitionBuilder CreateFromExisting(TagDefinition existing) {
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }

            return new TagDefinitionBuilder(existing);
        }


        /// <summary>
        /// Creates a <see cref="TagDefinition"/> using the configured settings.
        /// </summary>
        /// <returns>
        ///   A new <see cref="TagDefinition"/> object.
        /// </returns>
        public TagDefinition Build() {
            return new TagDefinition(_id!, _name!, _description, _units, _dataType, _states, _properties, _labels);
        }


        /// <summary>
        /// Updates the ID for the tag.
        /// </summary>
        /// <param name="id">
        ///   The ID.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithId(string id) {
            _id = id;
            return this;
        }


        /// <summary>
        /// Updates the name for the tag.
        /// </summary>
        /// <param name="name">
        ///   The name.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithName(string name) {
            _name = name;
            return this;
        }


        /// <summary>
        /// Updates the description for the tag.
        /// </summary>
        /// <param name="description">
        ///   The description.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithDescription(string? description) {
            _description = description;
            return this;
        }


        /// <summary>
        /// Updates the units for the tag.
        /// </summary>
        /// <param name="units">
        ///   The units.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithUnits(string? units) {
            _units = units;
            return this;
        }


        /// <summary>
        /// Updates the data type for the tag.
        /// </summary>
        /// <param name="dataType">
        ///   The data type.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithDataType(VariantType dataType) {
            _dataType = dataType;
            return this;
        }


        /// <summary>
        /// Adds digital states to the tag.
        /// </summary>
        /// <param name="states">
        ///   The digital states.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithDigitalStates(params DigitalState[] states) {
            return WithDigitalStates((IEnumerable<DigitalState>) states);
        }


        /// <summary>
        /// Adds digital states to the tag.
        /// </summary>
        /// <param name="states">
        ///   The digital states.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithDigitalStates(IEnumerable<DigitalState> states) {
            if (states != null) {
                _states.AddRange(states.Where(x => x != null).Select(x => new DigitalState(x.Name, x.Value)));
            }
            return this;
        }


        /// <summary>
        /// Adds digital states to the tag from a <see cref="DigitalStateSet"/>.
        /// </summary>
        /// <param name="stateSet">
        ///   The state set containing the digital state definitions to add.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithDigitalStates(DigitalStateSet stateSet) {
            return WithDigitalStates(stateSet?.States!);
        }


        /// <summary>
        /// Removes all digital states from the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder ClearDigitalStates() {
            _states.Clear();
            return this;
        }


        /// <summary>
        /// Adds bespoke properties to the tag.
        /// </summary>
        /// <param name="properties">
        ///   The properties.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithProperties(params AdapterProperty[] properties) {
            return WithProperties((IEnumerable<AdapterProperty>) properties);
        }


        /// <summary>
        /// Adds bespoke properties to the tag.
        /// </summary>
        /// <param name="properties">
        ///   The properties.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithProperties(IEnumerable<AdapterProperty> properties) {
            if (properties != null) {
                _properties.AddRange(properties.Where(x => x != null).Select(x => new AdapterProperty(x.Name, x.Value, x.Description)));
            }
            return this;
        }


        /// <summary>
        /// Removes all bespoke properties from the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder ClearProperties() {
            _properties.Clear();
            return this;
        }


        /// <summary>
        /// Adds labels to the tag.
        /// </summary>
        /// <param name="labels">
        ///   The labels.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithLabels(params string[] labels) {
            return WithLabels((IEnumerable<string>) labels);
        }


        /// <summary>
        /// Adds labels to the tag.
        /// </summary>
        /// <param name="labels">
        ///   The labels.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithLabels(IEnumerable<string> labels) {
            if (labels != null) {
                _labels.AddRange(labels.Where(x => !string.IsNullOrWhiteSpace(x)));
            }
            return this;
        }


        /// <summary>
        /// Removes all labels from the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder ClearLabels() {
            _labels.Clear();
            return this;
        }

    }
}
