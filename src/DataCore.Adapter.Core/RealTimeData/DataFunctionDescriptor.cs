using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a data function that is supported when making a call for historical process data.
    /// </summary>
    public sealed class DataFunctionDescriptor {

        /// <summary>
        /// The function name.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The function description.
        /// </summary>
        public string Description { get; set; }


        /// <summary>
        /// Creates a new <see cref="DataFunctionDescriptor"/> object.
        /// </summary>
        /// <param name="name">
        ///   The function name.
        /// </param>
        /// <param name="description">
        ///   The function description.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public static DataFunctionDescriptor Create(string name, string description) {
            return new DataFunctionDescriptor() {
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                Description = description
            };
        }

    }
}
