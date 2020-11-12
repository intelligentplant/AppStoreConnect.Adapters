
using DataCore.Adapter.RealTimeData;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class DataFunctionDescriptorTests : TestsBase {

        [TestMethod]
        public void DataFunctionDescriptorShouldMatchId() {
            var descriptor = new DataFunctionDescriptor(TestContext.TestName, TestContext.FullyQualifiedTestClassName, null);
            Assert.IsTrue(descriptor.IsMatch(TestContext.TestName));
        }



        [TestMethod]
        public void DataFunctionDescriptorShouldMatchName() {
            var descriptor = new DataFunctionDescriptor(TestContext.TestName, TestContext.FullyQualifiedTestClassName, null);
            Assert.IsTrue(descriptor.IsMatch(TestContext.FullyQualifiedTestClassName));
        }


        [TestMethod]
        public void DataFunctionDescriptorShouldMatchAlias() {
            var descriptor = new DataFunctionDescriptor(TestContext.TestName, TestContext.FullyQualifiedTestClassName, null, aliases: new[] { "Alt_Name" });
            Assert.IsTrue(descriptor.IsMatch("Alt_Name"));
        }

    }

}
