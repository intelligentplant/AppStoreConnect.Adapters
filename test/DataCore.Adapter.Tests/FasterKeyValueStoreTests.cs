using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataCore.Adapter.KeyValueStore.FASTER;
using DataCore.Adapter.Services;

using FASTER.core;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class FasterKeyValueStoreTests : KeyValueStoreTests<FasterKeyValueStore> {
        protected override FasterKeyValueStore CreateStore() {
            return new FasterKeyValueStore(new FasterKeyValueStoreOptions());
        }


        [TestMethod]
        public async Task ShouldPersistStateBetweenRestarts() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(FasterKeyValueStoreTests), Guid.NewGuid().ToString()));

            try {
                var now = DateTime.UtcNow;

                using (var store1 = new FasterKeyValueStore(new FasterKeyValueStoreOptions() { 
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                })) {

                    var writeResult = await store1.WriteAsync(TestContext.TestName, now);
                    Assert.AreEqual(KeyValueStoreOperationStatus.OK, writeResult);

                    // Checkpoint should be created when we dispose because we have specified a
                    // checkpoint manager.
                }

                using (var store2 = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                })) {
                    var readResult = await store2.ReadAsync<DateTime>(TestContext.TestName);
                    Assert.AreEqual(KeyValueStoreOperationStatus.OK, readResult.Status);
                    Assert.AreEqual(now, readResult.Value);
                }
            }
            finally {
                tmpPath.Delete(true);
            }
        }


        [TestMethod]
        public async Task ShouldNotCreateCheckpointUnlessDirty() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(FasterKeyValueStoreTests), Guid.NewGuid().ToString()));

            try {
                using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                })) {

                    var writeResult = await store.WriteAsync(TestContext.TestName, DateTime.UtcNow);
                    Assert.AreEqual(KeyValueStoreOperationStatus.OK, writeResult);

                    // Create checkpoint - should succeed
                    var cp1 = await store.TakeFullCheckpointAsync();
                    Assert.IsTrue(cp1);

                    // Create another checkpoint - should fail
                    var cp2 = await store.TakeFullCheckpointAsync();
                    Assert.IsFalse(cp2);

                    writeResult = await store.WriteAsync(TestContext.TestName, DateTime.UtcNow);
                    Assert.AreEqual(KeyValueStoreOperationStatus.OK, writeResult);

                    // Create a final checkpoint - should succeed
                    var cp3 = await store.TakeFullCheckpointAsync();
                    Assert.IsTrue(cp3);
                }
            }
            finally {
                tmpPath.Delete(true);
            }
        }

    }

}
