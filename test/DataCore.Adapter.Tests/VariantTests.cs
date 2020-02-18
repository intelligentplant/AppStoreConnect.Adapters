using System;
using DataCore.Adapter.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class VariantTests : TestsBase {

        [DataTestMethod]
        [DataRow(VariantType.Boolean, "true", true)]
        [DataRow(VariantType.Boolean, "false", false)]
        [DataRow(VariantType.Byte, "0", (byte) 0)]
        [DataRow(VariantType.Byte, "255", (byte) 255)]
        [DataRow(VariantType.DateTime, "2020-02-01T09:05:00", null)]
        [DataRow(VariantType.Double, "1234567890.12345", 1234567890.12345)]
        [DataRow(VariantType.Float, "12345.789", 12345.789f)]
        [DataRow(VariantType.Int16, "-32768", (short) -32768)]
        [DataRow(VariantType.Int16, "32767", (short) 32767)]
        [DataRow(VariantType.Int32, "-2147483648", -2147483648)]
        [DataRow(VariantType.Int32, "2147483647", 2147483647)]
        [DataRow(VariantType.Int64, "-9223372036854775808", -9223372036854775808)]
        [DataRow(VariantType.Int64, "9223372036854775807", 9223372036854775807)]
        [DataRow(VariantType.Null, null, null)]
        [DataRow(VariantType.Object, "test", "test")]
        [DataRow(VariantType.SByte, "-128", (sbyte) -128)]
        [DataRow(VariantType.SByte, "127", (sbyte) 127)]
        [DataRow(VariantType.String, "test", "test")]
        [DataRow(VariantType.TimeSpan, "1.23:45:06", null)]
        [DataRow(VariantType.UInt16, "0", (ushort) 0)]
        [DataRow(VariantType.UInt16, "65535", (ushort) 65535)]
        [DataRow(VariantType.UInt32, "0", (uint) 0)]
        [DataRow(VariantType.UInt32, "4294967295", (uint) 4294967295)]
        [DataRow(VariantType.UInt64, "0", (ulong) 0)]
        [DataRow(VariantType.UInt64, "18446744073709551615", (ulong) 18446744073709551615)]
        [DataRow(VariantType.Unknown, "true", true)]
        [DataRow(VariantType.Unknown, "1", 1)]
        [DataRow(VariantType.Unknown, "1.2345", 1.2345)]
        public void ParseToVariantShouldSucceed(VariantType type, string value, object expectedValue) {
            Assert.IsTrue(Variant.TryParse(value, type, out var variant));

            if (type != VariantType.Unknown) {
                Assert.AreEqual(type, variant.Type);
            }

            if (expectedValue == null) {
                switch (type) {
                    case VariantType.DateTime:
                        expectedValue = GetExpectedDateTimeValue(value);
                        break;
                    case VariantType.TimeSpan:
                        expectedValue = GetExpectedTimeSpanValue(value);
                        break;
                }
            }

            if (type == VariantType.Null || expectedValue != null) {
                Assert.AreEqual(expectedValue, variant.Value);
            }
        }


        private DateTime GetExpectedDateTimeValue(string value) {
            return DateTime.Parse(value, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
        }


        private TimeSpan GetExpectedTimeSpanValue(string value) {
            return TimeSpan.Parse(value);
        }

    }

}
