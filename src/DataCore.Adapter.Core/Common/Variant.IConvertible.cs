using System;

namespace DataCore.Adapter.Common {

    partial struct Variant : IConvertible {

        /// <inheritdoc/>
        public TypeCode GetTypeCode() {
            switch (Type) {
                case VariantType.Boolean:
                    return TypeCode.Boolean;
                case VariantType.Byte:
                    return TypeCode.Byte;
                case VariantType.DateTime:
                    return TypeCode.DateTime;
                case VariantType.Double:
                    return TypeCode.Double;
                case VariantType.Float:
                    return TypeCode.Single;
                case VariantType.Int16:
                    return TypeCode.Int16;
                case VariantType.Int32:
                    return TypeCode.Int32;
                case VariantType.Int64:
                    return TypeCode.Int64;
                case VariantType.Null:
                    return TypeCode.Empty;
                case VariantType.SByte:
                    return TypeCode.SByte;
                case VariantType.String:
                    return TypeCode.String;
                case VariantType.TimeSpan:
                    return TypeCode.Object;
                case VariantType.UInt16:
                    return TypeCode.UInt16;
                case VariantType.UInt32:
                    return TypeCode.UInt32;
                case VariantType.UInt64:
                    return TypeCode.UInt64;
                case VariantType.Url:
                    return TypeCode.Object;
                default:
                    return TypeCode.Object;
            }
        }


        /// <inheritdoc/>
        public bool ToBoolean(IFormatProvider provider) {
            return Convert.ToBoolean(Value, provider);
        }


        /// <inheritdoc/>
        public char ToChar(IFormatProvider provider) {
            return Convert.ToChar(Value, provider);
        }


        /// <inheritdoc/>
        public sbyte ToSByte(IFormatProvider provider) {
            return Convert.ToSByte(Value, provider);
        }


        /// <inheritdoc/>
        public byte ToByte(IFormatProvider provider) {
            return Convert.ToByte(Value, provider);
        }


        /// <inheritdoc/>
        public short ToInt16(IFormatProvider provider) {
            return Convert.ToInt16(Value, provider);
        }


        /// <inheritdoc/>
        public ushort ToUInt16(IFormatProvider provider) {
            return Convert.ToUInt16(Value, provider);
        }


        /// <inheritdoc/>
        public int ToInt32(IFormatProvider provider) {
            return Convert.ToInt32(Value, provider);
        }


        /// <inheritdoc/>
        public uint ToUInt32(IFormatProvider provider) {
            return Convert.ToUInt32(Value, provider);
        }


        /// <inheritdoc/>
        public long ToInt64(IFormatProvider provider) {
            return Convert.ToInt64(Value, provider);
        }


        /// <inheritdoc/>
        public ulong ToUInt64(IFormatProvider provider) {
            return Convert.ToUInt64(Value, provider);
        }


        /// <inheritdoc/>
        public float ToSingle(IFormatProvider provider) {
            return Convert.ToSingle(Value, provider);
        }


        /// <inheritdoc/>
        public double ToDouble(IFormatProvider provider) {
            return Convert.ToDouble(Value, provider);
        }


        /// <inheritdoc/>
        public decimal ToDecimal(IFormatProvider provider) {
            return Convert.ToDecimal(Value, provider);
        }


        /// <inheritdoc/>
        public DateTime ToDateTime(IFormatProvider provider) {
            return Convert.ToDateTime(Value, provider);
        }


        /// <inheritdoc/>
        public string ToString(IFormatProvider provider) {
            return ToString(provider);
        }


        /// <inheritdoc/>
        public object ToType(Type conversionType, IFormatProvider provider) {
            return Convert.ChangeType(Value, conversionType, provider);
        }

    }

}
