using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Extensions for <see cref="Variant"/>.
    /// </summary>
    public static class VariantExtensions {

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
            { typeof(ulong), VariantType.UInt64 },
            { typeof(Uri), VariantType.Url }
        };


        /// <summary>
        /// Tests if a variant contains a null value.
        /// </summary>
        /// <param name="variant">
        ///   The variant.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="Type"/> is <see cref="VariantType.Null"/> 
        ///   or the <see cref="Variant.Value"/> is <see langword="null"/>, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool IsNull(this Variant variant) {
            return (variant.Type == VariantType.Null) || (variant.Value == null);
        }


        /// <summary>
        /// Tests if the variant has the specified type.
        /// </summary>
        /// <param name="variant">
        ///   The variant.
        /// </param>
        /// <param name="type">
        ///   The variant type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the variant has the specified <paramref name="type"/>, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool Is(this Variant variant, VariantType type) {
            return variant.Type == type;
        }


        /// <summary>
        /// Tests if the variant is a numeric type.
        /// </summary>
        /// <param name="variant">
        ///   The variant.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the variant's <see cref="Variant.Type"/> indicates that 
        ///   its value is numeric, or <see langword="false"/> otherwise.
        /// </returns>
        /// <remarks>
        /// 
        /// The following types are considered to be numeric:
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <description><see cref="VariantType.Boolean"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Byte"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Double"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Float"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Int16"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Int32"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Int64"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.SByte"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.UInt16"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.UInt32"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.UInt64"/></description>
        ///   </item>
        /// </list>
        /// 
        /// All other types are considered to be non-numeric.
        /// 
        /// </remarks>
        public static bool IsNumericType(this Variant variant) {
            return variant.Type.IsNumericType();
        }


        /// <summary>
        /// Tests if the variant type is a numeric type.
        /// </summary>
        /// <param name="variantType">
        ///   The variant type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the variant's <see cref="Variant.Type"/> indicates that 
        ///   its value is numeric, or <see langword="false"/> otherwise.
        /// </returns>
        /// <remarks>
        /// 
        /// The following types are considered to be numeric:
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <description><see cref="VariantType.Boolean"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Byte"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Double"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Float"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Int16"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Int32"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Int64"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.SByte"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.UInt16"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.UInt32"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.UInt64"/></description>
        ///   </item>
        /// </list>
        /// 
        /// All other types are considered to be non-numeric.
        /// 
        /// </remarks>
        public static bool IsNumericType(this VariantType variantType) {
            switch (variantType) {
                case VariantType.Boolean:
                case VariantType.Byte:
                case VariantType.Double:
                case VariantType.Float:
                case VariantType.Int16:
                case VariantType.Int32:
                case VariantType.Int64:
                case VariantType.SByte:
                case VariantType.UInt16:
                case VariantType.UInt32:
                case VariantType.UInt64:
                    return true;
                default:
                    return false;
            }
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
        internal static VariantType GetVariantType(this Type type) {
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


        /// <summary>
        /// Gets the CLR type that is used to represent the variant type.
        /// </summary>
        /// <param name="type">
        ///   The variant type.
        /// </param>
        /// <returns>
        ///   The CLR type to for the variant type.
        /// </returns>
        public static Type GetClrType(this VariantType type) {
            var item = s_variantTypeMap.FirstOrDefault(x => x.Value == type).Key;
            return item ?? typeof(object);
        }


        /// <summary>
        /// Tries to convert the value of the variant to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to convert the variant value to.
        /// </typeparam>
        /// <param name="variant">
        ///   The <see cref="Variant"/>.
        /// </param>
        /// <param name="value">
        ///   The converted value.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the conversion was successful, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool TryGetValue<T>(this Variant variant, out T? value) {
            if (variant.Value is T val) {
                value = val;
                return true;
            }

            if (variant.Value is IConvertible convertible) {
                try {
                    value = (T) Convert.ChangeType(convertible, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                }
                catch {
                    // Do nothing - we just don't want any exceptions to propagate.
                }
            }

            value = default;
            return false;
        }


        /// <summary>
        /// Gets the variant value cast to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the variant value.
        /// </typeparam>
        /// <param name="variant">
        ///   The variant.
        /// </param>
        /// <returns>
        ///   The variant value cast to an instance of <typeparamref name="T"/>, or the default 
        ///   value of <typeparamref name="T"/> if the <see cref="Variant.Value"/> is not an
        ///   instance of <typeparamref name="T"/>.
        /// </returns>
        public static T? GetValueOrDefault<T>(this Variant variant) {
            return GetValueOrDefault<T>(variant, default);
        }


        /// <summary>
        /// Gets the variant value cast to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the variant value.
        /// </typeparam>
        /// <param name="variant">
        ///   The variant.
        /// </param>
        /// <returns>
        ///   The variant value cast to an instance of <typeparamref name="T"/>, or the default 
        ///   value of <typeparamref name="T"/> if the <see cref="Variant.Value"/> is not an
        ///   instance of <typeparamref name="T"/>.
        /// </returns>
        public static T? GetValueOrDefault<T>(this IEnumerable<Variant> variant) {
            return GetValueOrDefault<T>(variant, default);
        }



        /// <summary>
        /// Gets the variant value cast to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the variant value.
        /// </typeparam>
        /// <param name="variant">
        ///   The variant.
        /// </param>
        /// <param name="defaultValue">
        ///   The default value to return if the <see cref="Variant.Value"/> is not an instance of 
        ///   <typeparamref name="T"/>.
        /// </param>
        /// <returns>
        ///   The variant value cast to an instance of <typeparamref name="T"/>, or the provided 
        ///   default value if the variant <see cref="Variant.Value"/> is not an instance of 
        ///   <typeparamref name="T"/>.
        /// </returns>
        public static T? GetValueOrDefault<T>(this Variant variant, T? defaultValue) {
            if (variant.Value is T val) {
                return val;
            }

            return variant.TryGetValue(out val)
                ? val
                : defaultValue;
        }


        /// <summary>
        /// Gets the first value in the collection that can be cast to the specified type, or 
        /// returns a default value.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the value to return.
        /// </typeparam>
        /// <param name="variants">
        ///   The variant collection.
        /// </param>
        /// <param name="defaultValue">
        ///   The default value to return if none of the values can be converted to 
        ///   <typeparamref name="T"/>.
        /// </param>
        /// <returns>
        ///   The first variant value that can be cast to an instance of <typeparamref name="T"/>, 
        ///   or the provided default value if the no such conversion can be performed on any of 
        ///   the variants in the collection.
        /// </returns>
        public static T? GetValueOrDefault<T>(this IEnumerable<Variant> variants, T? defaultValue) {
            if (variants == null || !variants.Any()) {
                return defaultValue;
            }

            foreach (var value in variants) {
                if (value.TryGetValue<T>(out var val)) {
                    return val;
                }
            }

            return defaultValue;
        }

    }
}
