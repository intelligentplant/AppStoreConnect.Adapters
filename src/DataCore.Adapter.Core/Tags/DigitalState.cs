using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Describes a digital state associated with a tag.
    /// </summary>
    public class DigitalState {

        /// <summary>
        /// The state name.
        /// </summary>
        [Required]
        public string Name { get; }

        /// <summary>
        /// The state value.
        /// </summary>
        public int Value { get; }


        /// <summary>
        /// Creates a new <see cref="DigitalState"/> object.
        /// </summary>
        /// <param name="name">
        ///   The state name.
        /// </param>
        /// <param name="value">
        ///   The state value.
        /// </param>
        /// <returns>
        ///   A new <see cref="DigitalState"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        [JsonConstructor]
        public DigitalState(string name, int value) {
            if (name == null) {
                throw new ArgumentNullException(nameof(name));
            }
            Name = string.Intern(name);
            Value = value;
        }


        /// <summary>
        /// Creates a new <see cref="DigitalState"/> object.
        /// </summary>
        /// <param name="name">
        ///   The state name.
        /// </param>
        /// <param name="value">
        ///   The state value.
        /// </param>
        /// <returns>
        ///   A new <see cref="DigitalState"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public static DigitalState Create(string name, int value) {
            return new DigitalState(name, value);
        }


        /// <summary>
        /// Creates a new <see cref="DigitalState"/> obejct that is a copy of an existing 
        /// instance.
        /// </summary>
        /// <param name="state">
        ///   The state to copy.
        /// </param>
        /// <returns>
        ///   A copy of the existing state
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="state"/> is <see langword="null"/>.
        /// </exception>
        public static DigitalState FromExisting(DigitalState state) {
            if (state == null) {
                throw new ArgumentNullException(nameof(state));
            }

            return Create(state.Name, state.Value);
        }

    }

}
