using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes a variant value.
    /// </summary>
    public class Variant {

        /// <summary>
        /// Maps from type to variant type.
        /// </summary>
        private static readonly Dictionary<Type, VariantType> s_variantTypeMap = new Dictionary<Type, VariantType>() {
            { typeof(bool), VariantType.Boolean },
            { typeof(byte), VariantType.Byte },
            { typeof(DateTime), VariantType.DateTime },
            { typeof(double), VariantType.Double },
            { typeof(float), VariantType.Float },
            { typeof(short), VariantType.Int16 },
            { typeof(int), VariantType.Int32 },
            { typeof(long), VariantType.Int64 },
            { typeof(sbyte), VariantType.SByte },
            { typeof(string), VariantType.String },
            { typeof(TimeSpan), VariantType.TimeSpan },
            { typeof(ushort), VariantType.UInt16 },
            { typeof(uint), VariantType.UInt32 },
            { typeof(ulong), VariantType.UInt64 }
        };

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

            return Create(value, GetVariantType(value.GetType()));
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


        /// <summary>
        /// Gets the variant type for the specified CLR type.
        /// </summary>
        /// <param name="type">
        ///   The CLR type.
        /// </param>
        /// <returns>
        ///   The corresponding variant type.
        /// </returns>
        private static VariantType GetVariantType(Type type) {
            if (type == null) {
                return VariantType.Unknown;
            }

            if (s_variantTypeMap.TryGetValue(type, out var variantType)) {
                return variantType;
            }

            return type.IsValueType
                ? VariantType.Unknown
                : VariantType.Object;
        }


        public T GetValueOrDefault<T>() {
            return GetValueOrDefault<T>(default);
        }


        public T GetValueOrDefault<T>(T defaultValue) {
            return (Value is T val)
                ? (T) Value
                : defaultValue;
        }

    }
}
