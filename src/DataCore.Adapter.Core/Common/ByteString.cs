using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// <see cref="ByteString"/> represents an immutable sequence of bytes.
    /// </summary>
    [JsonConverter(typeof(ByteStringConverter))]
    public readonly struct ByteString : IEquatable<ByteString> {

        /// <summary>
        /// An empty <see cref="ByteString"/> instance.
        /// </summary>
        public static ByteString Empty => default;

        /// <summary>
        /// The underlying byte sequence.
        /// </summary>
        public ReadOnlyMemory<byte> Bytes { get; }

        /// <summary>
        /// The length of the byte sequence.
        /// </summary>
        public int Length => Bytes.Length;

        /// <summary>
        /// Specifies if the byte sequence is empty.
        /// </summary>
        public bool IsEmpty => Bytes.IsEmpty;


        /// <summary>
        /// Creates a new <see cref="ByteString"/> instance.
        /// </summary>
        /// <param name="bytes">
        ///   The byte sequence.
        /// </param>
        public ByteString(ReadOnlyMemory<byte> bytes) {
            Bytes = bytes;
        }


        /// <summary>
        /// Creates a new <see cref="ByteString"/> instance.
        /// </summary>
        /// <param name="bytes">
        ///   The byte sequence.
        /// </param>
        public ByteString(byte[]? bytes) {
            Bytes = bytes ?? Array.Empty<byte>();
        }


        /// <summary>
        /// Creates a new <see cref="ByteString"/> instance.
        /// </summary>
        /// <param name="base64">
        ///   The base64-encoded byte sequence.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="base64"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="FormatException">
        ///   <paramref name="base64"/> is not a valid base64-encoded string.
        /// </exception>
        public ByteString(string base64) {
            if (base64 == null) {
                throw new ArgumentNullException(nameof(base64));
            }
            Bytes = Convert.FromBase64String(base64);
        }


        /// <summary>
        /// Tried to parse a base64-encoded string into a <see cref="ByteString"/> instance.
        /// </summary>
        /// <param name="base64">
        ///   The base64-encoded byte sequence.
        /// </param>
        /// <param name="result">
        ///   The parsed <see cref="ByteString"/> instance.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if parsing was successful; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryParse(string? base64, out ByteString result) {
            if (string.IsNullOrWhiteSpace(base64)) {
                result = default;
                return false;
            }

            try {
                result = new ByteString(base64);
                return true;
            }
            catch (FormatException) {
                result = default;
                return false;
            }
        }


        /// <summary>
        /// Converts the <see cref="ByteString"/> to a base64-encoded string.
        /// </summary>
        /// <returns>
        ///   The base64-encoded string.
        /// </returns>
        /// <remarks>
        ///   If <see cref="IsEmpty"/> is <see langword="true"/>, an empty string is returned.
        /// </remarks>
        public override string ToString() { 
            if (IsEmpty) {
                return string.Empty;
            }

            if (MemoryMarshal.TryGetArray(Bytes, out ArraySegment<byte> segment)) {
                return Convert.ToBase64String(segment.Array!, segment.Offset, segment.Count);
            }
            else {
                return Convert.ToBase64String(Bytes.ToArray());
            }
        }


        /// <inheritdoc/>
        public override int GetHashCode() {
            // We need to calculate a hash code that distributes evenly across a hash space, but
            // we don't want to have to iterate over the entire byte sequence to do so. Therefore,
            // we will compute a hash code based on the following criteria:
            //
            // * Length of the byte sequence
            // * First byte (non-empty byte sequences only)
            // * Middle byte (non-empty byte sequences only)
            // * Last byte (non-empty byte sequences only)

            if (IsEmpty) {
                return HashCode.Combine(0);
            }

            return HashCode.Combine(Length, Bytes.Span[0], Bytes.Span[(Bytes.Span.Length - 1) / 2], Bytes.Span[Bytes.Span.Length - 1]);
        }


        /// <inheritdoc/>
        public override bool Equals(object obj) {
            return obj is ByteString other && Equals(other);
        }


        /// <inheritdoc/>
        public bool Equals(ByteString other) {
            return Length == other.Length && Bytes.Span.SequenceEqual(other.Bytes.Span);
        }


        /// <inheritdoc/>
        public static implicit operator ByteString(ReadOnlyMemory<byte> bytes) => new ByteString(bytes);

        /// <inheritdoc/>
        public static implicit operator ByteString(byte[]? bytes) => new ByteString(bytes);

        /// <inheritdoc/>
        public static implicit operator ReadOnlyMemory<byte>(ByteString bytes) => bytes.Bytes;

        /// <inheritdoc/>
        public static implicit operator byte[](ByteString bytes) => bytes.Bytes.ToArray();

        /// <inheritdoc/>
        public static bool operator ==(ByteString left, ByteString right) => left.Equals(right);

        /// <inheritdoc/>
        public static bool operator !=(ByteString left, ByteString right) => !left.Equals(right);

    }


    /// <summary>
    /// JSON converter for <see cref="ByteString"/>.
    /// </summary>
    internal sealed class ByteStringConverter : JsonConverter<ByteString> {

        /// <inheritdoc/>
        public override ByteString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) {
                return ByteString.Empty;
            }

            if (reader.TokenType != JsonTokenType.String) {
                throw new JsonException();
            }

            return new ByteString(reader.GetBytesFromBase64());
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ByteString value, JsonSerializerOptions options) {
            if (value.IsEmpty) {
                writer.WriteNullValue();
                return;
            }
            writer.WriteBase64StringValue(value.Bytes.Span);
        }

    }

}
