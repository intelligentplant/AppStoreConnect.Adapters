using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using DataCore.Adapter.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    public abstract class KeyValueStoreTests<T> : TestsBase where T : IKeyValueStore {

        protected abstract T CreateStore(CompressionLevel compressionLevel, bool enableRawWrites = false);


        [TestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
#if NET6_0_OR_GREATER
        [DataRow(CompressionLevel.SmallestSize)]
#endif
        public async Task ShouldWriteValueToStore(CompressionLevel compressionLevel) {
            var now = DateTime.UtcNow;

            var store = CreateStore(compressionLevel);
            try {
                await store.WriteAsync(TestContext.TestName, now);
            }
            finally {
                if (store is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync();
                }
                else if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
#if NET6_0_OR_GREATER
        [DataRow(CompressionLevel.SmallestSize)]
#endif
        public async Task ShouldUpdateValue(CompressionLevel compressionLevel) {
            var before = DateTime.UtcNow.AddDays(-1);
            var after = DateTime.UtcNow;

            var store = CreateStore(compressionLevel);
            try {
                await store.WriteAsync(TestContext.TestName, before);
                var value = await store.ReadAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(before, value);

                await store.WriteAsync(TestContext.TestName, after);
                value = await store.ReadAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(after, value);
            }
            finally {
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        public async Task KeyShouldExistInStore() {
            var now = DateTime.UtcNow;

            var store = CreateStore(CompressionLevel.NoCompression);
            try {
                await store.WriteAsync(TestContext.TestName, now);
                Assert.IsTrue(await store.ExistsAsync(TestContext.TestName));
            }
            finally {
                if (store is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync();
                }
                else if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
#if NET6_0_OR_GREATER
        [DataRow(CompressionLevel.SmallestSize)]
#endif
        public async Task ShouldReadValueFromStore(CompressionLevel compressionLevel) {
            var now = DateTime.UtcNow;

            var store = CreateStore(compressionLevel);
            try {
                await store.WriteAsync(TestContext.TestName, now);
                
                var value = await store.ReadAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(now, value);
            }
            finally {
                if (store is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync();
                }
                else if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
#if NET6_0_OR_GREATER
        [DataRow(CompressionLevel.SmallestSize)]
#endif
        public async Task ShouldDeleteValueFromStore(CompressionLevel compressionLevel) {
            var now = DateTime.UtcNow;

            var store = CreateStore(compressionLevel);
            try {
                await store.WriteAsync(TestContext.TestName, now);
                
                var value = await store.ReadAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(now, value);

                var delete = await store.DeleteAsync(TestContext.TestName);
                Assert.IsTrue(delete);

                var value2 = await store.ReadAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(default(DateTime), value2);
            }
            finally {
                if (store is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync();
                }
                else if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
#if NET6_0_OR_GREATER
        [DataRow(CompressionLevel.SmallestSize)]
#endif
        public async Task ShouldListKeys(CompressionLevel compressionLevel) {
            var store = CreateStore(compressionLevel);
            try {
                var keys = Enumerable.Range(1, 100).Select(x => $"key:{x}").ToArray();

                foreach (var key in keys) {
                    await store.WriteAsync(key, 0);
                }

                var keysActual = await store.GetKeysAsStrings().ToEnumerable();
                Assert.AreEqual(keys.Length, keysActual.Count());

                foreach (var key in keys) {
                    Assert.IsTrue(keysActual.Contains(key), $"Missing key: {key}");
                }
            }
            finally {
                if (store is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync();
                }
                else if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
#if NET6_0_OR_GREATER
        [DataRow(CompressionLevel.SmallestSize)]
#endif
        public async Task ScopedStoreShouldAddKeyPrefix(CompressionLevel compressionLevel) {
            var now = DateTime.UtcNow;

            var store = CreateStore(compressionLevel);
            try {
                var scopedStore = store
                    .CreateScopedStore("INNER 1:")
                    .CreateScopedStore("INNER 2:");

                await scopedStore.WriteAsync(TestContext.TestName, now);
                
                var value = await scopedStore.ReadAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(now, value);

                var value2 = await store.ReadAsync<DateTime>("INNER 1:INNER 2:" + TestContext.TestName);
                Assert.AreEqual(now, value2);
            }
            finally {
                if (store is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync();
                }
                else if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
#if NET6_0_OR_GREATER
        [DataRow(CompressionLevel.SmallestSize)]
#endif
        public async Task ScopedStoreShouldRemoveKeyPrefix(CompressionLevel compressionLevel) {
            var now = DateTime.UtcNow;

            var store = CreateStore(compressionLevel);
            try {
                var scopedStore = store.CreateScopedStore("INNER:");

                await scopedStore.WriteAsync(TestContext.TestName, now);
                
                var keys = await scopedStore.GetKeysAsStrings().ToEnumerable();
                Assert.IsTrue(keys.Contains(TestContext.TestName));
            }
            finally {
                if (store is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync();
                }
                else if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
#if NET6_0_OR_GREATER
        [DataRow(CompressionLevel.SmallestSize)]
#endif
        public async Task ShouldHandleParallelWrites(CompressionLevel compressionLevel) {
            var store = CreateStore(compressionLevel);
            try {
                var tasks = new List<Task>();
                var rng = System.Security.Cryptography.RandomNumberGenerator.Create();

                for (var i = 0; i < 100; i++) {
                    tasks.Add(Task.Run(async () => {
                        var key = new byte[128];
                        var value = new byte[1024];

                        rng.GetNonZeroBytes(key);
                        rng.GetNonZeroBytes(value);

                        await store.WriteAsync(key, value);
                    }));
                }

                await Task.WhenAll(tasks);

                for (var i = 0; i < tasks.Count; i++) {
                    var task = tasks[i];
                    Assert.IsTrue(task.IsCompleted && !task.IsCanceled && !task.IsFaulted, $"Unexpected result for task {i}.");
                }
            }
            finally {
                if (store is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync();
                }
                else if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
#if NET6_0_OR_GREATER
        [DataRow(CompressionLevel.SmallestSize)]
#endif
        public async Task ShouldWriteRawValueToStore(CompressionLevel compressionLevel) {
            var now = DateTime.UtcNow;

            var store = CreateStore(compressionLevel, enableRawWrites: true);
            if (store is not IRawKeyValueStore rawStore) {
                Assert.Inconclusive("Store does not support raw values.");
                return;
            }

            try {
                var raw = JsonSerializer.SerializeToUtf8Bytes(now);
                await rawStore.WriteRawAsync(TestContext.TestName, raw);

                var deserialized = await store.ReadAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(now, deserialized);
            }
            finally {
                if (store is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync();
                }
                else if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
#if NET6_0_OR_GREATER
        [DataRow(CompressionLevel.SmallestSize)]
#endif
        public async Task ShouldReadRawValueFromStore(CompressionLevel compressionLevel) {
            var now = DateTime.UtcNow;

            var store = CreateStore(compressionLevel, enableRawWrites: true);
            if (store is not IRawKeyValueStore rawStore) {
                Assert.Inconclusive("Store does not support raw values.");
                return;
            }

            try {
                var raw = JsonSerializer.SerializeToUtf8Bytes(now);
                await rawStore.WriteRawAsync(TestContext.TestName, raw);

                var raw2 = await rawStore.ReadRawAsync(TestContext.TestName);
                Assert.IsTrue(raw.SequenceEqual(raw2));
            }
            finally {
                if (store is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync();
                }
                else if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        public async Task RawWriteShouldThrowException() {
            var now = DateTime.UtcNow;

            var store = CreateStore(CompressionLevel.NoCompression, enableRawWrites: false);
            if (store is not IRawKeyValueStore rawStore) {
                Assert.Inconclusive("Store does not support raw values.");
                return;
            }

            try {
                var raw = JsonSerializer.SerializeToUtf8Bytes(now);
                await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await rawStore.WriteRawAsync(TestContext.TestName, raw));
            }
            finally {
                if (store is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync();
                }
                else if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [TestMethod]
        public async Task ShouldCopyAllKeysToAnotherStore() {
            var now = DateTime.UtcNow;

            var store1 = CreateStore(CompressionLevel.NoCompression, enableRawWrites: false) as IRawKeyValueStore;
            var store2 = CreateStore(CompressionLevel.NoCompression, enableRawWrites: true) as IRawKeyValueStore;

            if (store1 == null || store2 == null) {
                Assert.Inconclusive("Source or destination store does not support raw writes");
            }

            await store1.WriteAsync(TestContext.TestName, now);
            var count = await store1.BulkCopyToAsync(store2);
            Assert.AreEqual(1, count);

            var readResult = await store2.ReadAsync<DateTime>(TestContext.TestName);
            Assert.AreEqual(now, readResult);
        }


        [TestMethod]
        public async Task ShouldCopyAllKeysFromAnotherStore() {
            var now = DateTime.UtcNow;

            var store1 = CreateStore(CompressionLevel.NoCompression, enableRawWrites: true) as IRawKeyValueStore;
            var store2 = CreateStore(CompressionLevel.NoCompression, enableRawWrites: false) as IRawKeyValueStore;

            if (store1 == null || store2 == null) {
                Assert.Inconclusive("Source or destination store does not support raw writes");
            }

            await store2.WriteAsync(TestContext.TestName, now);
            var count = await store1.BulkCopyFromAsync(store2);
            Assert.AreEqual(1, count);

            var readResult = await store1.ReadAsync<DateTime>(TestContext.TestName);
            Assert.AreEqual(now, readResult);
        }


        [TestMethod]
        public async Task ShouldCopyFilteredKeysToAnotherStore() {
            var now = DateTime.UtcNow;

            var store1 = CreateStore(CompressionLevel.NoCompression, enableRawWrites: false) as IRawKeyValueStore;
            var store2 = CreateStore(CompressionLevel.NoCompression, enableRawWrites: true) as IRawKeyValueStore;

            if (store1 == null || store2 == null) {
                Assert.Inconclusive("Source or destination store does not support raw writes");
            }

            await store1.WriteAsync(TestContext.TestName + ":1:Value", now);
            await store1.WriteAsync(TestContext.TestName + ":2:Value", now);
            var count = await store1.BulkCopyToAsync(store2, TestContext.TestName + ":1");
            Assert.AreEqual(1, count);

            var readResult = await store2.ReadAsync<DateTime>(TestContext.TestName + ":1:Value");
            Assert.AreEqual(now, readResult);
        }


        [TestMethod]
        public async Task ShouldCopyFilteredKeysFromAnotherStore() {
            var now = DateTime.UtcNow;

            var store1 = CreateStore(CompressionLevel.NoCompression, enableRawWrites: true) as IRawKeyValueStore;
            var store2 = CreateStore(CompressionLevel.NoCompression, enableRawWrites: false) as IRawKeyValueStore;

            if (store1 == null || store2 == null) {
                Assert.Inconclusive("Source or destination store does not support raw writes");
            }

            await store2.WriteAsync(TestContext.TestName + ":1:Value", now);
            await store2.WriteAsync(TestContext.TestName + ":2:Value", now);
            var count = await store1.BulkCopyFromAsync(store2, TestContext.TestName + ":1");
            Assert.AreEqual(1, count);

            var readResult = await store1.ReadAsync<DateTime>(TestContext.TestName + ":1:Value");
            Assert.AreEqual(now, readResult);
        }


        [TestMethod]
        public async Task ShouldCopySpecifiedKeysToAnotherStore() {
            var now = DateTime.UtcNow;

            var store1 = CreateStore(CompressionLevel.NoCompression, enableRawWrites: false) as IRawKeyValueStore;
            var store2 = CreateStore(CompressionLevel.NoCompression, enableRawWrites: true) as IRawKeyValueStore;

            if (store1 == null || store2 == null) {
                Assert.Inconclusive("Source or destination store does not support raw writes");
            }

            await store1.WriteAsync(TestContext.TestName, now);
            var count = await store1.CopyToAsync(store2, new KVKey[] { TestContext.TestName });
            Assert.AreEqual(1, count);

            var readResult = await store2.ReadAsync<DateTime>(TestContext.TestName);
            Assert.AreEqual(now, readResult);
        }


        [TestMethod]
        public async Task ShouldCopySpecifiedKeysFromAnotherStore() {
            var now = DateTime.UtcNow;

            var store1 = CreateStore(CompressionLevel.NoCompression, enableRawWrites: true) as IRawKeyValueStore;
            var store2 = CreateStore(CompressionLevel.NoCompression, enableRawWrites: false) as IRawKeyValueStore;

            if (store1 == null || store2 == null) {
                Assert.Inconclusive("Source or destination store does not support raw writes");
            }

            await store2.WriteAsync(TestContext.TestName, now);
            var count = await store1.CopyFromAsync(store2, new KVKey[] { TestContext.TestName });
            Assert.AreEqual(1, count);

            var readResult = await store1.ReadAsync<DateTime>(TestContext.TestName);
            Assert.AreEqual(now, readResult);
        }

    }

}
