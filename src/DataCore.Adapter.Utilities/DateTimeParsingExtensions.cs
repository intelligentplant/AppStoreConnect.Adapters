using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DataCore.Adapter {

    /// <summary>
    /// Extension methods for parsing absolute and relative time stamps, and sample intervals.
    /// </summary>
    public static class DateTimeParsingExtensions {

        /// <summary>
        /// Base regex pattern for matching time span literals.
        /// </summary>
        /// <remarks>
        ///   Do not use this pattern to directly match time spans - this requires start/end anchors 
        ///   and white space padding before/after the time span.
        /// </remarks>
        private const string BaseTimeSpanRegexPattern = @"(?<count>[0-9]*\.?[0-9]+)\s*(?<unit>ms|[ydhms])";

        /// <summary>
        /// Regex pattern for matching time span literals.
        /// </summary>
        private const string TimeSpanRegexPattern = @"^\s*" + BaseTimeSpanRegexPattern + @"\s*$";

        /// <summary>
        /// Regex pattern for matching relatve time stamp literals.
        /// </summary>
        private const string RelativeDateRegexPattern = @"^\(?<base>*|[ty])\s*(?:(?<operator>\+|-)\s*" + BaseTimeSpanRegexPattern + @")?\s*$";


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
        ///   <see langword="true"/> if the string is a valid relative time stamp, otherwise 
        ///   <see langword="false"/>.
        /// </returns>
        private static bool IsRelativeDateTime(string s, out Match m) {
            m = Regex.Match(s, RelativeDateRegexPattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            return m.Success;
        }


        /// <summary>
        /// Determines whether a string is a relative time stamp.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid relative time stamp, otherwise 
        ///   <see langword="false"/>.
        /// </returns>
        /// <remarks>
        ///   Relative time stamps are specified in the format <c>[base] - [duration][unit]</c> or 
        ///   <c>[base] + [duration][unit]</c> where <c>[base]</c> represents the base time to offset from, 
        ///   <c>[duration]</c> is a number greater than or equal to zero and <c>[unit]</c> is the 
        ///   unit that the duration is measured in. Integer and floating point durations are both valid.
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
        ///         *
        ///       </term>
        ///       <description>
        ///         Current time.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         t
        ///       </term>
        ///       <description>
        ///         Midnight UTC on the current day.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         y
        ///       </term>
        ///       <description>
        ///         Midnight UTC on the previous day.
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
        ///         ms
        ///       </term>
        ///       <description>
        ///         milliseconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         s
        ///       </term>
        ///       <description>
        ///         seconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         m
        ///       </term>
        ///       <description>
        ///         minutes
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         h
        ///       </term>
        ///       <description>
        ///         hours
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         d
        ///       </term>
        ///       <description>
        ///         days
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         y
        ///       </term>
        ///       <description>
        ///         years (1y == 365d)
        ///       </description>
        ///     </item>
        ///   </list>
        /// 
        ///   Note that all units are case insensitive and white space in the string is ignored.
        /// </remarks>
        public static bool IsRelativeDateTime(this string s) {
            return IsRelativeDateTime(s, out var m);
        }


        /// <summary>
        /// Determines whether a string is a valid absolute time stamp.
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
        ///   <see langword="true"/> if the string is a valid absolute time stamp, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsAbsoluteDateTime(this string s, IFormatProvider formatProvider, DateTimeStyles dateTimeStyle) {
            return DateTime.TryParse(s, formatProvider, dateTimeStyle, out var d);
        }


        /// <summary>
        /// Attempts to convert a time stamp expressed as milliseconds since 01 January 1970 into a UTC 
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

            utcDateTime = default(DateTime);
            return false;
        }


        /// <summary>
        /// Determines whether a string is a valid absolute or relative time stamp.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="formatProvider">
        ///   The <see cref="IFormatProvider"/> to use when parsing absolute dates.
        /// </param>
        /// <param name="dateTimeStyle">
        ///   A <see cref="DateTimeStyles"/> instance specifying flags to use while parsing dates.
        /// </param>
        /// <param name="m">
        ///   A regular expression match for the string.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid time stamp, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <remarks>
        ///   If the string can be successfully parsed as an absolute time stamp, <paramref name="m"/> 
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
        /// Determines whether a string is a valid absolute or relative time stamp.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="formatProvider">
        ///   The <see cref="IFormatProvider"/> to use when parsing absolute dates.
        /// </param>
        /// <param name="dateTimeStyle">
        ///   A <see cref="DateTimeStyles"/> instance specifying flags to use while parsing dates.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid time stamp, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        public static bool IsDateTime(this string s, IFormatProvider formatProvider, DateTimeStyles dateTimeStyle) {
            return IsDateTime(s, formatProvider, dateTimeStyle, out var m);
        }


        /// <summary>
        /// Determines whether a string is a valid absolute or relative time stamp.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid time stamp, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool IsDateTime(this string s) {
            return IsDateTime(s, null, DateTimeStyles.None, out var m);
        }


        /// <summary>
        /// Converts an absolute or relative time stamp string into a UTC <see cref="DateTime"/> instance.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="formatProvider">
        ///   The <see cref="IFormatProvider"/> to use when parsing absolute dates.
        /// </param>
        /// <returns>
        ///   A UTC <see cref="DateTime"/> representing the time stamp string.
        /// </returns>
        /// <exception cref="FormatException">
        ///   The string is not a valid absolute or relative time stamp.
        /// </exception>
        /// <remarks>
        ///   Relative time stamps are specified in the format <c>[base] - [duration][unit]</c> or 
        ///   <c>[base] + [duration][unit]</c> where <c>[base]</c> represents the base time to offset from, 
        ///   <c>[duration]</c> is a number greater than or equal to zero and <c>[unit]</c> is the 
        ///   unit that the duration is measured in. Integer and floating point durations are both valid.
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
        ///         *
        ///       </term>
        ///       <description>
        ///         Current time.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         t
        ///       </term>
        ///       <description>
        ///         Midnight UTC on the current day.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         y
        ///       </term>
        ///       <description>
        ///         Midnight UTC on the previous day.
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
        ///         ms
        ///       </term>
        ///       <description>
        ///         milliseconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         s
        ///       </term>
        ///       <description>
        ///         seconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         m
        ///       </term>
        ///       <description>
        ///         minutes
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         h
        ///       </term>
        ///       <description>
        ///         hours
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         d
        ///       </term>
        ///       <description>
        ///         days
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         y
        ///       </term>
        ///       <description>
        ///         years (1y == 365d)
        ///       </description>
        ///     </item>
        ///   </list>
        /// 
        ///   Note that all units are case insensitive and white space in the string is ignored.
        /// </remarks>
        public static DateTime ToUtcDateTime(this string s, IFormatProvider formatProvider) {
            if (string.IsNullOrWhiteSpace(s)) {
                throw new FormatException(Resources.Error_InvalidTimeStamp);
            }

            var dateTimeStyle = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

            if (!IsDateTime(s, formatProvider, dateTimeStyle, out var m)) {
                throw new FormatException(Resources.Error_InvalidTimeStamp);
            }

            if (s.TryParseNumericDateTime(formatProvider, out var dt)) {
                return dt;
            }

            if (m == null) {
                return DateTime.Parse(s, formatProvider, dateTimeStyle);
            }

            DateTime baseDate;
            switch(m.Groups["base"].Value.ToUpper()) {
                case "T":
                    // Midnight UTC today.
                    baseDate = DateTime.UtcNow.Date;
                    break;
                case "Y":
                    // Midnight UTC yesterday.
                    baseDate = DateTime.UtcNow.Date.AddDays(-1);
                    break;
                default:
                    baseDate = DateTime.UtcNow;
                    break;
            }

            if (!m.Groups["operator"].Success) {
                return baseDate;
            }

            var difference = ToTimeSpan(m);
            if (m.Groups["operator"].Value == "-") {
                difference = difference.Negate();
            }

            return baseDate.Add(difference);
        }


        /// <summary>
        /// Attempts to parse the specified absolute or relative time stamp into a UTC <see cref="DateTime"/> 
        /// instance using the specified settings.
        /// </summary>
        /// <param name="s">
        ///   The time stamp.
        /// </param>
        /// <param name="formatProvider">
        ///   The <see cref="IFormatProvider"/> to use when parsing absolute dates.
        /// </param>
        /// <param name="dateTime">
        ///   The parsed date.
        ///  </param>
        /// <returns>
        ///   <see langword="true"/> if the literal was successfully parsed, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        /// <remarks>
        ///   Relative time stamps are specified in the format <c>[base] - [duration][unit]</c> or 
        ///   <c>[base] + [duration][unit]</c> where <c>[base]</c> represents the base time to offset from, 
        ///   <c>[duration]</c> is a number greater than or equal to zero and <c>[unit]</c> is the 
        ///   unit that the duration is measured in. Integer and floating point durations are both valid.
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
        ///         *
        ///       </term>
        ///       <description>
        ///         Current time.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         t
        ///       </term>
        ///       <description>
        ///         Midnight UTC on the current day.
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         y
        ///       </term>
        ///       <description>
        ///         Midnight UTC on the previous day.
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
        ///         ms
        ///       </term>
        ///       <description>
        ///         milliseconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         s
        ///       </term>
        ///       <description>
        ///         seconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         m
        ///       </term>
        ///       <description>
        ///         minutes
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         h
        ///       </term>
        ///       <description>
        ///         hours
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         d
        ///       </term>
        ///       <description>
        ///         days
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         y
        ///       </term>
        ///       <description>
        ///         years (1y == 365d)
        ///       </description>
        ///     </item>
        ///   </list>
        /// 
        ///   Note that all units are case insensitive and white space in the string is ignored.
        /// </remarks>
        public static bool TryConvertToUtcDateTime(this string s, IFormatProvider formatProvider, out DateTime dateTime) {
            try {
                dateTime = ToUtcDateTime(s, formatProvider);
                return true;
            }
            catch {
                dateTime = default(DateTime);
                return false;
            }
        }


        /// <summary>
        /// Determines whether a string is a valid longhand or shorthand time span literal.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="formatProvider">
        ///   The <see cref="IFormatProvider"/> to use when parsing longhand time spans.
        /// </param>
        /// <param name="m">
        ///   A regular expression match for the string.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid time span, or <see langword="false"/>
        ///   othewise.
        /// </returns>
        /// <remarks>
        ///   If the string can be successfully parsed as a longhand literal time span (e.g. "01:23:55"), 
        ///   <paramref name="m"/> will be <see langword="null"/>.
        /// </remarks>
        private static bool IsTimeSpan(string s, IFormatProvider formatProvider, out Match m) {
            if (TimeSpan.TryParse(s, formatProvider, out var t)) {
                m = null;
                return true;
            }

            m = Regex.Match(s, TimeSpanRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return m.Success;
        }


        /// <summary>
        /// Determines whether a string is a valid longhand or shorthand time span literal.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="formatProvider">
        ///   The <see cref="IFormatProvider"/> to use when parsing longhand time spans.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string is a valid time span, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        public static bool IsTimeSpan(this string s, IFormatProvider formatProvider) {
            return IsTimeSpan(s, formatProvider, out var m);
        }


        /// <summary>
        /// Converts a match for a timespan regex into a <see cref="TimeSpan"/> instance.
        /// </summary>
        /// <param name="m">
        ///   The regex match.
        /// </param>
        /// <returns>
        ///   The matching time span.
        /// </returns>
        private static TimeSpan ToTimeSpan(Match m) {
            TimeSpan result;
            var count = Convert.ToDouble(m.Groups["count"].Value, CultureInfo.InvariantCulture);

            switch (m.Groups["unit"].Value.ToUpperInvariant()) {
                case "Y":
                    result = TimeSpan.FromTicks((long) (TimeSpan.TicksPerDay * 365 * count));
                    break;
                case "D":
                    result = TimeSpan.FromTicks((long) (TimeSpan.TicksPerDay * count));
                    break;
                case "H":
                    result = TimeSpan.FromTicks((long) (TimeSpan.TicksPerHour * count));
                    break;
                case "M":
                    result = TimeSpan.FromTicks((long) (TimeSpan.TicksPerMinute * count));
                    break;
                case "S":
                    result = TimeSpan.FromTicks((long) (TimeSpan.TicksPerSecond * count));
                    break;
                case "MS":
                    result = TimeSpan.FromTicks((long) (TimeSpan.TicksPerMillisecond * count));
                    break;
                default:
                    result = TimeSpan.Zero;
                    break;
            }

            return result;
        }


        /// <summary>
        /// Converts a longhand or shorthand time span literal into a <see cref="TimeSpan"/> instance.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="formatProvider">
        ///   The <see cref="IFormatProvider"/> to use when parsing longhand time spans.
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
        ///   ensures that standard time span literals (e.g. <c>"365.00:00:00"</c>) are parsed in the 
        ///   standard way. If the string cannot be parsed in this way, it is tested to see if it is 
        ///   in the format <c>[duration][unit]</c>, where <c>[duration]</c> is a number greater than 
        ///   or equal to zero and <c>[unit]</c> is the unit that the duration is measured in.  
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
        ///         ms
        ///       </term>
        ///       <description>
        ///         milliseconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         s
        ///       </term>
        ///       <description>
        ///         seconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         m
        ///       </term>
        ///       <description>
        ///         minutes
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         h
        ///       </term>
        ///       <description>
        ///         hours
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         d
        ///       </term>
        ///       <description>
        ///         days
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         y
        ///       </term>
        ///       <description>
        ///         years (1y == 365d)
        ///       </description>
        ///     </item>
        ///   </list>
        /// 
        ///   Note that all units are case insensitive and white space in the string is ignored.
        /// </remarks>
        public static TimeSpan ToTimeSpan(this string s, IFormatProvider formatProvider) {
            if (string.IsNullOrWhiteSpace(s)) {
                throw new FormatException(Resources.Error_InvalidTimeSpan);
            }

            if (!IsTimeSpan(s, formatProvider, out var m)) {
                throw new FormatException(Resources.Error_InvalidTimeSpan);
            }

            return m == null 
                ? TimeSpan.Parse(s, formatProvider) 
                : ToTimeSpan(m);
        }


        /// <summary>
        /// Attempts to parse the specified longhand or shorthand time span literal into a 
        /// <see cref="TimeSpan"/> instance.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="formatProvider">
        ///   The format provider to use when parsing.
        /// </param>
        /// <param name="timeSpan">
        ///   The parsed time span.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the literal was successfully parsed, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        /// <remarks>
        ///   Initially, the method will attempt to parse the string using the 
        ///   <see cref="TimeSpan.TryParse(string, IFormatProvider, out TimeSpan)"/> method. This 
        ///   ensures that standard time span literals (e.g. <c>"365.00:00:00"</c>) are parsed in the 
        ///   standard way. If the string cannot be parsed in this way, it is tested to see if it is 
        ///   in the format <c>[duration][unit]</c>, where <c>[duration]</c> is a number greater than 
        ///   or equal to zero and <c>[unit]</c> is the unit that the duration is measured in.  
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
        ///         ms
        ///       </term>
        ///       <description>
        ///         milliseconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         s
        ///       </term>
        ///       <description>
        ///         seconds
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         m
        ///       </term>
        ///       <description>
        ///         minutes
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         h
        ///       </term>
        ///       <description>
        ///         hours
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         d
        ///       </term>
        ///       <description>
        ///         days
        ///       </description>
        ///     </item>
        ///     <item>
        ///       <term>
        ///         y
        ///       </term>
        ///       <description>
        ///         years (1y == 365d)
        ///       </description>
        ///     </item>
        ///   </list>
        /// 
        ///   Note that all units are case insensitive and white space in the string is ignored.
        /// </remarks>
        public static bool TryConvertToTimeSpan(this string s, IFormatProvider formatProvider, out TimeSpan timeSpan) {
            try {
                timeSpan = ToTimeSpan(s, formatProvider);
                return true;
            }
            catch {
                timeSpan = default(TimeSpan);
                return false;
            }
        }

    }
}
