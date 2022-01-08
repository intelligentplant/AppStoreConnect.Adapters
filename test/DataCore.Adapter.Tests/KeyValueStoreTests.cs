using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    public abstract class KeyValueStoreTests<T> : TestsBase where T : IKeyValueStore {

        protected abstract T CreateStore();


        [TestMethod]
        public async Task ShouldWriteValueToStore() {
            var now = DateTime.UtcNow;

            var store = CreateStore();
            try {
                var result = await store.WriteJsonAsync(TestContext.TestName, now);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, result);
            }
            finally {
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        public async Task ShouldReadValueFromStore() {
            var now = DateTime.UtcNow;

            var store = CreateStore();
            try {
                var result = await store.WriteJsonAsync(TestContext.TestName, now);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, result);

                var value = await store.ReadJsonAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, value.Status);
                Assert.AreEqual(now, value.Value);
            }
            finally {
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        public async Task ShouldDeleteValueFromStore() {
            var now = DateTime.UtcNow;

            var store = CreateStore();
            try {
                var result = await store.WriteJsonAsync(TestContext.TestName, now);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, result);

                var value = await store.ReadJsonAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, value.Status);
                Assert.AreEqual(now, value.Value);

                var delete = await store.DeleteAsync(TestContext.TestName);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, delete);
                var value2 = await store.ReadJsonAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(KeyValueStoreOperationStatus.NotFound, value2.Status);
                Assert.AreEqual(default(DateTime), value2.Value);
            }
            finally {
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        public async Task ShouldListKeys() {
            var store = CreateStore();
            try {
                var keys = Enumerable.Range(1, 10).Select(x => $"key:{x}").ToArray();

                foreach (var key in keys) {
                    var result = await store.WriteJsonAsync(key, 0);
                    Assert.AreEqual(KeyValueStoreOperationStatus.OK, result);
                }

                var keysActual = await store.GetKeysAsStrings().ToEnumerable();
                Assert.AreEqual(keys.Length, keysActual.Count());

                foreach (var key in keys) {
                    Assert.IsTrue(keysActual.Contains(key), $"Missing key: {key}");
                }
            }
            finally {
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        public async Task ScopedStoreShouldAddKeyPrefix() {
            var now = DateTime.UtcNow;

            var store = CreateStore();
            try {
                var scopedStore = store
                    .CreateScopedStore("INNER 1:")
                    .CreateScopedStore("INNER 2:");

                var result = await scopedStore.WriteJsonAsync(TestContext.TestName, now);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, result);

                var value = await scopedStore.ReadJsonAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, value.Status);
                Assert.AreEqual(now, value.Value);

                var value2 = await store.ReadJsonAsync<DateTime>("INNER 1:INNER 2:" + TestContext.TestName);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, value2.Status);
                Assert.AreEqual(now, value2.Value);
            }
            finally {
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        public async Task ScopedStoreShouldRemoveKeyPrefix() {
            var now = DateTime.UtcNow;

            var store = CreateStore();
            try {
                var scopedStore = store.CreateScopedStore("INNER:");

                var result = await scopedStore.WriteJsonAsync(TestContext.TestName, now);
                Assert.AreEqual(KeyValueStoreOperationStatus.OK, result);

                var keys = await scopedStore.GetKeysAsStrings().ToEnumerable();
                Assert.IsTrue(keys.Contains(TestContext.TestName));
            }
            finally {
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        public async Task ShouldHandleParallelWrites() {
            var store = CreateStore();
            try {
                var tasks = new List<Task<KeyValueStoreOperationStatus>>();
                var rng = System.Security.Cryptography.RandomNumberGenerator.Create();

                for (var i = 0; i < 100; i++) {
                    tasks.Add(Task.Run(async () => {
                        var key = new byte[128];
                        var value = new byte[1024];

                        rng.GetNonZeroBytes(key);
                        rng.GetNonZeroBytes(value);

                        return await store.WriteAsync(key, value);
                    }));
                }

                await Task.WhenAll(tasks);

                for (var i = 0; i < tasks.Count; i++) {
                    var task = tasks[i];
                    Assert.AreEqual(KeyValueStoreOperationStatus.OK, task.Result, $"Unexpected result for task {i}.");
                }
            }
            finally {
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }

    }

}
