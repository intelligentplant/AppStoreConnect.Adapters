using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Extensions for <see cref="StatusCode"/>.
    /// </summary>
    public static class StatusCodeExtensions {

        /// <summary>
        /// Creates a copy of the <see cref="StatusCode"/> but optionally clears the sub code 
        /// and/or info bits.
        /// </summary>
        /// <param name="statusCode">
        ///   The <see cref="StatusCode"/>.
        /// </param>
        /// <param name="resetSubCode">
        ///   When <see langword="true"/>, the sub code bits in the new <see cref="StatusCode"/> 
        ///   are cleared.
        /// </param>
        /// <param name="resetInfoBits">
        ///   When <see langword="true"/>, the info bits in the new <see cref="StatusCode"/> are 
        ///   cleared.
        /// </param>
        /// <returns>
        ///   A new <see cref="StatusCode"/> instance that is a copy of the existing <paramref name="statusCode"/> 
        ///   but with the info type and info bits reset.
        /// </returns>
        public static StatusCode Clone(this StatusCode statusCode, bool resetSubCode = false, bool resetInfoBits = false) {
            uint newCode = statusCode;

            if (resetSubCode) {
                newCode = newCode & StatusCode.ClearSubCodeMask;
            }

            if (resetInfoBits) {
                newCode = newCode & StatusCode.ClearInfoMask;
            }

            return newCode;
        }


        /// <summary>
        /// Tests if the <see cref="StatusCode"/> represents a good-quality status.
        /// </summary>
        /// <param name="statusCode">
        ///   The <see cref="StatusCode"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="StatusCode"/> represents a good-quality 
        ///   status, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsGood(this StatusCode statusCode) {
            return (statusCode.Value & StatusCode.QualityMask) == StatusCodes.Good;
        }


        /// <summary>
        /// Tests if the <see cref="StatusCode"/> represents an uncertain-quality status.
        /// </summary>
        /// <param name="statusCode">
        ///   The <see cref="StatusCode"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="StatusCode"/> represents an uncertain-quality 
        ///   status, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsUncertain(this StatusCode statusCode) {
            return (statusCode.Value & StatusCode.QualityMask) == StatusCodes.Uncertain;
        }


        /// <summary>
        /// Tests if the <see cref="StatusCode"/> represents a bad-quality status.
        /// </summary>
        /// <param name="statusCode">
        ///   The <see cref="StatusCode"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="StatusCode"/> represents a bad-quality 
        ///   status, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsBad(this StatusCode statusCode) {
            // >= here because a quality of 0x11 is reserved for future use in OPC UA but should
            // be interpreted as bad for now.
            return (statusCode.Value & StatusCode.QualityMask) >= StatusCodes.Bad;
        }


        /// <summary>
        /// Tests if the info type bits of the <see cref="StatusCode"/> specify that the info bits 
        /// contain tag value information.
        /// </summary>
        /// <param name="statusCode">
        ///   The <see cref="StatusCode"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="StatusCode"/> info bits contain tag value 
        ///   information, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool HasTagValueInfoType(this StatusCode statusCode) {
            return (statusCode.Value & StatusCode.InfoTypeMask) == StatusCode.InfoTypeTagValue;
        }


        /// <summary>
        /// Tests if the <see cref="StatusCode"/> has the specified <see cref="TagValueStatusCodeFlags"/> 
        /// flag set.
        /// </summary>
        /// <param name="statusCode">
        ///   The <see cref="StatusCode"/>.
        /// </param>
        /// <param name="flag">
        ///   The flag to check for.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the status code's info type bits specify that the info 
        ///   bits are for a tag value and the requested flag is present, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <remarks>
        ///   This method will always return <see langword="false"/> unless the info type bits on 
        ///   the <paramref name="statusCode"/> (bits <c>10:11</c>) are set to <c>0x01</c> (the 
        ///   flag that specifies that the info bits contain qualifiers for a tag value).
        /// </remarks>
        /// <seealso cref="IsCalculatedTagValue(StatusCode)"/>
        /// <seealso cref="IsInterpolatedTagValue(StatusCode)"/>
        /// <seealso cref="IsRawTagValue(StatusCode)"/>
        public static bool HasFlag(this StatusCode statusCode, TagValueStatusCodeFlags flag) {
            var flagVal = (ushort) flag;
            return statusCode.HasTagValueInfoType() && (statusCode.Value & flagVal) == flagVal;
        }


        /// <summary>
        /// Tests if the <see cref="StatusCode"/> indicates that it describes a calculated tag 
        /// value.
        /// </summary>
        /// <param name="statusCode">
        ///   The <see cref="StatusCode"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the info type and info bits specify that the status code 
        ///   describes a calculated tag value, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsCalculatedTagValue(this StatusCode statusCode) {
            return HasFlag(statusCode, TagValueStatusCodeFlags.Calculated);
        }


        /// <summary>
        /// Tests if the <see cref="StatusCode"/> indicates that it describes an interpolated tag 
        /// value.
        /// </summary>
        /// <param name="statusCode">
        ///   The <see cref="StatusCode"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the info type and info bits specify that the status code 
        ///   describes an interpolated tag value, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsInterpolatedTagValue(this StatusCode statusCode) {
            return HasFlag(statusCode, TagValueStatusCodeFlags.Interpolated);
        }


        /// <summary>
        /// Tests if the <see cref="StatusCode"/> indicates that it describes a raw tag value.
        /// </summary>
        /// <param name="statusCode">
        ///   The <see cref="StatusCode"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the info type and info bits specify that the status code 
        ///   describes a raw tag value, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsRawTagValue(this StatusCode statusCode) {
            return !IsCalculatedTagValue(statusCode) && !IsInterpolatedTagValue(statusCode);
        }

    }

}
