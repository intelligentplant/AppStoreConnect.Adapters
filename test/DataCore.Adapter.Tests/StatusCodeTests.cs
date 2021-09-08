
using DataCore.Adapter.Common;

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
            var code = new StatusCode(0, 0, 1, (ushort) TagValueInfoBits.None);
            Assert.IsTrue(code.IsRawTagValue());
        }


        [TestMethod]
        public void ShouldBeDetectedAsCalculatedTagValue() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueInfoBits.Calculated);
            Assert.IsTrue(code.IsCalculatedTagValue());
        }


        [TestMethod]
        public void ShouldBeDetectedAsInterpolatedValue() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueInfoBits.Interpolated);
            Assert.IsTrue(code.IsInterpolatedTagValue());
        }


        [TestMethod]
        public void ShouldHavePartialBitSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueInfoBits.Partial);
            Assert.IsTrue(code.HasFlag(TagValueInfoBits.Partial));
        }


        [TestMethod]
        public void ShouldHaveExtraDataBitSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueInfoBits.ExtraData);
            Assert.IsTrue(code.HasFlag(TagValueInfoBits.ExtraData));
        }


        [TestMethod]
        public void ShouldHaveMultiValueBitSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueInfoBits.MultiValue);
            Assert.IsTrue(code.HasFlag(TagValueInfoBits.MultiValue));
        }


        [TestMethod]
        public void ShouldHaveOverflowBitSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueInfoBits.Overflow);
            Assert.IsTrue(code.HasFlag(TagValueInfoBits.Overflow));
        }


        [TestMethod]
        public void ShouldHaveLowLimitBitsSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueInfoBits.LimitLow);
            Assert.IsTrue(code.HasFlag(TagValueInfoBits.LimitLow));
        }


        [TestMethod]
        public void ShouldHaveHighLimitBitsSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueInfoBits.LimitHigh);
            Assert.IsTrue(code.HasFlag(TagValueInfoBits.LimitHigh));
        }


        [TestMethod]
        public void ShouldHaveConstantBitsSet() {
            var code = new StatusCode(0, 0, 1, (ushort) TagValueInfoBits.LimitConstant);
            Assert.IsTrue(code.HasFlag(TagValueInfoBits.LimitConstant));
        }

    }

}
