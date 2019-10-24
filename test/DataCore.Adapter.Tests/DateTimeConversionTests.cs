using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class DateTimeConversionTests {

        [TestMethod]
        public void AbsoluteUtcTimestampShouldBeParsedCorrectly() {
            var unparsed = "2019-10-24T07:00:00Z";
            var expected = new DateTime(2019, 10, 24, 7, 0, 0, DateTimeKind.Utc);

            Assert.IsTrue(unparsed.TryConvertToUtcDateTime(out var actual));
            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void AbsoluteLocalTimestampShouldBeParsedCorrectly() {
            var unparsed = "2019-10-24T07:00:00+03:00";
            var expected = new DateTime(2019, 10, 24, 4, 0, 0, DateTimeKind.Utc);

            Assert.IsTrue(unparsed.TryConvertToUtcDateTime(out var actual));
            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void AbsoluteTimestampShouldBeParsedCorrectlyUsingTimeZoneInfo() {
            var unparsed = "2019-10-24T07:00:00";
            DateTime expected;
            DateTime actual;
            string tzName;

            // Test 1: convert using UTC time zone.
            expected = new DateTime(2019, 10, 24, 7, 0, 0, DateTimeKind.Utc);
            Assert.IsTrue(unparsed.TryConvertToUtcDateTime(out actual, timeZone: TimeZoneInfo.Utc));
            Assert.AreEqual(expected, actual, "Unexpected date when using UTC time zone");

            // Test 2: convert using local time zone.
            expected = new DateTime(2019, 10, 24, 7, 0, 0, DateTimeKind.Local).ToUniversalTime();
            Assert.IsTrue(unparsed.TryConvertToUtcDateTime(out actual, timeZone: TimeZoneInfo.Local));
            Assert.AreEqual(expected, actual, "Unexpected date when using local time zone");

            // Test 3: convert from Azerbaijan Standard Time (UTC+4).
            tzName = "Azerbaijan Standard Time";
            expected = new DateTime(2019, 10, 24, 3, 0, 0, DateTimeKind.Utc);
            Assert.IsTrue(unparsed.TryConvertToUtcDateTime(out actual, timeZone: TimeZoneInfo.FindSystemTimeZoneById(tzName)));
            Assert.AreEqual(expected, actual, $"Unexpected date when using time zone: {tzName}");

            // Test 4: convert from Mountain Standard Time (UTC-7; -6 for specified date due to DST).
            tzName = "Mountain Standard Time";
            expected = new DateTime(2019, 10, 24, 13, 0, 0, DateTimeKind.Utc);
            Assert.IsTrue(unparsed.TryConvertToUtcDateTime(out actual, timeZone: TimeZoneInfo.FindSystemTimeZoneById(tzName)));
            Assert.AreEqual(expected, actual, $"Unexpected date when using time zone: {tzName}");
        }


        [DataTestMethod]
        // UTC
        [DataRow(DateTimeParsingExtensions.BaseTime.Now, "UTC")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentSecond, "UTC")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentMinute, "UTC")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentHour, "UTC")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentDay, "UTC")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentWeek, "UTC")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentMonth, "UTC")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentYear, "UTC")]
        // Mountain Standard Time
        [DataRow(DateTimeParsingExtensions.BaseTime.Now, "Mountain Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentSecond, "Mountain Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentMinute, "Mountain Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentHour, "Mountain Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentDay, "Mountain Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentWeek, "Mountain Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentMonth, "Mountain Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentYear, "Mountain Standard Time")]
        // Azerbaijan Standard Time
        [DataRow(DateTimeParsingExtensions.BaseTime.Now, "Azerbaijan Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentSecond, "Azerbaijan Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentMinute, "Azerbaijan Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentHour, "Azerbaijan Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentDay, "Azerbaijan Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentWeek, "Azerbaijan Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentMonth, "Azerbaijan Standard Time")]
        [DataRow(DateTimeParsingExtensions.BaseTime.CurrentYear, "Azerbaijan Standard Time")]
        public void RelativeTimestampShouldBeParsedCorrectly(string baseTime, string timeZoneId) {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            // No offset
            RunRelativeTimestampTest(baseTime, timeZone, null, null);

            // Milliseconds
            RunRelativeTimestampTest(baseTime, timeZone, "+100MS", dt => dt.AddMilliseconds(100));
            RunRelativeTimestampTest(baseTime, timeZone, "-100MS", dt => dt.AddMilliseconds(-100));

            // Seconds
            RunRelativeTimestampTest(baseTime, timeZone, "+18S", dt => dt.AddSeconds(18));
            RunRelativeTimestampTest(baseTime, timeZone, "-18S", dt => dt.AddSeconds(-18));

            // Minutes
            RunRelativeTimestampTest(baseTime, timeZone, "+427M", dt => dt.AddMinutes(427));
            RunRelativeTimestampTest(baseTime, timeZone, "-427M", dt => dt.AddMinutes(-427));

            // Hours
            RunRelativeTimestampTest(baseTime, timeZone, "+9H", dt => dt.AddHours(9));
            RunRelativeTimestampTest(baseTime, timeZone, "-9H", dt => dt.AddHours(-9));

            // Days
            RunRelativeTimestampTest(baseTime, timeZone, "+12D", dt => dt.AddDays(12));
            RunRelativeTimestampTest(baseTime, timeZone, "-12D", dt => dt.AddDays(-12));

            // Weeks
            RunRelativeTimestampTest(baseTime, timeZone, "+51W", dt => dt.AddDays(7 * 51));
            RunRelativeTimestampTest(baseTime, timeZone, "-51W", dt => dt.AddDays(-1 * 7 * 51));

            // Months
            RunRelativeTimestampTest(baseTime, timeZone, "+3MO", dt => dt.AddMonths(3));
            RunRelativeTimestampTest(baseTime, timeZone, "-3MO", dt => dt.AddMonths(-3));

            // Years
            RunRelativeTimestampTest(baseTime, timeZone, "+5Y", dt => dt.AddYears(5));
            RunRelativeTimestampTest(baseTime, timeZone, "-5Y", dt => dt.AddYears(-5));
        }


        private void RunRelativeTimestampTest(string baseTime, TimeZoneInfo timeZone, string offset, Func<DateTime, DateTime> addOffsetToBaseTime) {
            const long delta = TimeSpan.TicksPerMillisecond * 10;
            var unparsed = string.IsNullOrWhiteSpace(offset)
                ? baseTime
                : $"{baseTime}{offset}";

            var baseTimeActual = GetBaseTime(baseTime, timeZone);
            var expected = timeZone.ConvertToUtc(addOffsetToBaseTime?.Invoke(baseTimeActual) ?? baseTimeActual);
            Assert.IsTrue(unparsed.TryConvertToUtcDateTime(out var actual, timeZone: timeZone));
            Assert.AreEqual(expected.Ticks, actual.Ticks, delta, $"{unparsed}: Expected ({expected:yyyy-MM-ddTHH:mm:ss.fff}) and actual ({actual:yyyy-MM-ddTHH:mm:ss.fff}) dates differed by more than {delta} ticks.");
        }


        private DateTime GetBaseTime(string baseTime, TimeZoneInfo timeZone) {
            return DateTimeParsingExtensions.GetBaseTime(baseTime, timeZone.GetCurrentTime(), DayOfWeek.Sunday);
        }


        [DataTestMethod]
        // Regular time span liternals
        [DataRow("00:00:00.001", null)]
        [DataRow("00:00:01", null)]
        [DataRow("00:01:00", null)]
        [DataRow("01:00:00", null)]
        [DataRow("1.00:00:00", null)]
        [DataRow("7.00:00:00", null)]
        // Short-hand time span literals
        [DataRow("1MS", "00:00:00.001")]
        [DataRow("1S", "00:00:01")]
        [DataRow("1M", "00:01:00")]
        [DataRow("1H", "01:00:00")]
        [DataRow("1D", "1.00:00:00")]
        [DataRow("1W", "7.00:00:00")]
        public void TimeSpanShouldBeParsedCorrectly(string unparsed, string literal) {
            var expected = string.IsNullOrWhiteSpace(literal)
                ? TimeSpan.Parse(unparsed)
                : TimeSpan.Parse(literal);

            Assert.IsTrue(unparsed.TryConvertToTimeSpan(out var actual));
            Assert.AreEqual(expected, actual);
        }

    }

}
