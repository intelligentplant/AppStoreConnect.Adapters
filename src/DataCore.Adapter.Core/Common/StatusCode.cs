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

        // 30:31     29        16:28            12:15     10:11      0:9
        // [00]      [00]      [0000 00000000]  [0000]    [00]       [00 00000000]
        // quality   not_used  sub_code         not_used  info_type  info_bits

        /// <summary>
        /// Bitmask that can be applied to a status code value to get just the overall quality 
        /// indicator of the status code.
        /// </summary>
        private const uint QualityMask = 0xC0000000;

        /// <summary>
        /// Good quality/success condition.
        /// </summary>
        private const uint QualityGood = 0x00000000;

        /// <summary>
        /// Uncertain quality/warning condition.
        /// </summary>
        private const uint QualityUncertain = 0x40000000;

        /// <summary>
        /// Bad quality/failure condition.
        /// </summary>
        private const uint QualityBad = 0x80000000;


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
        ///   The <see cref="StatusCodes"/> class defines constants for the most common status 
        ///   codes.
        /// </remarks>
        public StatusCode(uint value) {
            Value = value;
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
                    value = QualityGood;
                    break;
                case RealTimeData.TagValueStatus.Uncertain:
                    value = QualityUncertain;
                    break;
                case RealTimeData.TagValueStatus.Bad:
                default:
                    value = QualityBad;
                    break;
            }

            return new StatusCode(value);
        }


        /// <summary>
        /// Tests if the specified <see cref="StatusCode"/> represents a good-quality status.
        /// </summary>
        /// <param name="statusCode">
        ///   The <see cref="StatusCode"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="StatusCode"/> represents a good-quality 
        ///   status, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsGood(StatusCode statusCode) {
            return (statusCode.Value & QualityMask) == QualityGood;
        }


        /// <summary>
        /// Tests if the specified <see cref="StatusCode"/> represents an uncertain-quality status.
        /// </summary>
        /// <param name="statusCode">
        ///   The <see cref="StatusCode"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="StatusCode"/> represents an uncertain-quality 
        ///   status, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsUncertain(StatusCode statusCode) {
            return (statusCode.Value & QualityMask) == QualityUncertain;
        }


        /// <summary>
        /// Tests if the specified <see cref="StatusCode"/> represents a bad-quality status.
        /// </summary>
        /// <param name="statusCode">
        ///   The <see cref="StatusCode"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="StatusCode"/> represents a bad-quality 
        ///   status, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsBad(StatusCode statusCode) {
            return (statusCode.Value & QualityMask) == QualityBad;
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
