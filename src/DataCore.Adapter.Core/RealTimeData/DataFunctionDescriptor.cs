using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a data function that is supported when making a call for historical process data.
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public DataFunctionDescriptor(string id, string name, string description) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public static DataFunctionDescriptor Create(string id, string name, string description) {
            return new DataFunctionDescriptor(id, name, description);
        }

    }
}
