using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

using DataCore.Adapter.KeyValueStore.FileSystem;
using DataCore.Adapter.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class FileSystemKeyValueStoreTests : KeyValueStoreTests<FileSystemKeyValueStore> {

        private static DirectoryInfo s_baseDirectory;


        [ClassInitialize]
        public static void ClassInit(TestContext context) {
            s_baseDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            s_baseDirectory.Create();
        }


        [ClassCleanup]
        public static void ClassCleanup() {
            if (s_baseDirectory != null) {
                s_baseDirectory.Refresh();
                s_baseDirectory.Delete(true);
            }
        }


        private static FileSystemKeyValueStore CreateStore(string baseDirectory, CompressionLevel compressionLevel) {
            return new FileSystemKeyValueStore(new FileSystemKeyValueStoreOptions() {
                Path = baseDirectory,
                CompressionLevel = compressionLevel
            });
        }


        protected override FileSystemKeyValueStore CreateStore(CompressionLevel compressionLevel, bool enableRawWrites = false) {
            return CreateStore(Path.Combine(s_baseDirectory.FullName, Guid.NewGuid().ToString()), compressionLevel);
        }


        [DataTestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
#if NET6_0_OR_GREATER
        [DataRow(CompressionLevel.SmallestSize)]
#endif
        public async Task ShouldShareDataBetweenStores(CompressionLevel compressionLevel) {
            var now = DateTime.UtcNow;
            var baseDir = Path.Combine(s_baseDirectory.FullName, Guid.NewGuid().ToString());

            var store1 = CreateStore(baseDir, compressionLevel);
            await ((IKeyValueStore) store1).WriteAsync(TestContext.TestName, now);

            var store2 = CreateStore(baseDir, compressionLevel);
            var readResult = await ((IKeyValueStore) store2).ReadAsync<DateTime>(TestContext.TestName);

            Assert.AreEqual(now, readResult);

            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(FasterKeyValueStoreTests), Guid.NewGuid().ToString()));
        }

    }

}
