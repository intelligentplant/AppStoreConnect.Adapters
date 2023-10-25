using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

using DataCore.Adapter.KeyValueStore.Sqlite;
using DataCore.Adapter.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class SqliteKeyValueStoreTests : KeyValueStoreTests<SqliteKeyValueStore> {

        private static DirectoryInfo s_baseDirectory;


        [ClassInitialize]
        public static void ClassInit(TestContext context) {
            s_baseDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            s_baseDirectory.Create();
        }


        [ClassCleanup]
        public static void ClassCleanup() {
            try {
                s_baseDirectory.Refresh();
                if (s_baseDirectory.Exists) {
                    s_baseDirectory.Delete(true);
                }
            }
            catch {
                // Suppress
            }
        }


        private static SqliteKeyValueStore CreateStore(string fileName, CompressionLevel compressionLevel, bool enableRawWrites) {
            return new SqliteKeyValueStore(new SqliteKeyValueStoreOptions() { 
                ConnectionString = $"Data Source={fileName};Cache=Shared",
                CompressionLevel = compressionLevel,
                EnableRawWrites = enableRawWrites
            });
        }


        private static string GetDatabaseFileName() {
            return Path.Combine(s_baseDirectory.FullName, Guid.NewGuid().ToString());
        }


        protected override SqliteKeyValueStore CreateStore(CompressionLevel compressionLevel, bool enableRawWrites = false) {
            return CreateStore(GetDatabaseFileName(), compressionLevel, enableRawWrites);
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
            var path = GetDatabaseFileName();

            var store1 = CreateStore(path, compressionLevel, false);
            await ((IKeyValueStore) store1).WriteAsync(TestContext.TestName, now);

            var store2 = CreateStore(path, compressionLevel, false);
            var readResult = await ((IKeyValueStore) store2).ReadAsync<DateTime>(TestContext.TestName);

            Assert.AreEqual(now, readResult);

            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(FasterKeyValueStoreTests), Guid.NewGuid().ToString()));
        }

    }

}
