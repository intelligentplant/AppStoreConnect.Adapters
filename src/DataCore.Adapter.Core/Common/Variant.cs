using System;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes a variant value.
    /// </summary>
    public struct Variant : IEquatable<Variant>, IFormattable {

        /// <summary>
        /// Null variant.
        /// </summary>
        public static Variant Null { get; } = FromValue(null);


        /// <summary>
        /// The value.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// The variant type.
        /// </summary>
        public VariantType Type { get; }


        /// <summary>
        /// Creates a new <see cref="Variant"/> object. It is preferable to call <see cref="FromValue"/> 
        /// instead of calling the constructor directly, as this allows the <see cref="Type"/> of 
        /// the variant to be inferred from the value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <param name="type">
        ///   The variant type.
        /// </param>
        public Variant(object value, VariantType type) {
            Value = value;
            Type = type;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> object that infers the <see cref="VariantType"/> 
        /// from the value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <param name="typeOverride">
        ///   When a value is provided, the <see cref="Type"/> of the resulting variant is set to 
        ///   this value instead of being inferred from the <paramref name="value"/>.
        /// </param>
        public static Variant FromValue(object value, VariantType? typeOverride = null) {
            if (value is Variant v) {
                return new Variant(v.Value, v.Type);
            }

            return new Variant(
                value,
                typeOverride.HasValue
                    ? typeOverride.Value
                    : value == null
                        ? VariantType.Null
                        : value.GetType().GetVariantType()
            );
        }


        /// <inheritdoc/>
        public override string ToString() {
            return Value?.ToString() ?? "{null}";
        }


        /// <inheritdoc/>
        public string ToString(string format, IFormatProvider formatProvider) {
            return (Value is IFormattable formattable)
                ? formattable.ToString(format, formatProvider)
                : ToString();
        }


        /// <inheritdoc/>
        public override int GetHashCode() {
            unchecked {
                // Choose large primes to avoid hashing collisions
                const int HashingBase = (int) 2166136261;
                const int HashingMultiplier = 16777619;

                var hash = HashingBase;
                hash = (hash * HashingMultiplier) ^ Type.GetHashCode();
                hash = (hash * HashingMultiplier) ^ (ReferenceEquals(Value, null) ? 0 : Value.GetHashCode());
                return hash;
            }
        }


        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is Variant v)) {
                return false;
            }

            return Equals(v);
        }


        /// <inheritdoc/>
        public bool Equals(Variant other) {
            return other.Type == Type && Equals(other.Value, Value);
        }


        /// <inheritdoc/>
        public static bool operator ==(Variant left, Variant right) {
            return left.Equals(right);
        }


        /// <inheritdoc/>
        public static bool operator !=(Variant left, Variant right) {
            return !left.Equals(right);
        }

    }
}
