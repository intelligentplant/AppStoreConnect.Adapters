using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes a data function that is supported when making a call for historical process data.
    /// </summary>
    public sealed class DataFunctionDescriptor {

        /// <summary>
        /// The function name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The function description.
        /// </summary>
        public string Description { get; }


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
        public DataFunctionDescriptor(string name, string description) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
        }

    }
}
