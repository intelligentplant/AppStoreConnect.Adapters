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
        public static Variant Null { get; } = new Variant(null);


        /// <summary>
        /// The value.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// The variant type.
        /// </summary>
        public VariantType Type { get; }


        /// <summary>
        /// Creates a new <see cref="Variant"/> object.
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
        public Variant(object value) {
            Value = value;
            Type = value == null
                ? VariantType.Null 
                : value.GetType().GetVariantType();
        }


        /// <inheritdoc/>
        public override string ToString() {
            return Value == null
                ? "{null}"
                : Value.ToString();
        }

    }
}
