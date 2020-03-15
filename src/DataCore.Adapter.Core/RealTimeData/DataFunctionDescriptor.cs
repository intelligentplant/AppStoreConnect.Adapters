using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a data function that is supported when making a call for processed historical data.
    /// </summary>
    public sealed class DataFunctionDescriptor {

        /// <summary>
        /// The function ID.
        /// </summary>
        [Required]
        public string Id { get; }

        /// <summary>
        /// The function display name.
        /// </summary>
        [Required]
        public string Name { get; }

        /// <summary>
        /// The function description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Bespoke properties associated with the data function.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; }


        /// <summary>
        /// Creates a new <see cref="DataFunctionDescriptor"/> object.
        /// </summary>
        /// <param name="id">
        ///   The function ID.
        /// </param>
        /// <param name="name">
        ///   The function name.
        /// </param>
        /// <param name="description">
        ///   The function description.
        /// </param>
        /// <param name="properties">
        ///   Additional properties associated with the function.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        public DataFunctionDescriptor(string id, string name, string description, IEnumerable<AdapterProperty> properties) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = string.IsNullOrWhiteSpace(name) ? id : name;
            Description = description;
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }


        /// <summary>
        /// Creates a new <see cref="DataFunctionDescriptor"/> object.
        /// </summary>
        /// <param name="id">
        ///   The function ID.
        /// </param>
        /// <param name="name">
        ///   The function name.
        /// </param>
        /// <param name="description">
        ///   The function description.
        /// </param>
        /// <param name="properties">
        ///   Additional properties associated with the function.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        public static DataFunctionDescriptor Create(string id, string name = null, string description = null, IEnumerable<AdapterProperty> properties = null) {
            return new DataFunctionDescriptor(id, name ?? id, description, properties);
        }

    }
}
