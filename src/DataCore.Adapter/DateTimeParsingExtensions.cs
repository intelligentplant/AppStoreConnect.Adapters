using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DataCore.Adapter {

    /// <summary>
    /// Extension methods for parsing absolute and relative timestamps, and sample intervals.
    /// </summary>
    public static class DateTimeParsingExtensions {

        /// <summary>
        /// Defines keywords for base times used in relative timestamps.
        /// </summary>
        public static class BaseTime {

            /// <summary>
            /// Start of current year.
            /// </summary>
            public const string CurrentYear = "YEAR";

            /// <summary>
            /// Start of current month.
            /// </summary>
            public const string CurrentMonth = "MONTH";

            /// <summary>
            /// Start of current week.
            /// </summary>
            public const string CurrentWeek = "WEEK";

            /// <summary>
            /// Start of current day.
            /// </summary>
            public const string CurrentDay = "DAY";

            /// <summary>
            /// Start of current hour.
            /// </summary>
            public const string CurrentHour = "HOUR";

            /// <summary>
            /// Start of current minute.
            /// </summary>
            public const string CurrentMinute = "MINUTE";

            /// <summary>
            /// Start of current second.
            /// </summary>
            public const string CurrentSecond = "SECOND";

            /// <summary>
            /// Current time.
            /// </summary>
            public const string Now = "NOW";

        }


        /// <summary>
        /// Defines keywords for time units, used in short-hand time span literals, and offsets in 
        /// relative timestamps.
        /// </summary>
        public static class TimeOffset {

            /// <summary>
            /// Years. Not available when parsing time span literals.
            /// </summary>
            public const string Years = "Y";

            /// <summary>
            /// Months. Not available when parsing time span literals.
            /// </summary>
            public const string Months = "MO";

            /// <summary>
            /// Weeks.
            /// </summary>
            public const string Weeks = "W";

            /// <summary>
            /// Days.
            /// </summary>
            public const string Days = "D";

            /// <summary>
            /// Hours.
            /// </summary>
            public const string Hours = "H";

            /// <summary>
            /// Minutes.
            /// </summary>
            public const string Minutes = "M";

            /// <summary>
            /// Seconds.
            /// </summary>
            public const string Seconds = "S";

            /// <summary>
            /// Milliseconds.
            /// </summary>
            public const string Milliseconds = "MS";

        }


        /// <summary>
        /// Regex pattern for matching time span literals. Note that <see cref="TimeOffset.Years"/> 
        /// and <see cref="TimeOffset.Months"/> are not valid units in time span literals as they 
        /// are meaningless without a base time; they can only be used as offsets in relative 
        /// timestamps.
        /// </summary>
        public static string TimeSpanRegexPattern { get; } = string.Concat(
            @"^\s*(?<count>[0-9]+)\s*(?<unit>",
            string.Join(
                "|",
                TimeOffset.Weeks,
                TimeOffset.Days,
                TimeOffset.Hours,
                TimeOffset.Minutes,
                TimeOffset.Seconds,
                TimeOffset.Milliseconds
            ),
            @")\s*$"
        );
            

        /// <summary>
        /// Regex pattern for matching relative timestamp literals.
        /// </summary>
        public static string RelativeDateRegexPattern { get; } = string.Concat(
            @"^\s*(?<base>",
            string.Join(
                "|",
                BaseTime.CurrentYear,
                BaseTime.CurrentMonth,
                BaseTime.CurrentWeek,
                BaseTime.CurrentDay,
                BaseTime.CurrentHour,
                BaseTime.CurrentMinute,
                BaseTime.CurrentSecond,
                BaseTime.Now
            ),
            @")\s*(?:(?<operator>\+|-)\s*(?<count>[0-9]+)\s*(?<unit>",
            string.Join(
                "|",
                TimeOffset.Years,
                TimeOffset.Months,
                TimeOffset.Weeks,
                TimeOffset.Days,
                TimeOffset.Hours,
                TimeOffset.Minutes,
                TimeOffset.Seconds,
                TimeOffset.Milliseconds
            ),
            @"))?\s*$"
        );

        /// <summary>
        /// Determines whether a string is a relative time stamp.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="m">
        ///   A regular expression match for the string.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid relative timestamp, otherwise 
        ///   <see langword="false"/>.
        /// </returns>
        private static bool IsRelativeDateTime(string s, out Match m) {
            m = Regex.Match(s, RelativeDateRegexPattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            return m.Success;
        }


        /// <summary>
        /// Determines whether a string is a relative timestamp.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid relative timestamp, otherwise 
        ///   <see langword="false"/>.
        /// </returns>
        /// <remarks>
        ///   Relative timestamps are specified in the format <c>[base] - [quantity][unit]</c> or 
        ///   <c>[base] + [quantity][unit]</c> where <c>[base]</c> represents the base time to offset 
        ///   from, <c>[quantity]</c> is a whole number greater than or equal to zero and <c>[unit]</c> 
        ///   is the unit that the offset is measured in.
        ///   
        ///   The following base times can be used:
        ///   
        ///   <list type="table">
        ///     <listheader>
        ///       <term>
        ///         Base Time
        ///       </term>
        ///       <description>
        ///         Description
        ///       </description>
        ///     </listheader>
        ///     <item>
        ///       <term>
        ///         NOW
        ///       </term>
        ///       <description>
        ///         Current time.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         SECOND
        ///       </term>
        ///       <description>
        ///         The start of the current second.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         MINUTE
        ///       </term>
        ///       <description>
        ///         The start of the current minute.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         HOUR
        ///       </term>
        ///       <description>
        ///         The start of the current hour.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         DAY
        ///       </term>
        ///       <description>
        ///         The start of the current day.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         WEEK
        ///       </term>
        ///       <description>
        ///         The start of the current month.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         MONTH
        ///       </term>
        ///       <description>
        ///         The start of the current month.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         YEAR
        ///       </term>
        ///       <description>
        ///         The start of the current year.
        ///       </description>
        ///     </item>
        ///   </list>
        /// 
        ///   The following units can be used:
        /// 
        ///   <list type="table">
        ///     <listheader>
        ///       <term>
        ///         Unit
        ///       </term>
        ///       <description>
        ///         Description
        ///       </description>
        ///     </listheader>
        ///     <item>
        ///       <term>
        ///         MS
        ///       </term>
        ///       <description>
        ///         milliseconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         S
        ///       </term>
        ///       <description>
        ///         seconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         M
        ///       </term>
        ///       <description>
        ///         minutes
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         H
        ///       </term>
        ///       <description>
        ///         hours
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         D
        ///       </term>
        ///       <description>
        ///         days
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         W
        ///       </term>
        ///       <description>
        ///         weeks
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         MO
        ///       </term>
        ///       <description>
        ///         months
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         Y
        ///       </term>
        ///       <description>
        ///         years
        ///       </description>
        ///     </item>
        ///   </list>
        /// 
        ///   Note that all units are case insensitive and white space in the string is ignored.
        /// </remarks>
        public static bool IsRelativeDateTime(this string s) {
            return IsRelativeDateTime(s, out var _);
        }


        /// <summary>
        /// Determines whether a string is a valid absolute timestamp.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="formatProvider">
        ///   An <see cref="IFormatProvider"/> to use when parsing the string.
        /// </param>
        /// <param name="dateTimeStyle">
        ///   A <see cref="DateTimeStyles"/> instance specifying flags to use while parsing dates.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid absolute timestamp, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsAbsoluteDateTime(this string s, IFormatProvider formatProvider, DateTimeStyles dateTimeStyle) {
            return DateTime.TryParse(s, formatProvider, dateTimeStyle, out var _);
        }


        /// <summary>
        /// Attempts to convert a timestamp expressed as milliseconds since 01 January 1970 into a UTC 
        /// <see cref="DateTime"/> instance.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="formatProvider">
        ///   An <see cref="IFormatProvider"/> to use when parsing the string.
        /// </param>
        /// <param name="utcDateTime">
        ///   The UTC time stamp, if <paramref name="s"/> is a numeric value.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="s"/> is a numeric value, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        private static bool TryParseNumericDateTime(this string s, IFormatProvider formatProvider, out DateTime utcDateTime) {
            if (double.TryParse(s, NumberStyles.Float, formatProvider, out var milliseconds)) {
                utcDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(milliseconds);
                return true;
            }

            utcDateTime = default;
            return false;
        }


        /// <summary>
        /// Determines whether a string is a valid absolute or relative timestamp.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="formatProvider">
        ///   The <see cref="IFormatProvider"/> to use when parsing.
        /// </param>
        /// <param name="dateTimeStyle">
        ///   A <see cref="DateTimeStyles"/> instance specifying flags to use while parsing dates.
        /// </param>
        /// <param name="m">
        ///   A regular expression match for the string.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid timestamp, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <remarks>
        ///   If the string can be successfully parsed as an absolute timestamp, <paramref name="m"/> 
        ///   will be <see langword="null"/>.
        /// </remarks>
        private static bool IsDateTime(string s, IFormatProvider formatProvider, DateTimeStyles dateTimeStyle, out Match m) {
            if (IsAbsoluteDateTime(s, formatProvider, dateTimeStyle)) {
                m = null;
                return true;
            }
            if (TryParseNumericDateTime(s, formatProvider, out var _)) {
                m = null;
                return true;
            }

            return IsRelativeDateTime(s, out m);
        }


        /// <summary>
        /// Determines whether a string is a valid absolute or relative timestamp.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="formatProvider">
        ///   The <see cref="IFormatProvider"/> to use when parsing.
        /// </param>
        /// <param name="dateTimeStyle">
        ///   A <see cref="DateTimeStyles"/> instance specifying flags to use while parsing dates.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid timestamp, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        public static bool IsDateTime(this string s, IFormatProvider formatProvider, DateTimeStyles dateTimeStyle) {
            return IsDateTime(s, formatProvider, dateTimeStyle, out var _);
        }


        /// <summary>
        /// Determines whether a string is a valid absolute or relative timestamp.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid timestamp, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool IsDateTime(this string s) {
            return IsDateTime(s, null, DateTimeStyles.None, out var _);
        }


        /// <summary>
        /// Adjusts a <see cref="DateTime"/> based on the specified time unit and quantity.
        /// </summary>
        /// <param name="baseDate">
        ///   The <see cref="DateTime"/> to adjust.
        /// </param>
        /// <param name="unit">
        ///   The time unit.
        /// </param>
        /// <param name="quantity">
        ///   The time unit quantity.
        /// </param>
        /// <param name="add">
        ///   Indicates if the time span should be added to or removed from the <paramref name="baseDate"/>.
        /// </param>
        /// <returns>
        ///   The adjusted <see cref="DateTime"/>.
        /// </returns>
        private static DateTime ApplyOffset(DateTime baseDate, string unit, int quantity, bool add) {
            switch (unit.ToUpperInvariant()) {
                case TimeOffset.Years:
                    return add
                        ? baseDate.AddYears(quantity)
                        : baseDate.AddYears(-1 * quantity);
                case TimeOffset.Months:
                    return add
                        ? baseDate.AddMonths(quantity)
                        : baseDate.AddMonths(-1 * quantity);
                case TimeOffset.Weeks:
                    return add
                        ? baseDate.AddDays(7 * quantity)
                        : baseDate.AddDays(-7 * quantity);
                case TimeOffset.Days:
                    return add
                        ? baseDate.AddDays(quantity)
                        : baseDate.AddDays(-1 * quantity);
                case TimeOffset.Hours:
                    return add
                        ? baseDate.AddHours(quantity)
                        : baseDate.AddHours(-1 * quantity);
                case TimeOffset.Minutes:
                    return add
                        ? baseDate.AddMinutes(quantity)
                        : baseDate.AddMinutes(-1 * quantity);
                case TimeOffset.Seconds:
                    return add
                        ? baseDate.AddSeconds(quantity)
                        : baseDate.AddSeconds(-1 * quantity);
                case TimeOffset.Milliseconds:
                    return add
                        ? baseDate.AddMilliseconds(quantity)
                        : baseDate.AddMilliseconds(-1 * quantity);
            }

            return baseDate;
        }


        /// <summary>
        /// Converts a base time keyword into a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="baseTime">
        ///   The base time keyword. See <see cref="BaseTime"/> for valid values.
        /// </param>
        /// <param name="relativeTo">
        ///   The <see cref="DateTime"/> that the <paramref name="baseTime"/> is relative to.
        /// </param>
        /// <param name="startOfWeek">
        ///   The first day of the week (used when <see cref="BaseTime.CurrentWeek"/> is specified 
        ///   as the base time).
        /// </param>
        /// <returns>
        ///   A <see cref="DateTime"/> that represents the specified base time.
        /// </returns>
        public static DateTime GetBaseTime(string baseTime, DateTime relativeTo, DayOfWeek startOfWeek) {
            if (baseTime == null) {
                throw new ArgumentNullException(nameof(baseTime));
            }

            switch (baseTime.ToUpperInvariant()) {
                case BaseTime.CurrentYear:
                    // Start of current year.
                    return new DateTime(relativeTo.Year, 1, 1, 0, 0, 0, relativeTo.Kind);
                case BaseTime.CurrentMonth:
                    // Start of current month.
                    return new DateTime(relativeTo.Year, relativeTo.Month, 1, 0, 0, 0, relativeTo.Kind);
                case BaseTime.CurrentWeek:
                    // Start of current week.
                    var diff = (7 + (relativeTo.DayOfWeek - startOfWeek)) % 7;
                    var startOfWeekDate = relativeTo.AddDays(-1 * diff).Date;
                    return new DateTime(startOfWeekDate.Year, startOfWeekDate.Month, startOfWeekDate.Day, 0, 0, 0, relativeTo.Kind);
                case BaseTime.CurrentDay:
                    // Start of current day.
                    return new DateTime(relativeTo.Date.Year, relativeTo.Date.Month, relativeTo.Date.Day, 0, 0, 0, relativeTo.Kind);
                case BaseTime.CurrentHour:
                    // Start of current hour.
                    return new DateTime(relativeTo.Year, relativeTo.Month, relativeTo.Day, relativeTo.Hour, 0, 0, relativeTo.Kind);
                case BaseTime.CurrentMinute:
                    // Start of current minute.
                    return new DateTime(relativeTo.Year, relativeTo.Month, relativeTo.Day, relativeTo.Hour, relativeTo.Minute, 0, relativeTo.Kind);
                case BaseTime.CurrentSecond:
                    // Start of current second.
                    return new DateTime(relativeTo.Year, relativeTo.Month, relativeTo.Day, relativeTo.Hour, relativeTo.Minute, relativeTo.Second, relativeTo.Kind);
                case BaseTime.Now:
                    return relativeTo;
                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.Error_InvalidBaseDate, baseTime), nameof(baseTime));
            }
        }


        /// <summary>
        /// Converts an absolute or relative timestamp string into a UTC <see cref="DateTime"/> instance.
        /// </summary>
        /// <param name="dateString">
        ///   The date string.
        /// </param>
        /// <param name="cultureInfo">
        ///   The <see cref="CultureInfo"/> to use when parsing.
        /// </param>
        /// <param name="timeZone">
        ///   The time zone that relative dates are assumed to be in. If <see langword="null"/>, 
        ///   <see cref="TimeZoneInfo.Local"/> will be used.
        /// </param>
        /// <returns>
        ///   A UTC <see cref="DateTime"/> representing the timestamp string.
        /// </returns>
        /// <exception cref="FormatException">
        ///   The string is not a valid absolute or relative time stamp.
        /// </exception>
        /// <remarks>
        ///   Relative time stamps are specified in the format <c>[base] - [quantity][unit]</c> or 
        ///   <c>[base] + [quantity][unit]</c> where <c>[base]</c> represents the base time to offset 
        ///   from, <c>[quantity]</c> is a whole number greater than or equal to zero and <c>[unit]</c> 
        ///   is the unit that the offset is measured in.
        ///   
        ///   The following base times can be used:
        ///   
        ///   <list type="table">
        ///     <listheader>
        ///       <term>
        ///         Base Time
        ///       </term>
        ///       <description>
        ///         Description
        ///       </description>
        ///     </listheader>
        ///     <item>
        ///       <term>
        ///         NOW
        ///       </term>
        ///       <description>
        ///         Current time.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         SECOND
        ///       </term>
        ///       <description>
        ///         The start of the current second.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         MINUTE
        ///       </term>
        ///       <description>
        ///         The start of the current minute.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         HOUR
        ///       </term>
        ///       <description>
        ///         The start of the current hour.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         DAY
        ///       </term>
        ///       <description>
        ///         The start of the current day.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         WEEK
        ///       </term>
        ///       <description>
        ///         The start of the current month.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         MONTH
        ///       </term>
        ///       <description>
        ///         The start of the current month.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         YEAR
        ///       </term>
        ///       <description>
        ///         The start of the current year.
        ///       </description>
        ///     </item>
        ///   </list>
        /// 
        ///   The following units can be used:
        /// 
        ///   <list type="table">
        ///     <listheader>
        ///       <term>
        ///         Unit
        ///       </term>
        ///       <description>
        ///         Description
        ///       </description>
        ///     </listheader>
        ///     <item>
        ///       <term>
        ///         MS
        ///       </term>
        ///       <description>
        ///         milliseconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         S
        ///       </term>
        ///       <description>
        ///         seconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         M
        ///       </term>
        ///       <description>
        ///         minutes
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         H
        ///       </term>
        ///       <description>
        ///         hours
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         D
        ///       </term>
        ///       <description>
        ///         days
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         W
        ///       </term>
        ///       <description>
        ///         weeks
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         MO
        ///       </term>
        ///       <description>
        ///         months
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         Y
        ///       </term>
        ///       <description>
        ///         years
        ///       </description>
        ///     </item>
        ///   </list>
        /// 
        ///   Note that all units are case insensitive and white space in the string is ignored.
        /// </remarks>
        public static DateTime ToUtcDateTime(this string dateString, CultureInfo cultureInfo = null, TimeZoneInfo timeZone = null) {
            if (string.IsNullOrWhiteSpace(dateString)) {
                throw new FormatException(SharedResources.Error_InvalidTimeStamp);
            }

            var dateTimeStyle = DateTimeStyles.None;

            if (!IsDateTime(dateString, cultureInfo, dateTimeStyle, out var m)) {
                throw new FormatException(SharedResources.Error_InvalidTimeStamp);
            }

            if (dateString.TryParseNumericDateTime(cultureInfo, out var dt)) {
                return timeZone.ConvertToUtc(dt);
            }

            if (m == null) {
                dt = DateTime.Parse(dateString, cultureInfo, dateTimeStyle);
                return timeZone.ConvertToUtc(dt);
            }

            
            if (timeZone == null) {
                timeZone = TimeZoneInfo.Local;
            }
            var now = timeZone.GetCurrentTime();
            var startOfWeek = cultureInfo?.DateTimeFormat?.FirstDayOfWeek ?? CultureInfo.InvariantCulture.DateTimeFormat.FirstDayOfWeek;

            var baseDate = GetBaseTime(m.Groups["base"].Value, now, startOfWeek);

            DateTime adjustedDate;

            if (!m.Groups["operator"].Success) {
                adjustedDate = baseDate;
            }
            else {
                GetTimeSpanUnitAndCount(m, cultureInfo, out var unit, out var quantity);
                if (string.IsNullOrWhiteSpace(unit) || double.IsNaN(quantity)) {
                    adjustedDate = baseDate;
                }
                else {
                    adjustedDate = ApplyOffset(baseDate, unit, quantity, !string.Equals(m.Groups["operator"].Value, "-", StringComparison.Ordinal));
                }
            }

            return timeZone.ConvertToUtc(adjustedDate);
        }


        /// <summary>
        /// Attempts to parse the specified absolute or relative timestamp into a UTC <see cref="DateTime"/> 
        /// instance using the specified settings.
        /// </summary>
        /// <param name="dateString">
        ///   The time stamp.
        /// </param>
        /// <param name="dateTime">
        ///   The parsed date.
        ///  </param>
        /// <param name="cultureInfo">
        ///   The <see cref="CultureInfo"/> to use when parsing.
        /// </param>
        /// <param name="timeZone">
        ///   The time zone that relative dates are assumed to be in. If <see langword="null"/>, 
        ///   <see cref="TimeZoneInfo.Local"/> will be used.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the literal was successfully parsed, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        /// <remarks>
        ///   Relative timestamps are specified in the format <c>[base] - [quantity][unit]</c> or 
        ///   <c>[base] + [quantity][unit]</c> where <c>[base]</c> represents the base time to offset 
        ///   from, <c>[quantity]</c> is a whole number greater than or equal to zero and <c>[unit]</c> 
        ///   is the unit that the offset is measured in.
        ///   
        ///   The following base times can be used:
        ///   
        ///   <list type="table">
        ///     <listheader>
        ///       <term>
        ///         Base Time
        ///       </term>
        ///       <description>
        ///         Description
        ///       </description>
        ///     </listheader>
        ///     <item>
        ///       <term>
        ///         NOW
        ///       </term>
        ///       <description>
        ///         Current time.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         SECOND
        ///       </term>
        ///       <description>
        ///         The start of the current second.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         MINUTE
        ///       </term>
        ///       <description>
        ///         The start of the current minute.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         HOUR
        ///       </term>
        ///       <description>
        ///         The start of the current hour.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         DAY
        ///       </term>
        ///       <description>
        ///         The start of the current day.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         WEEK
        ///       </term>
        ///       <description>
        ///         The start of the current month.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         MONTH
        ///       </term>
        ///       <description>
        ///         The start of the current month.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         YEAR
        ///       </term>
        ///       <description>
        ///         The start of the current year.
        ///       </description>
        ///     </item>
        ///   </list>
        /// 
        ///   The following units can be used:
        /// 
        ///   <list type="table">
        ///     <listheader>
        ///       <term>
        ///         Unit
        ///       </term>
        ///       <description>
        ///         Description
        ///       </description>
        ///     </listheader>
        ///     <item>
        ///       <term>
        ///         MS
        ///       </term>
        ///       <description>
        ///         milliseconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         S
        ///       </term>
        ///       <description>
        ///         seconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         M
        ///       </term>
        ///       <description>
        ///         minutes
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         H
        ///       </term>
        ///       <description>
        ///         hours
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         D
        ///       </term>
        ///       <description>
        ///         days
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         W
        ///       </term>
        ///       <description>
        ///         weeks
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         MO
        ///       </term>
        ///       <description>
        ///         months
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         Y
        ///       </term>
        ///       <description>
        ///         years
        ///       </description>
        ///     </item>
        ///   </list>
        /// 
        ///   Note that all units are case insensitive and white space in the string is ignored.
        /// </remarks>
        public static bool TryConvertToUtcDateTime(this string dateString, out DateTime dateTime, CultureInfo cultureInfo = null, TimeZoneInfo timeZone = null) {
            try {
                dateTime = ToUtcDateTime(dateString, cultureInfo, timeZone);
                return true;
            }
            catch {
                dateTime = default;
                return false;
            }
        }


        /// <summary>
        /// Determines whether a string is a valid long-hand or short-hand time span literal.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="formatProvider">
        ///   The <see cref="IFormatProvider"/> to use when parsing.
        /// </param>
        /// <param name="m">
        ///   A regular expression match for the string.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid time span, or <see langword="false"/>
        ///   othewise.
        /// </returns>
        /// <remarks>
        ///   If the string can be successfully parsed as a long-hand literal time span (e.g. "01:23:55"), 
        ///   <paramref name="m"/> will be <see langword="null"/>.
        /// </remarks>
        private static bool IsTimeSpan(string s, IFormatProvider formatProvider, out Match m) {
            if (TimeSpan.TryParse(s, formatProvider, out var _)) {
                m = null;
                return true;
            }

            m = Regex.Match(s, TimeSpanRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return m.Success;
        }


        /// <summary>
        /// Determines whether a string is a valid long-hand or short-hand time span literal.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="formatProvider">
        ///   The <see cref="IFormatProvider"/> to use when parsing.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid time span, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        public static bool IsTimeSpan(this string s, IFormatProvider formatProvider) {
            return IsTimeSpan(s, formatProvider, out var _);
        }


        /// <summary>
        /// Gets the time unit and quantity from the provided regular expression match created 
        /// from <see cref="TimeSpanRegexPattern"/>.
        /// </summary>
        /// <param name="timeSpanMatch">
        ///   The time span regex pattern match.
        /// </param>
        /// <param name="cultureInfo">
        ///   The culture to use when converting the <paramref name="quantity"/> to a number.
        /// </param>
        /// <param name="unit">
        ///   The time unit.
        /// </param>
        /// <param name="quantity">
        ///   The time unit quantity.
        /// </param>
        private static void GetTimeSpanUnitAndCount(Match timeSpanMatch, CultureInfo cultureInfo, out string unit, out int quantity) {
            unit = timeSpanMatch.Groups["unit"].Value;
            quantity = Convert.ToInt32(timeSpanMatch.Groups["count"].Value, cultureInfo);
        }


        /// <summary>
        /// Converts a match for a timespan regex into a <see cref="TimeSpan"/> instance.
        /// </summary>
        /// <param name="m">
        ///   The regex match.
        /// </param>
        /// <param name="cultureInfo">
        ///   The cultue info to use when parsing the quantity to a number.
        /// </param>
        /// <returns>
        ///   The matching time span.
        /// </returns>
        private static TimeSpan ToTimeSpan(Match m, CultureInfo cultureInfo) {
            TimeSpan result;
            GetTimeSpanUnitAndCount(m, cultureInfo, out var unit, out var count);

            switch (unit.ToUpperInvariant()) {
                case TimeOffset.Weeks:
                    result = TimeSpan.FromTicks(TimeSpan.TicksPerDay * 7 * count);
                    break;
                case TimeOffset.Days:
                    result = TimeSpan.FromTicks(TimeSpan.TicksPerDay * count);
                    break;
                case TimeOffset.Hours:
                    result = TimeSpan.FromTicks(TimeSpan.TicksPerHour * count);
                    break;
                case TimeOffset.Minutes:
                    result = TimeSpan.FromTicks(TimeSpan.TicksPerMinute * count);
                    break;
                case TimeOffset.Seconds:
                    result = TimeSpan.FromTicks(TimeSpan.TicksPerSecond * count);
                    break;
                case TimeOffset.Milliseconds:
                    result = TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond * count);
                    break;
                default:
                    result = TimeSpan.Zero;
                    break;
            }

            return result;
        }


        /// <summary>
        /// Converts a long-hand or short-hand time span literal into a <see cref="TimeSpan"/> instance.
        /// </summary>
        /// <param name="timeSpanString">
        ///   The string.
        /// </param>
        /// <param name="cultureInfo">
        ///   The <see cref="CultureInfo"/> to use when parsing.
        /// </param>
        /// <returns>
        ///   A <see cref="TimeSpan"/>.
        /// </returns>
        /// <exception cref="FormatException">
        ///   The string is not a valid timespan.
        /// </exception>
        /// <remarks>
        ///   Initially, the method will attempt to parse the string using the 
        ///   <see cref="TimeSpan.TryParse(string, IFormatProvider, out TimeSpan)"/> method. This 
        ///   ensures that standard time span literals (e.g. <c>"365.00:00:00"</c>) are parsed in 
        ///   the standard way. If the string cannot be parsed in this way, it is tested to see if 
        ///   it is in the format <c>[duration][unit]</c>, where <c>[duration]</c> is a whole 
        ///   number greater than or equal to zero and <c>[unit]</c> is the unit that the duration 
        ///   is measured in.  
        ///   
        ///   The following units are valid:
        /// 
        ///   <list type="table">
        ///     <listheader>
        ///       <term>
        ///         Unit
        ///       </term>
        ///       <description>
        ///         Description
        ///       </description>
        ///     </listheader>
        ///     <item>
        ///       <term>
        ///         MS
        ///       </term>
        ///       <description>
        ///         milliseconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         S
        ///       </term>
        ///       <description>
        ///         seconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         M
        ///       </term>
        ///       <description>
        ///         minutes
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         H
        ///       </term>
        ///       <description>
        ///         hours
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         D
        ///       </term>
        ///       <description>
        ///         days
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         W
        ///       </term>
        ///       <description>
        ///         Weeks
        ///       </description>
        ///     </item>
        ///   </list>
        /// 
        ///   Note that all units are case insensitive and white space in the string is ignored.
        /// </remarks>
        public static TimeSpan ToTimeSpan(this string timeSpanString, CultureInfo cultureInfo = null) {
            if (string.IsNullOrWhiteSpace(timeSpanString)) {
                throw new FormatException(SharedResources.Error_InvalidTimeSpan);
            }

            if (!IsTimeSpan(timeSpanString, cultureInfo, out var m)) {
                throw new FormatException(SharedResources.Error_InvalidTimeSpan);
            }

            return m == null 
                ? TimeSpan.Parse(timeSpanString, cultureInfo) 
                : ToTimeSpan(m, cultureInfo);
        }


        /// <summary>
        /// Attempts to parse the specified long-hand or short-hand time span literal into a 
        /// <see cref="TimeSpan"/> instance.
        /// </summary>
        /// <param name="timeSpanString">
        ///   The string.
        /// </param>
        /// <param name="timeSpan">
        ///   The parsed time span.
        /// </param>
        /// <param name="cultureInfo">
        ///   The <see cref="CultureInfo"/> to use when parsing.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the literal was successfully parsed, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        /// <remarks>
        ///   Initially, the method will attempt to parse the string using the 
        ///   <see cref="TimeSpan.TryParse(string, IFormatProvider, out TimeSpan)"/> method. This 
        ///   ensures that standard time span literals (e.g. <c>"365.00:00:00"</c>) are parsed in 
        ///   the standard way. If the string cannot be parsed in this way, it is tested to see if 
        ///   it is in the format <c>[duration][unit]</c>, where <c>[duration]</c> is a whole 
        ///   number greater than or equal to zero and <c>[unit]</c> is the unit that the duration 
        ///   is measured in.  
        ///   
        ///   The following units are valid:
        /// 
        ///   <list type="table">
        ///     <listheader>
        ///       <term>
        ///         Unit
        ///       </term>
        ///       <description>
        ///         Description
        ///       </description>
        ///     </listheader>
        ///     <item>
        ///       <term>
        ///         MS
        ///       </term>
        ///       <description>
        ///         milliseconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         S
        ///       </term>
        ///       <description>
        ///         seconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         M
        ///       </term>
        ///       <description>
        ///         minutes
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         H
        ///       </term>
        ///       <description>
        ///         hours
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         D
        ///       </term>
        ///       <description>
        ///         days
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         W
        ///       </term>
        ///       <description>
        ///         weeks
        ///       </description>
        ///     </item>
        ///   </list>
        /// 
        ///   Note that all units are case insensitive and white space in the string is ignored.
        /// </remarks>
        public static bool TryConvertToTimeSpan(this string timeSpanString, out TimeSpan timeSpan, CultureInfo cultureInfo = null) {
            try {
                timeSpan = ToTimeSpan(timeSpanString, cultureInfo);
                return true;
            }
            catch {
                timeSpan = default;
                return false;
            }
        }


        /// <summary>
        /// Gets the current time in the specified time zone.
        /// </summary>
        /// <param name="tz">
        ///   The time zone. If <see langword="null"/>, the local system time zone is assumed.
        /// </param>
        /// <returns>
        ///   The current time in the specified time zone.
        /// </returns>
        public static DateTime GetCurrentTime(this TimeZoneInfo tz) {
            if (tz == null) {
                tz = TimeZoneInfo.Local;
            }

            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }


        /// <summary>
        /// Converts a time in the specified time zone to UTC.
        /// </summary>
        /// <param name="tz">
        ///   The time zone. If <see langword="null"/>, the local system time zone is assumed.
        /// </param>
        /// <param name="date">
        ///   The time in the time zone to convert to UTC.
        /// </param>
        /// <returns>
        ///   The equivalent UTC <see cref="DateTime"/>.
        /// </returns>
        public static DateTime ConvertToUtc(this TimeZoneInfo tz, DateTime date) {
            if (date.Kind == DateTimeKind.Utc) {
                return date;
            }

            if (TimeZoneInfo.Utc.Equals(tz)) {
                return new DateTime(date.Ticks, DateTimeKind.Utc);
            }

            if (tz == null) {
                tz = TimeZoneInfo.Local;
            }

            return TimeZoneInfo.ConvertTimeToUtc(date, tz);
        }

    }
}
