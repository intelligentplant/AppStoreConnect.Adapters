
using DataCore.Adapter.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class InMemoryKeyValueStoreTests : KeyValueStoreTests<InMemoryKeyValueStore> {

        protected override InMemoryKeyValueStore CreateStore() {
            return new InMemoryKeyValueStore();
        }

    }
}
