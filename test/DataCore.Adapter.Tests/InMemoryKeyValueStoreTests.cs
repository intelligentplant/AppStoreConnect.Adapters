
using System.IO.Compression;

using DataCore.Adapter.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class InMemoryKeyValueStoreTests : KeyValueStoreTests<InMemoryKeyValueStore> {

        protected override InMemoryKeyValueStore CreateStore(CompressionLevel compressionLevel, bool enableRawWrites = false) {
            return new InMemoryKeyValueStore();
        }

    }
}
