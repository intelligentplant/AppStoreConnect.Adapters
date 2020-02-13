using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    /// <summary>
    /// Base class for other test classes to inherit from.
    /// </summary>
    public abstract class TestsBase {

        /// <summary>
        /// The test context for the current test.
        /// </summary>
        public TestContext TestContext { get; set; }

    }
}
