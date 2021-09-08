
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class StatusCodeTests : TestsBase {

        [DataTestMethod]
        [DataRow(0x00000000u)]
        [DataRow(0x002F0000u)]
        public void ShouldBeIdentifiedAsGoodQuality(uint code) {
            Assert.IsTrue(((StatusCode) code).IsGood());
        }


        [DataTestMethod]
        [DataRow(0x40000000u)]
        [DataRow(0x40930000u)]
        public void ShouldBeIdentifiedAsUncertainQuality(uint code) {
            Assert.IsTrue(((StatusCode) code).IsUncertain());
        }


        [DataTestMethod]
        [DataRow(0x80000000u)]
        [DataRow(0x80D60000u)]
        public void ShouldBeIdentifiedAsBadQuality(uint code) {
            Assert.IsTrue(((StatusCode) code).IsBad());
        }


        [TestMethod]
        public void ShouldBeDetectedAsRawTagValue() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueStatusCodeFlags.None);
            Assert.IsTrue(code.IsRawTagValue());
        }


        [TestMethod]
        public void ShouldBeDetectedAsCalculatedTagValue() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueStatusCodeFlags.Calculated);
            Assert.IsTrue(code.IsCalculatedTagValue());
        }


        [TestMethod]
        public void ShouldBeDetectedAsInterpolatedValue() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueStatusCodeFlags.Interpolated);
            Assert.IsTrue(code.IsInterpolatedTagValue());
        }


        [TestMethod]
        public void ShouldHavePartialBitSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueStatusCodeFlags.Partial);
            Assert.IsTrue(code.HasFlag(TagValueStatusCodeFlags.Partial));
        }


        [TestMethod]
        public void ShouldHaveExtraDataBitSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueStatusCodeFlags.ExtraData);
            Assert.IsTrue(code.HasFlag(TagValueStatusCodeFlags.ExtraData));
        }


        [TestMethod]
        public void ShouldHaveMultiValueBitSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueStatusCodeFlags.MultiValue);
            Assert.IsTrue(code.HasFlag(TagValueStatusCodeFlags.MultiValue));
        }


        [TestMethod]
        public void ShouldHaveOverflowBitSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueStatusCodeFlags.Overflow);
            Assert.IsTrue(code.HasFlag(TagValueStatusCodeFlags.Overflow));
        }


        [TestMethod]
        public void ShouldHaveLowLimitBitsSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueStatusCodeFlags.LimitLow);
            Assert.IsTrue(code.HasFlag(TagValueStatusCodeFlags.LimitLow));
        }


        [TestMethod]
        public void ShouldHaveHighLimitBitsSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueStatusCodeFlags.LimitHigh);
            Assert.IsTrue(code.HasFlag(TagValueStatusCodeFlags.LimitHigh));
        }


        [TestMethod]
        public void ShouldHaveConstantBitsSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueStatusCodeFlags.LimitConstant);
            Assert.IsTrue(code.HasFlag(TagValueStatusCodeFlags.LimitConstant));
        }

    }

}
