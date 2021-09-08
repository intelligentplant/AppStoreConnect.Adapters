using System;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes the status code for a tag value or operation.
    /// </summary>
    /// <remarks>
    /// 
    /// <para>
    ///   The <see cref="StatusCode"/> type is largely based on the 32-bit unsigned integer used 
    ///   by the OPC UA specification, but with some OPC UA-specific bits ignored. The following 
    ///   table describes the meanings of individual bits:
    /// </para>
    /// 
    /// <list type="table">
    ///   <item>
    ///     <term>30:31</term>
    ///     <description>
    ///       The overall quality of the <see cref="StatusCode"/>: <c>00</c> = good, <c>01</c> = uncertain, <c>10</c> = bad.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>29</term>
    ///     <description>
    ///       Not used.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>16:28</term>
    ///     <description>
    ///       The sub-code accompanying the overall quality indicator.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>12:15</term>
    ///     <description>
    ///       Not used.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>10:11</term>
    ///     <description>
    ///       The type of information contained in the info bits (see below): <c>00</c> = not used, 
    ///       <c>01</c> = tag value
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>0:9</term>
    ///     <description>
    ///       Additional bits that qualify the status code.
    ///     </description>
    ///   </item>
    /// </list>
    /// 
    /// <para>
    ///   The <see cref="StatusCodes"/> class defines constants for the most common status 
    ///   codes.
    /// </para>
    /// 
    /// </remarks>
    public struct StatusCode : IEquatable<StatusCode> {

        // 30:31     28:29     16:27            12:15     10:11      0:9
        // quality   not_used  sub_code         not_used  info_type  info_bits

        /// <summary>
        /// Bitmask that can be applied to a status code value to clear everything except for the 
        /// quality bits.
        /// </summary>
        internal const uint QualityMask = 0xC0000000;

        /// <summary>
        /// Bitmask for clearing the sub code.
        /// </summary>
        internal const uint ClearSubCodeMask = 0xE000FFFF;

        /// <summary>
        /// Bitmask for clearing the info type and info bits.
        /// </summary>
        internal const uint ClearInfoMask = 0xCFFFF600;

        /// <summary>
        /// Bitmask that can be used to get the info type.
        /// </summary>
        internal const uint InfoTypeMask = 0x00000C00;

        /// <summary>
        /// Info type value when setting tag value info bits.
        /// </summary>
        internal const uint InfoTypeTagValue = 0x00000400;

        /// <summary>
        /// Bitmask for retrieving only the info bits.
        /// </summary>
        internal const uint InfoBitsMask = 0x000001FF;


        /// <summary>
        /// The status code value.
        /// </summary>
        public uint Value { get; }


        /// <summary>
        /// Creates a new <see cref="StatusCode"/>.
        /// </summary>
        /// <param name="value">
        ///   The status code.
        /// </param>
        /// <remarks>
        /// 
        /// <para>
        ///   The <see cref="StatusCodes"/> class defines constants for the most common status 
        ///   codes.
        /// </para>
        /// 
        /// <para>
        ///   <see cref="ForTagValue"/> provides a convenient way to qualify a base status code 
        ///   with info bits for a tag value.
        /// </para>
        /// 
        /// </remarks>
        /// <seealso cref="ForTagValue"/>
        public StatusCode(uint value) {
            Value = value;
        }


        /// <summary>
        /// Creates a new <see cref="StatusCode"/> using the specified quality, sub-code, info 
        /// type flag and info bits.
        /// </summary>
        /// <param name="quality">
        ///   The 2-bit quality flag (<c>0</c> = good, <c>1</c> = uncertain, <c>2</c> = bad).
        /// </param>
        /// <param name="subCode">
        ///   The sub-code for the status code (<c>0x0000</c> - <c>0x0FFF</c>).
        /// </param>
        /// <param name="infoType">
        ///   The info bits type indicator (<c>0</c> = not set, <c>1</c> = tag value, <c>other</c> = not used).
        /// </param>
        /// <param name="infoBits">
        ///   The info bits for the status code (<c>0x0000</c> - <c>0x03FF</c>).
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="quality"/> is outside the range of allowed values.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="subCode"/> is outside the range of allowed values.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="infoType"/> is outside the range of allowed values.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="infoBits"/> is outside the range of allowed values.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   The following table summarises the allowed values for each parameter:
        /// </para>
        /// 
        /// <list type="table">
        ///   <item>
        ///     <term>
        ///       <paramref name="quality"/>
        ///     </term>
        ///     <description>
        ///       <c>0x00</c> - <c>0x03</c> (<c>0</c> = good, <c>1</c> = uncertain, <c>2</c> = bad, <c>3</c> = not currently in use)
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>
        ///       <paramref name="subCode"/>
        ///     </term>
        ///     <description>
        ///       <c>0x0000</c> - <c>0x0FFF</c>
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>
        ///       <paramref name="infoType"/>
        ///     </term>
        ///     <description>
        ///        <c>0x00</c> - <c>0x03</c> (<c>0</c> = not set, <c>1</c> = tag value, <c>2</c> = not currently in use, <c>3</c> = not currently in use)
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>
        ///       <paramref name="infoBits"/>
        ///     </term>
        ///     <description>
        ///       <c>0x0000</c> - <c>0x03FF</c>
        ///     </description>
        ///   </item>
        /// </list>
        /// 
        /// </remarks>
        public StatusCode(byte quality, ushort subCode, byte infoType = 0, ushort infoBits = 0) {
            if (quality > 0x11u) {
                throw new ArgumentOutOfRangeException(nameof(quality));
            }
            if (subCode > 0x0FFFu) {
                throw new ArgumentOutOfRangeException(nameof(subCode));
            }
            if (infoType > 0x03u) {
                throw new ArgumentOutOfRangeException(nameof(infoType));
            }
            if (infoBits > 0x03FFu) {
                throw new ArgumentOutOfRangeException(nameof(infoBits));
            }

            Value = 0u | (quality * 0x40000000u) | (subCode * 0x10000u) | (infoType * 0x400u) | infoBits;
        }


        /// <summary>
        /// Creates a new <see cref="StatusCode"/> using a base status code and additional bits 
        /// that quality the status code for a tag value.
        /// </summary>
        /// <param name="baseValue">
        ///   The base status code.
        /// </param>
        /// <param name="infoBits">
        ///   Additional tag value-specific bits to set on the status code.
        /// </param>
        /// <remarks>
        ///   The <see cref="StatusCodes"/> class defines constants for base status 
        ///   codes.
        /// </remarks>
        public static StatusCode ForTagValue(uint baseValue, RealTimeData.TagValueStatusCodeFlags infoBits) {
            return new StatusCode(baseValue | InfoTypeTagValue | (ushort) infoBits);
        }


        /// <summary>
        /// Creates a new <see cref="StatusCode"/> from a <see cref="RealTimeData.TagValueStatus"/> 
        /// value.
        /// </summary>
        /// <param name="tagValueStatus">
        ///   The <see cref="RealTimeData.TagValueStatus"/> value.
        /// </param>
        [Obsolete("TagValueStatus is deprecated. Use the StatusCode(uint) constructor instead.", false)]
        public static StatusCode FromTagValueStatus(RealTimeData.TagValueStatus tagValueStatus) {
            uint value;

            switch (tagValueStatus) {
                case RealTimeData.TagValueStatus.Good:
                    value = StatusCodes.Good;
                    break;
                case RealTimeData.TagValueStatus.Uncertain:
                    value = StatusCodes.Uncertain;
                    break;
                case RealTimeData.TagValueStatus.Bad:
                default:
                    value = StatusCodes.Bad;
                    break;
            }

            return new StatusCode(value | InfoTypeTagValue);
        }


        /// <inheritdoc/>
        public override int GetHashCode() {
            return Value.GetHashCode();
        }


        /// <inheritdoc/>
        public override bool Equals(object obj) {
            return obj is StatusCode other && Equals(other);
        }


        /// <inheritdoc/>
        public bool Equals(StatusCode other) {
            return Value == other.Value;
        }


        /// <inheritdoc/>
        public override string ToString() {
            return $"0x{Value:X8}";
        }


        /// <inheritdoc/>
        public static implicit operator StatusCode(uint a) {
            return new StatusCode(a);
        }


        /// <inheritdoc/>
        public static implicit operator uint(StatusCode a) {
            return a.Value;
        }


        /// <inheritdoc/>
        public static bool operator ==(StatusCode left, StatusCode right) {
            return left.Equals(right);
        }


        /// <inheritdoc/>
        public static bool operator !=(StatusCode left, StatusCode right) {
            return !left.Equals(right);
        }

    }

}
