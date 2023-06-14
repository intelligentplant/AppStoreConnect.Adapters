using DataCore.Adapter.Common;
using DataCore.Adapter.Tags;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AdapterDescriptorBuilderTests : TestsBase {

        [TestMethod]
        public void ShouldSetStandardFeature() {
            var descriptor = new AdapterDescriptorBuilder(TestContext.TestName)
                .WithFeature<ITagInfo>()
                .Build();

            Assert.IsTrue(descriptor.HasFeature<ITagInfo>());
        }


        [TestMethod]
        public void ShouldClearStandardFeature() {
            var descriptor = new AdapterDescriptorBuilder(TestContext.TestName)
                .WithFeature<ITagInfo>()
                .ClearFeature<ITagInfo>()
                .Build();

            Assert.IsFalse(descriptor.HasFeature<ITagInfo>());
        }


        [TestMethod]
        public void ShouldSetExtensionFeature() {
            var descriptor = new AdapterDescriptorBuilder(TestContext.TestName)
                .WithFeature<PingPongExtension>()
                .Build();

            Assert.IsTrue(descriptor.HasFeature<PingPongExtension>());
        }


        [TestMethod]
        public void ShouldClearExtensionFeature() {
            var descriptor = new AdapterDescriptorBuilder(TestContext.TestName)
                .WithFeature<PingPongExtension>()
                .ClearFeature<PingPongExtension>()
                .Build();

            Assert.IsFalse(descriptor.HasFeature<PingPongExtension>());
        }

    }

}
