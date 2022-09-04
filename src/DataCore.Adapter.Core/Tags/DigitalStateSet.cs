using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.Json;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Describes a collection of discrete states.
    /// </summary>
    [JsonConverter(typeof(DigitalStateSetConverter))]
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


    /// <summary>
    /// JSON converter for <see cref="DigitalStateSet"/>.
    /// </summary>
    internal class DigitalStateSetConverter : AdapterJsonConverter<DigitalStateSet> {

        /// <inheritdoc/>
        public override DigitalStateSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                ThrowInvalidJsonError();
            }

            string id = null!;
            string name = null!;
            DigitalState[] states = null!;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }

                var propertyName = reader.GetString();
                if (!reader.Read()) {
                    ThrowInvalidJsonError();
                }

                if (string.Equals(propertyName, nameof(DigitalStateSet.Id), StringComparison.OrdinalIgnoreCase)) {
                    id = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(DigitalStateSet.Name), StringComparison.OrdinalIgnoreCase)) {
                    name = JsonSerializer.Deserialize<string>(ref reader, options)!;
                }
                else if (string.Equals(propertyName, nameof(DigitalStateSet.States), StringComparison.OrdinalIgnoreCase)) {
                    states = JsonSerializer.Deserialize<DigitalState[]>(ref reader, options)!;
                }
                else {
                    reader.Skip();
                }
            }

            return DigitalStateSet.Create(id, name, states);
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DigitalStateSet value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            WritePropertyValue(writer, nameof(DigitalStateSet.Id), value.Id, options);
            WritePropertyValue(writer, nameof(DigitalStateSet.Name), value.Name, options);
            WritePropertyValue(writer, nameof(DigitalStateSet.States), value.States, options);
            writer.WriteEndObject();
        }

    }

}
