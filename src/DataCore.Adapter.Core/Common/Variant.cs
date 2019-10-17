using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes a variant value.
    /// </summary>
    public struct Variant {

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

    }
}
