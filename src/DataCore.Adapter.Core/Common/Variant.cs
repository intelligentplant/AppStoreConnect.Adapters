using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes a variant value.
    /// </summary>
    public class Variant {

        /// <summary>
        /// The value.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The variant type.
        /// </summary>
        public VariantType Type { get; set; }


        /// <summary>
        /// Creates a new <see cref="Variant"/> object.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A new <see cref="Variant"/> object.
        /// </returns>
        public static Variant Create(object value) {
            if (value == null) {
                return new Variant() { 
                    Value = null,
                    Type = VariantType.Null
                };
            }

            if (value is Variant v) {
                return Create(v.Value, v.Type);
            }

            return Create(value, value.GetType().GetVariantType());
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> object.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <param name="type">
        ///   The value type.
        /// </param>
        /// <returns>
        ///   A new <see cref="Variant"/> object.
        /// </returns>
        public static Variant Create(object value, VariantType type) {
            return new Variant() {
                Value = value,
                Type = type
            };
        }


        /// <inheritdoc/>
        public override string ToString() {
            return Value == null
                ? "{null}"
                : Value.ToString();
        }

    }
}
