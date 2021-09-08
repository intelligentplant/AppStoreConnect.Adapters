using System;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Defines flags that can be used to qualify a <see cref="StatusCode"/> for a tag value.
    /// </summary>
    [Flags]
    public enum TagValueStatusCodeFlags : ushort {
        // 0:4 Historian bits
        // 5:6 Not used
        // 7   Overflow
        // 8:9 Limit bits

        #region 0:4 - Historian Bits

        /// <summary>
        /// No flags set.
        /// </summary>
        None = 0x0000,

        /// <summary>
        /// Value was calculated (e.g. by an aggregation functions).
        /// </summary>
        Calculated = 0x0001,

        /// <summary>
        /// Value was interpolated.
        /// </summary>
        Interpolated = 0x0002,

        /// <summary>
        /// Value was calculated from an incomplete interval (e.g. a sample interval of 30 minutes 
        /// was specified but the interval for this calculation was smaller due to the time range 
        /// of the query not being exactly divisible by 30 minutes).
        /// </summary>
        Partial = 0x0004,

        /// <summary>
        /// Additional raw values are defined at the sample timestamp.
        /// </summary>
        ExtraData = 0x0008,

        /// <summary>
        /// Multiple values match the criteria used to select the value (e.g. a maximum 
        /// aggregation where multiple maximum values with different timestamps exist in the 
        /// aggregation interval).
        /// </summary>
        MultiValue = 0x0010,

        #endregion

        #region 7 - Overflow

        /// <summary>
        /// The value was received via a subscription where the source has been forced to drop 
        /// some values due to a maximum queue size limit being exceeded.
        /// </summary>
        Overflow = 0x0080,

        #endregion

        #region 8:9 - Limit Bits

        /// <summary>
        /// The value is at the lower limit defined by the source.
        /// </summary>
        LimitLow = 0x0100,

        /// <summary>
        /// The value is at the higher limit defined by the source.
        /// </summary>
        LimitHigh = 0x0200,

        /// <summary>
        /// The value is constant.
        /// </summary>
        LimitConstant = 0x0300

        #endregion

    }

}
