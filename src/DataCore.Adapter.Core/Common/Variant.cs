using System;
using System.Collections.Generic;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes a variant value.
    /// </summary>
    public struct Variant : IEquatable<Variant>, IFormattable {

        /// <summary>
        /// Maps from type to variant type.
        /// </summary>
        public static IReadOnlyDictionary<Type, VariantType> VariantTypeMap { get; } = new System.Collections.ObjectModel.ReadOnlyDictionary<Type, VariantType>(new Dictionary<Type, VariantType>() {
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
        });

        /// <summary>
        /// Default string format to use for date-time variant values (ISO 8601-1:2019 extended profile).
        /// </summary>
        public const string DefaultDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

        /// <summary>
        /// Default string format to use for double-precision variant values.
        /// </summary>
        public const string DefaultDoubleFormat = "G17";

        /// <summary>
        /// Default string format to use for single-precision variant values.
        /// </summary>
        public const string DefaultFloatFormat = "G9";

        /// <summary>
        /// Default string format to use for integral variant values.
        /// </summary>
        public const string DefaultIntegralFormat = "G";

        /// <summary>
        /// Null variant.
        /// </summary>
        public static Variant Null { get; } = new Variant(null, VariantType.Null, null);

        /// <summary>
        /// The <see cref="VariantType"/> flags to use when trying to parse a value when a 
        /// value of <see cref="VariantType.Unknown"/> is specified.
        /// </summary>
        private static readonly VariantType[] s_tryParseUnknownVariantTypes = {
            VariantType.Boolean,
            VariantType.Int32,
            VariantType.Int64,
            VariantType.UInt32,
            VariantType.UInt64,
            VariantType.Double,
            VariantType.DateTime,
            VariantType.TimeSpan,
            VariantType.Url,
            // String comes last as it is our final fallback.
            VariantType.String
        };

        /// <summary>
        /// The value.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// The variant type.
        /// </summary>
        public VariantType Type { get; }

        /// <summary>
        /// If <see cref="Value"/> is an array, <see cref="ArrayDimensions"/> defines the 
        /// dimensions of the array. For all other values, this property will be 
        /// <see langword="null"/>.
        /// </summary>
        public int[]? ArrayDimensions { get; }


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
        /// <param name="arrayDimensions">
        ///   The array dimensions of the value, if it is an array.
        /// </param>
        internal Variant(object? value, VariantType type, int[]? arrayDimensions) {
            Value = value;
            Type = type;
            ArrayDimensions = arrayDimensions;
        }


        /// <summary>
        /// Creates a new <see cref="Variant"/> object from the specified value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="value"/> is not a supported type. See <see cref="VariantTypeMap"/> 
        ///   for supported types.
        /// </exception>
        public Variant(object? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            if (value is Variant v) {
                Value = v.Value;
                Type = v.Type;
                ArrayDimensions = v.ArrayDimensions;
                return;
            }

            VariantType variantType;
            int[]? arrayDimensions = null;

            if (value is Array a) {
                GetArraySettings(a, out variantType, out arrayDimensions);
            }
            else {
                var valueType = value.GetType();
                if (!VariantTypeMap.TryGetValue(valueType, out variantType)) {
                    throw new ArgumentOutOfRangeException(nameof(value), valueType, SharedResources.Error_TypeIsUnsupported);
                }
            }

            Value = value;
            Type = variantType;
            ArrayDimensions = arrayDimensions;
        }


        

        /// <summary>
        /// Creates a new <see cref="Variant"/> object from the specified array.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="value"/> is not a supported type. See 
        ///   <see cref="VariantTypeMap"/> for supported types.
        /// </exception>
        public Variant(Array? value) {
            if (value == null) {
                Value = null;
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            GetArraySettings(value!, out var variantType, out var arrayDimensions);

            Value = value;
            Type = variantType;
            ArrayDimensions = arrayDimensions;
        }


        /// <summary>
        /// Gets the <see cref="VariantType"/> and array dimensions for the specified array.
        /// </summary>
        /// <param name="value">
        ///   The array.
        /// </param>
        /// <param name="variantType">
        ///   The <see cref="VariantType"/> for the array's element type.
        /// </param>
        /// <param name="arrayDimensions">
        ///   The dimensions of the array.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The element type of <paramref name="value"/> is not a supported type. See 
        ///   <see cref="VariantTypeMap"/> for supported types.
        /// </exception>
        private static void GetArraySettings(Array value, out VariantType variantType, out int[] arrayDimensions) {
            Type? elemType = value.GetType().GetElementType();
            if (elemType == null || !VariantTypeMap.TryGetValue(elemType, out variantType)) {
                throw new ArgumentOutOfRangeException(nameof(value), elemType, SharedResources.Error_ArrayElementTypeIsUnsupported);
            }

            arrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++) {
                arrayDimensions[i] = value.GetLength(i);
            }
        }


        /// <summary>
        /// Tests if the specified variant has a type of <see cref="VariantType.Null"/> or a 
        /// <see langword="null"/> value.
        /// </summary>
        /// <param name="variant">
        ///   The variant.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the variant type is <see cref="VariantType.Null"/> or the 
        ///   variant value is <see langword="null"/>, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsNull(Variant variant) {
            return variant.Type == VariantType.Null || variant.Value == null;
        }



        /// <summary>
        /// Creates a new <see cref="Variant"/> object that infers the <see cref="VariantType"/> 
        /// from the value.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <remarks>
        ///   If <paramref name="value"/> is a <see cref="Variant"/>, it will be returned 
        ///   unmodified.
        /// </remarks>
        public static Variant FromValue(object? value) {
            if (value == null) {
                return Null;
            }

            if (value is Variant v) {
                return v;
            }

            if (value is Array a) {
                return new Variant(a);
            }

            return new Variant(value);
        }


        /// <summary>
        /// Tries to parse the specified string into a <see cref="Variant"/>, using the provided 
        /// <paramref name="type"/> hint to identify the target value type.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="type">
        ///   The value type for the variant. Specify <see cref="VariantType.Unknown"/> to try and 
        ///   detect the value type automatically.
        /// </param>
        /// <param name="provider">
        ///   The format provider to use. Can be <see langword="null"/>.
        /// </param>
        /// <param name="variant">
        ///   The parsed <see cref="Variant"/> instance.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string was successfully parsed, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool TryParse(string s, VariantType type, IFormatProvider? provider, out Variant variant) {
            if (s == null) {
                variant = Null;
                return type == VariantType.Null;
            }

            switch (type) {
                case VariantType.Boolean:
                    if (bool.TryParse(s, out var boolResult)) {
                        variant = FromValue(boolResult);
                        return true;
                    }
                    break;
                case VariantType.Byte:
                    if (byte.TryParse(s, System.Globalization.NumberStyles.Integer, provider, out var byteResult)) {
                        variant = FromValue(byteResult);
                        return true;
                    }
                    break;
                case VariantType.DateTime:
                    if (DateTime.TryParse(s, provider, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dateResult)) {
                        variant = FromValue(dateResult);
                        return true;
                    }
                    break;
                case VariantType.Double:
                    if (double.TryParse(s, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, provider, out var doubleResult)) {
                        variant = FromValue(doubleResult);
                        return true;
                    }
                    break;
                case VariantType.Float:
                    if (float.TryParse(s, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, provider, out var floatResult)) {
                        variant = FromValue(floatResult);
                        return true;
                    }
                    break;
                case VariantType.Int16:
                    if (short.TryParse(s, System.Globalization.NumberStyles.Integer, provider, out var shortResult)) {
                        variant = FromValue(shortResult);
                        return true;
                    }
                    break;
                case VariantType.Int32:
                    if (int.TryParse(s, System.Globalization.NumberStyles.Integer, provider, out var intResult)) {
                        variant = FromValue(intResult);
                        return true;
                    }
                    break;
                case VariantType.Int64:
                    if (long.TryParse(s, System.Globalization.NumberStyles.Integer, provider, out var longResult)) {
                        variant = FromValue(longResult);
                        return true;
                    }
                    break;
                case VariantType.SByte:
                    if (sbyte.TryParse(s, System.Globalization.NumberStyles.Integer, provider, out var sbyteResult)) {
                        variant = FromValue(sbyteResult);
                        return true;
                    }
                    break;
                case VariantType.String:
                    variant = FromValue(s);
                    return true;
                case VariantType.TimeSpan:
                    if (TimeSpan.TryParse(s, provider, out var timeSpanResult)) {
                        variant = FromValue(timeSpanResult);
                        return true;
                    }
                    break;
                case VariantType.UInt16:
                    if (ushort.TryParse(s, System.Globalization.NumberStyles.Integer, provider, out var ushortResult)) {
                        variant = FromValue(ushortResult);
                        return true;
                    }
                    break;
                case VariantType.UInt32:
                    if (uint.TryParse(s, System.Globalization.NumberStyles.Integer, provider, out var uintResult)) {
                        variant = FromValue(uintResult);
                        return true;
                    }
                    break;
                case VariantType.UInt64:
                    if (ulong.TryParse(s, System.Globalization.NumberStyles.Integer, provider, out var ulongResult)) {
                        variant = FromValue(ulongResult);
                        return true;
                    }
                    break;
                case VariantType.Unknown:
                    foreach (var vType in s_tryParseUnknownVariantTypes) {
                        if (TryParse(s, vType, provider, out var v)) {
                            variant = v;
                            return true;
                        }
                    }
                    break;
                case VariantType.Url:
                    if (Uri.TryCreate(s, UriKind.Absolute, out var url)) {
                        variant = FromValue(url);
                        return true;
                    }
                    break;
            }

            variant = Null;
            return false;
        }


        /// <summary>
        /// Tries to parse the specified string into a <see cref="Variant"/>, using the provided 
        /// <paramref name="type"/> hint to identify the target value type.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="type">
        ///   The value type for the variant. Specify <see cref="VariantType.Unknown"/> to try and 
        ///   detect the value type automatically.
        /// </param>
        /// <param name="variant">
        ///   The parsed <see cref="Variant"/> instance.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string was successfully parsed, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool TryParse(string s, VariantType type, out Variant variant) {
            return TryParse(s, type, null, out variant);
        }


        /// <summary>
        /// Gets the default string format to use for the specified <see cref="VariantType"/>.
        /// </summary>
        /// <param name="type">
        ///   The <see cref="VariantType"/>.
        /// </param>
        /// <returns>
        ///   The default format to use when <see cref="ToString()"/> is called.
        /// </returns>
        public static string GetDefaultFormat(VariantType type) {
            string format = null!;

            // Special handling for some types to ensure correct round-tripping. 
            // See https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings

            switch (type) {
                case VariantType.Double:
                    format = DefaultDoubleFormat;
                    break;
                case VariantType.Float:
                    format = DefaultFloatFormat;
                    break;
                case VariantType.Byte:
                case VariantType.Int16:
                case VariantType.Int32:
                case VariantType.Int64:
                case VariantType.SByte:
                case VariantType.UInt16:
                case VariantType.UInt32:
                case VariantType.UInt64:
                    format = DefaultIntegralFormat;
                    break;
                case VariantType.DateTime:
                    format = DefaultDateTimeFormat;
                    break;
            }

            return format;
        }


        /// <summary>
        ///   Formats the value of the current instance.
        /// </summary>
        /// <returns>
        ///   The formatted value.
        /// </returns>
        public override string ToString() {
            return ToString(GetDefaultFormat(Type), null!);
        }


        /// <summary>
        /// Formats the value of the current instance using the specified format.
        /// </summary>
        /// <param name="format">
        ///   The format to use.
        /// </param>
        /// <returns>
        ///   The formatted value.
        /// </returns>
        public string ToString(string format) {
            return ToString(format, null!);
        }


        /// <inheritdoc/>
        public string ToString(string? format, IFormatProvider? formatProvider) {
            if (Value == null) {
                return null!;
            }
            if (Value is string s) {
                return s;
            }

            if (Value is Array a) {
                return a.ToString();
            }

            try {
                return (format != null && Value is IFormattable formattable)
                    ? formattable.ToString(format, formatProvider)
                    : Value?.ToString()!;
            }
            catch {
                return Value?.ToString()!;
            }
        }


        /// <inheritdoc/>
        public override int GetHashCode() {
#if NETSTANDARD2_0 || NET46
            return HashGenerator.Combine(Type, Value);
#else
            return HashCode.Combine(Type, Value);
#endif
        }


        /// <inheritdoc/>
        public override bool Equals(object? obj) {
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


        /// <inheritdoc/>
        public static implicit operator Variant(bool val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator bool(Variant val) => val.Value == null ? default : (bool) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(bool[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator bool[]?(Variant val) => (bool[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(sbyte val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator sbyte(Variant val) => val.Value == null ? default : (sbyte) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(sbyte[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator sbyte[]?(Variant val) => (sbyte[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(byte val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator byte(Variant val) => val.Value == null ? default : (byte) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(byte[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator byte[]?(Variant val) => (byte[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(short val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator short(Variant val) => val.Value == null ? default : (short) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(short[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator short[]?(Variant val) => (short[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(ushort val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator ushort(Variant val) => val.Value == null ? default : (ushort) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(ushort[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator ushort[]?(Variant val) => (ushort[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(int val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator int(Variant val) => val.Value == null ? default : (int) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(int[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator int[]?(Variant val) => (int[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(uint val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator uint(Variant val) => val.Value == null ? default : (uint) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(uint[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator uint[]?(Variant val) => (uint[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(long val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator long(Variant val) => val.Value == null ? default : (long) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(long[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator long[]?(Variant val) => (long[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(ulong val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator ulong(Variant val) => val.Value == null ? default : (ulong) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(ulong[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator ulong[]?(Variant val) => (ulong[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(float val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator float(Variant val) => val.Value == null ? default : (float) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(float[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator float[]?(Variant val) => (float[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(double val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator double(Variant val) => val.Value == null ? default : (double) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(double[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator double[]?(Variant val) => (double[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(string? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator string?(Variant val) => (string?) val.Value!;

        /// <inheritdoc/>
        public static implicit operator Variant(string[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator string[]?(Variant val) => (string[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(Uri val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator Uri(Variant val) => (Uri) val.Value!;

        /// <inheritdoc/>
        public static implicit operator Variant(Uri[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator Uri[]?(Variant val) => (Uri[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(DateTime val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator DateTime(Variant val) => val.Value == null ? default : (DateTime) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(DateTime[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator DateTime[]?(Variant val) => (DateTime[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(TimeSpan val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator TimeSpan(Variant val) => val.Value == null ? default : (TimeSpan) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(TimeSpan[]? val) => FromValue(val);

        /// <inheritdoc/>
        public static explicit operator TimeSpan[]?(Variant val) => (TimeSpan[]?) val.Value;

    }

}
