using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

using DataCore.Adapter.KeyValueStore.Sqlite;
using DataCore.Adapter.Services;

using Microsoft.Extensions.DependencyInjection;
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


        private static SqliteKeyValueStore CreateStore(string fileName, CompressionLevel compressionLevel, bool enableRawWrites, TimeSpan flushInterval) {
            return ActivatorUtilities.CreateInstance<SqliteKeyValueStore>(AssemblyInitializer.ApplicationServices, new SqliteKeyValueStoreOptions() { 
                ConnectionString = $"Data Source={fileName};Cache=Shared",
                CompressionLevel = compressionLevel,
                EnableRawWrites = enableRawWrites,
                FlushInterval = flushInterval
            });
        }


        private static string GetDatabaseFileName() {
            return Path.Combine(s_baseDirectory.FullName, Guid.NewGuid().ToString());
        }


        protected override SqliteKeyValueStore CreateStore(CompressionLevel compressionLevel, bool enableRawWrites = false) {
            return CreateStore(GetDatabaseFileName(), compressionLevel, enableRawWrites, TimeSpan.Zero);
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

            var store1 = CreateStore(path, compressionLevel, false, TimeSpan.Zero);
            await ((IKeyValueStore) store1).WriteAsync(TestContext.TestName, now);

            var store2 = CreateStore(path, compressionLevel, false, TimeSpan.Zero);
            var readResult = await ((IKeyValueStore) store2).ReadAsync<DateTime>(TestContext.TestName);

            Assert.AreEqual(now, readResult);
        }


        [TestMethod]
        public async Task ShouldFlushAtConfiguredInterval() {
            var now = DateTime.UtcNow;
            var path = GetDatabaseFileName();

            using var store = CreateStore(path, CompressionLevel.NoCompression, false, TimeSpan.FromMilliseconds(100));

            await ((IKeyValueStore) store).WriteAsync(TestContext.TestName, now);
            CancelAfter(TimeSpan.FromSeconds(5));
            await store.WaitForNextFlushAsync(CancellationToken);

            var readResult = await ((IKeyValueStore) store).ReadAsync<DateTime>(TestContext.TestName);
            Assert.AreEqual(now, readResult);
        }


        [TestMethod]
        public async Task ShouldFlushManually() {
            var now = DateTime.UtcNow;
            var path = GetDatabaseFileName();

            using var store = CreateStore(path, CompressionLevel.NoCompression, false, TimeSpan.FromSeconds(60));

            await ((IKeyValueStore) store).WriteAsync(TestContext.TestName, now);
            _ = Task.Run(async () => {
                await Task.Delay(50);
                await store.FlushAsync();
            });
            CancelAfter(TimeSpan.FromSeconds(5));
            await store.WaitForNextFlushAsync(CancellationToken);

            var readResult = await ((IKeyValueStore) store).ReadAsync<DateTime>(TestContext.TestName);
            Assert.AreEqual(now, readResult);
        }

    }

}
