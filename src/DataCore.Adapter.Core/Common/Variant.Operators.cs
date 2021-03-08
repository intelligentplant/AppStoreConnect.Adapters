using System;

namespace DataCore.Adapter.Common {

    partial struct Variant {

        /// <inheritdoc/>
        public static bool operator ==(Variant left, Variant right) {
            return left.Equals(right);
        }


        /// <inheritdoc/>
        public static bool operator !=(Variant left, Variant right) {
            return !left.Equals(right);
        }


        /// <inheritdoc/>
        public static implicit operator Variant(bool val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator bool(Variant val) => val.Value == null ? default : (bool) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(bool[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator bool[]?(Variant val) => (bool[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(sbyte val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator sbyte(Variant val) => val.Value == null ? default : (sbyte) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(sbyte[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator sbyte[]?(Variant val) => (sbyte[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(byte val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator byte(Variant val) => val.Value == null ? default : (byte) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(byte[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator byte[]?(Variant val) => (byte[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(short val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator short(Variant val) => val.Value == null ? default : (short) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(short[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator short[]?(Variant val) => (short[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(ushort val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator ushort(Variant val) => val.Value == null ? default : (ushort) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(ushort[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator ushort[]?(Variant val) => (ushort[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(int val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator int(Variant val) => val.Value == null ? default : (int) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(int[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator int[]?(Variant val) => (int[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(uint val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator uint(Variant val) => val.Value == null ? default : (uint) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(uint[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator uint[]?(Variant val) => (uint[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(long val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator long(Variant val) => val.Value == null ? default : (long) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(long[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator long[]?(Variant val) => (long[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(ulong val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator ulong(Variant val) => val.Value == null ? default : (ulong) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(ulong[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator ulong[]?(Variant val) => (ulong[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(float val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator float(Variant val) => val.Value == null ? default : (float) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(float[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator float[]?(Variant val) => (float[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(double val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator double(Variant val) => val.Value == null ? default : (double) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(double[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator double[]?(Variant val) => (double[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(string? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator string?(Variant val) => (string?) val.Value!;

        /// <inheritdoc/>
        public static implicit operator Variant(string[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator string[]?(Variant val) => (string[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(Uri val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator Uri(Variant val) => (Uri) val.Value!;

        /// <inheritdoc/>
        public static implicit operator Variant(Uri[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator Uri[]?(Variant val) => (Uri[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(DateTime val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator DateTime(Variant val) => val.Value == null ? default : (DateTime) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(DateTime[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator DateTime[]?(Variant val) => (DateTime[]?) val.Value;


        /// <inheritdoc/>
        public static implicit operator Variant(TimeSpan val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator TimeSpan(Variant val) => val.Value == null ? default : (TimeSpan) val.Value;

        /// <inheritdoc/>
        public static implicit operator Variant(TimeSpan[]? val) => new Variant(val);

        /// <inheritdoc/>
        public static explicit operator TimeSpan[]?(Variant val) => (TimeSpan[]?) val.Value;

    }

}
