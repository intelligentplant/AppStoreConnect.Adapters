using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataCore.Adapter.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    public abstract class KeyValueStoreTests<T> : TestsBase where T : IKeyValueStore, IDisposable {

        protected abstract T CreateStore();


        [TestMethod]
        public async Task ShouldWriteValueToStore() {
            var now = DateTime.UtcNow;

            using (var store = CreateStore()) {
                var result = await store.WriteAsync(TestContext.TestName, now);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, result);
            }
        }


        [TestMethod]
        public async Task ShouldReadValueFromStore() {
            var now = DateTime.UtcNow;

            using (var store = CreateStore()) {
                var result = await store.WriteAsync(TestContext.TestName, now);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, result);

                var value = await store.ReadAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, value.Status);
                Assert.AreEqual(now, value.Value);
            }
        }


        [TestMethod]
        public async Task ShouldDeleteValueFromStore() {
            var now = DateTime.UtcNow;

            using (var store = CreateStore()) {
                var result = await store.WriteAsync(TestContext.TestName, now);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, result);

                var value = await store.ReadAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, value.Status);
                Assert.AreEqual(now, value.Value);

                var delete = await store.DeleteAsync(TestContext.TestName);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, delete);
                var value2 = await store.ReadAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(KeyValueStoreOperationStatus.NotFound, value2.Status);
                Assert.AreEqual(default(DateTime), value2.Value);
            }
        }


        [TestMethod]
        public async Task ShouldListKeys() {
            using (var store = CreateStore()) {
                var keys = Enumerable.Range(1, 10).Select(x => $"key:{x}").ToArray();

                foreach (var key in keys) {
                    var result = await store.WriteAsync(key, 0);
                    Assert.AreEqual(KeyValueStoreOperationStatus.OK, result);
                }

                var keysActual = store.GetKeysAsStrings().ToArray();
                Assert.AreEqual(keys.Length, keysActual.Length);

                foreach (var key in keys) {
                    Assert.IsTrue(keysActual.Contains(key), $"Missing key: {key}");
                }
            }
        }


        [TestMethod]
        public async Task ScopedStoreShouldAddKeyPrefix() {
            var now = DateTime.UtcNow;

            using (var store = CreateStore()) {
                var scopedStore = store.CreateScopedStore("INNER:");

                var result = await scopedStore.WriteAsync(TestContext.TestName, now);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, result);

                var value = await scopedStore.ReadAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, value.Status);
                Assert.AreEqual(now, value.Value);

                var value2 = await store.ReadAsync<DateTime>("INNER:" + TestContext.TestName);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, value2.Status);
                Assert.AreEqual(now, value2.Value);
            }
        }


        [TestMethod]
        public async Task ScopedStoreShouldRemoveKeyPrefix() {
            var now = DateTime.UtcNow;

            using (var store = CreateStore()) {
                var scopedStore = store.CreateScopedStore("INNER:");

                var result = await scopedStore.WriteAsync(TestContext.TestName, now);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, result);

                var keys = scopedStore.GetKeysAsStrings().ToArray();
                Assert.IsTrue(keys.Contains(TestContext.TestName));
            }
        }

    }

}
