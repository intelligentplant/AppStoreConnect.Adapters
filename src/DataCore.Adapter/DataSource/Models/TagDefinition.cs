﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DataCore.Adapter.DataSource.Models {

    /// <summary>
    /// Describes a tag definition.
    /// </summary>
    public sealed class TagDefinition {

        /// <summary>
        /// The unique identifier for the tag.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The tag name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The tag description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The tag units.
        /// </summary>
        public string Units { get; }

        /// <summary>
        /// The tag's data type.
        /// </summary>
        public TagDataType DataType { get; }

        /// <summary>
        /// The discrete states for the tag. If <see cref="DataType"/> is not <see cref="TagDataType.State"/>, 
        /// this property will be <see langword="null"/>.
        /// </summary>
        public IReadOnlyDictionary<string, int> States { get; }

        /// <summary>
        /// Bespoke tag properties.
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties { get; }


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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public TagDefinition(string id, string name, string description, string units, TagDataType dataType, IDictionary<string, int> states, IDictionary<string, string> properties) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Units = units;
            DataType = dataType;
            States = dataType != TagDataType.State
                ? null
                : states == null
                    ? new ReadOnlyDictionary<string, int>(new Dictionary<string, int>())
                    : new ReadOnlyDictionary<string, int>(states);
            Properties = properties == null
                ? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
                : new ReadOnlyDictionary<string, string>(properties);
        }

    }
}
