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


        private static SqliteKeyValueStore CreateStore(string fileName, CompressionLevel compressionLevel) {
            return new SqliteKeyValueStore(new SqliteKeyValueStoreOptions() { 
                ConnectionString = $"Data Source={fileName};Cache=Shared",
                CompressionLevel = compressionLevel
            });
        }


        private static string GetDatabaseFileName() {
            return Path.Combine(s_baseDirectory.FullName, Guid.NewGuid().ToString());
        }


        protected override SqliteKeyValueStore CreateStore(CompressionLevel compressionLevel) {
            return CreateStore(GetDatabaseFileName(), compressionLevel);
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

            var store1 = CreateStore(path, compressionLevel);
            await store1.WriteJsonAsync(TestContext.TestName, now);

            var store2 = CreateStore(path, compressionLevel);
            var readResult = await store2.ReadJsonAsync<DateTime>(TestContext.TestName);

            Assert.AreEqual(now, readResult);

            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(FasterKeyValueStoreTests), Guid.NewGuid().ToString()));
        }

    }

}
