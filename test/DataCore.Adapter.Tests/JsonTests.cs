using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using DataCore.Adapter.Common;
using DataCore.Adapter.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class JsonTests {

        private static JsonSerializerOptions GetOptions() {
            var result = new JsonSerializerOptions();
            result.Converters.AddAdapterConverters();

            return result;
        }


        private void RoundTripTest<T>(T value, JsonSerializerOptions options) {
            var variant = Variant.FromValue(value);
            var json = JsonSerializer.Serialize(variant, options);

            var deserialized = JsonSerializer.Deserialize<Variant>(json, options);
            Assert.AreEqual(variant.Type, deserialized.Type);
            Assert.AreEqual(variant.Value, deserialized.Value);
        }


        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Variant_BooleanShouldRoundTrip(bool value) {
            var options = GetOptions();
            RoundTripTest(value, options);
        }


        [DataTestMethod]
        [DataRow(byte.MinValue)]
        [DataRow(byte.MaxValue)]
        public void Variant_ByteShouldRoundTrip(byte value) {
            var options = GetOptions();
            RoundTripTest(value, options);
        }


        [TestMethod]
        public void Variant_DateTimeShouldRoundTrip() {
            var now = DateTime.UtcNow;
            var options = GetOptions();
            RoundTripTest(now, options);
        }


        [DataTestMethod]
        [DataRow(double.MinValue)]
        [DataRow(double.MaxValue)]
        public void Variant_DoubleShouldRoundTrip(double val) {
            var options = GetOptions();
            RoundTripTest(val, options);
        }


        [DataTestMethod]
        [DataRow(float.MinValue)]
        [DataRow(float.MaxValue)]
        public void Variant_FloatShouldRoundTrip(float val) {
            var options = GetOptions();
            RoundTripTest(val, options);
        }


        [DataTestMethod]
        [DataRow(short.MinValue)]
        [DataRow(short.MaxValue)]
        public void Variant_Int16ShouldRoundTrip(short value) {
            var options = GetOptions();
            RoundTripTest(value, options);
        }


        [DataTestMethod]
        [DataRow(int.MinValue)]
        [DataRow(int.MaxValue)]
        public void Variant_Int32ShouldRoundTrip(int value) {
            var options = GetOptions();
            RoundTripTest(value, options);
        }


        [DataTestMethod]
        [DataRow(long.MinValue)]
        [DataRow(long.MaxValue)]
        public void Variant_Int64ShouldRoundTrip(long value) {
            var options = GetOptions();
            RoundTripTest(value, options);
        }


        [DataTestMethod]
        [DataRow(sbyte.MinValue)]
        [DataRow(sbyte.MaxValue)]
        public void Variant_SByteShouldRoundTrip(sbyte value) {
            var options = GetOptions();
            RoundTripTest(value, options);
        }


        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("TEST")]
        [DataRow("контрольная работа")] // "test" in Russian
        [DataRow("テスト")] // "test" in Japanese
        [DataRow("測試")] // "test" in Chinese
        [DataRow("اختبار")] // "test" in Arabic
        public void Variant_StringShouldRoundTrip(string value) {
            var options = GetOptions();
            RoundTripTest(value, options);
        }


        [TestMethod]
        public void Variant_TimeSpanShouldRoundTrip() {
            var options = GetOptions();
            RoundTripTest(TimeSpan.FromDays(1.234), options);
        }


        [DataTestMethod]
        [DataRow(ushort.MinValue)]
        [DataRow(ushort.MaxValue)]
        public void Variant_UInt16ShouldRoundTrip(ushort value) {
            var options = GetOptions();
            RoundTripTest(value, options);
        }


        [DataTestMethod]
        [DataRow(uint.MinValue)]
        [DataRow(uint.MaxValue)]
        public void Variant_UInt32ShouldRoundTrip(uint value) {
            var options = GetOptions();
            RoundTripTest(value, options);
        }


        [DataTestMethod]
        [DataRow(ulong.MinValue)]
        [DataRow(ulong.MaxValue)]
        public void Variant_UInt64ShouldRoundTrip(ulong value) {
            var options = GetOptions();
            RoundTripTest(value, options);
        }

    }

}
