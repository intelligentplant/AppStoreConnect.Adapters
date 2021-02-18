using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Describes a collection of discrete states.
    /// </summary>
    public class DigitalStateSet {

        /// <summary>
        /// The ID of the state set.
        /// </summary>
        [Required]
        public string Id { get; }

        /// <summary>
        /// The name of the set.
        /// </summary>
        [Required]
        public string Name { get; }

        /// <summary>
        /// The states.
        /// </summary>
        public IEnumerable<DigitalState> States { get; }


        /// <summary>
        /// Creates a new <see cref="DigitalStateSet"/>.
        /// </summary>
        /// <param name="id">
        ///   The set ID.
        /// </param>
        /// <param name="name">
        ///   The set display name.
        /// </param>
        /// <param name="states">
        ///   The states.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        public DigitalStateSet(string id, string name, IEnumerable<DigitalState>? states) {
            Id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(id))
                : id;
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException(SharedResources.Error_NameIsRequired, nameof(name))
                : name;
            States = states?.ToArray() ?? Array.Empty<DigitalState>();
        }


        /// <summary>
        /// Creates a new <see cref="DigitalStateSet"/>.
        /// </summary>
        /// <param name="id">
        ///   The set ID.
        /// </param>
        /// <param name="name">
        ///   The set display name.
        /// </param>
        /// <param name="states">
        ///   The states.
        /// </param>
        /// <returns>
        ///   A new <see cref="DigitalStateSet"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        public static DigitalStateSet Create(string id, string name, IEnumerable<DigitalState>? states) {
            return new DigitalStateSet(
                id,
                name, 
                states
            );
        }

    }
}
