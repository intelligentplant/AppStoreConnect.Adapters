using System;
using System.IO;
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
            if (s_baseDirectory != null) {
                s_baseDirectory.Refresh();
                s_baseDirectory.Delete(true);
            }
        }


        private static SqliteKeyValueStore CreateStore(string fileName) {
            return new SqliteKeyValueStore(new SqliteKeyValueStoreOptions() { 
                ConnectionString = $"Data Source={fileName};Cache=Shared"
            });
        }


        private static string GetDatabaseFileName() {
            return Path.Combine(s_baseDirectory.FullName, Guid.NewGuid().ToString());
        }


        protected override SqliteKeyValueStore CreateStore() {
            return CreateStore(GetDatabaseFileName());
        }


        [TestMethod]
        public async Task ShouldShareDataBetweenStores() {
            var now = DateTime.UtcNow;
            var path = GetDatabaseFileName();

            var store1 = CreateStore(path);
            var writeResult = await store1.WriteJsonAsync(TestContext.TestName, now);

            Assert.AreEqual(KeyValueStoreOperationStatus.OK, writeResult);

            var store2 = CreateStore(path);
            var readResult = await store2.ReadJsonAsync<DateTime>(TestContext.TestName);

            Assert.AreEqual(KeyValueStoreOperationStatus.OK, readResult.Status);
            Assert.AreEqual(now, readResult.Value);

            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(FasterKeyValueStoreTests), Guid.NewGuid().ToString()));
        }

    }

}
