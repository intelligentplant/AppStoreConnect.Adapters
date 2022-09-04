using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Json;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Describes a digital state associated with a tag.
    /// </summary>
    [JsonConverter(typeof(DigitalStateConverter))]
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
        public DigitalState(string name, int value) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
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


    /// <summary>
    /// JSON converter for <see cref="DigitalState"/>.
    /// </summary>
    internal class DigitalStateConverter : AdapterJsonConverter<DigitalState> {


        /// <inheritdoc/>
        public override DigitalState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string name = null!;
            int value = -1;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(DigitalState.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(DigitalState.Value), StringComparison.OrdinalIgnoreCase)) {
                    value = JsonSerializer.Deserialize<int>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return DigitalState.Create(name, value);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DigitalState value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(DigitalState.Name), value.Name, options);
            WritePropertyValue(writer, nameof(DigitalState.Value), value.Value, options);
            writer.WriteEndObject();
        }

    }

}
