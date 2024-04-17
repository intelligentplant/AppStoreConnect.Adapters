using System;
using System.Globalization;
using System.Text;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// Describes a key or a prefix in a <see cref="IKeyValueStore"/>.
    /// </summary>
    public struct KVKey : IEquatable<KVKey> {

        /// <summary>
        /// A <see cref="KVKey"/> that represents an empty prefix.
        /// </summary>
        public static KVKey Empty => default;

        /// <summary>
        /// The value of the key.
        /// </summary>
        public byte[] Value { get; }

        /// <summary>
        /// The length of the key, in bytes.
        /// </summary>
        public int Length => Value?.Length ?? 0;


        /// <summary>
        /// Creates a new <see cref="KVKey"/>.
        /// </summary>
        /// <param name="value">
        ///   The value of the key.
        /// </param>
        public KVKey(byte[]? value) { 
            Value = value!;
        }


        /// <inheritdoc/>
        public override int GetHashCode() {
            return HashCode.Combine(Value);
        }


        /// <inheritdoc/>
        public override bool Equals(object obj) {
            return obj is KVKey other 
                ? Equals(other) 
                : false;
        }


        /// <inheritdoc/>
        public bool Equals(KVKey other) {
            if (Value == null && other.Value == null) {
                return true;
            }

            if (Value == null && other.Value != null) {
                return false;
            }

            if (Value != null && other.Value == null) {
                return false;
            }

            if (Value!.Length != other.Value!.Length) {
                return false;
            }

            for (var i = 0; i < Value.Length; i++) { 
                if (Value[i] != other.Value[i]) {
                    return false;
                }
            }

            return true;
        }


        /// <inheritdoc/>
        public override string ToString() {
            if (Length == 0) {
                return string.Empty;
            }

            try {
                return Encoding.UTF8.GetString(Value);
            }
            catch {
                return BitConverter.ToString(Value);
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static bool operator ==(KVKey left, KVKey right) => left.Equals(right);
        public static bool operator !=(KVKey left, KVKey right) => !left.Equals(right);

        public static implicit operator KVKey(byte[] value) => new KVKey(value);
        public static implicit operator KVKey(string? value) => new KVKey(Encoding.UTF8.GetBytes(value ?? string.Empty));
        public static implicit operator KVKey(DateTime value) => new KVKey(Encoding.UTF8.GetBytes(value.ToString("O", CultureInfo.InvariantCulture)));
        public static implicit operator KVKey(TimeSpan value) => new KVKey(Encoding.UTF8.GetBytes(value.ToString("C", CultureInfo.InvariantCulture)));
        public static implicit operator KVKey(byte value) => new KVKey(new[] { value });
        public static implicit operator KVKey(bool value) => new KVKey(BitConverter.GetBytes(value));
        public static implicit operator KVKey(char value) => new KVKey(BitConverter.GetBytes(value));
        public static implicit operator KVKey(double value) => new KVKey(BitConverter.GetBytes(value));
        public static implicit operator KVKey(float value) => new KVKey(BitConverter.GetBytes(value));
        public static implicit operator KVKey(int value) => new KVKey(BitConverter.GetBytes(value));
        public static implicit operator KVKey(long value) => new KVKey(BitConverter.GetBytes(value));
        public static implicit operator KVKey(short value) => new KVKey(BitConverter.GetBytes(value));
        public static implicit operator KVKey(ushort value) => new KVKey(BitConverter.GetBytes(value));
        public static implicit operator KVKey(uint value) => new KVKey(BitConverter.GetBytes(value));
        public static implicit operator KVKey(ulong value) => new KVKey(BitConverter.GetBytes(value));
        public static implicit operator byte[](KVKey value) => value.Value;
        public static implicit operator string?(KVKey value) => value.Length == 0 ? null : value.ToString();

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    }
}
