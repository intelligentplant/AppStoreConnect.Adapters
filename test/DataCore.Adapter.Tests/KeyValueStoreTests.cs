using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    public abstract class KeyValueStoreTests<T> : TestsBase where T : IKeyValueStore {

        protected abstract T CreateStore(CompressionLevel compressionLevel);


        [DataTestMethod]
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
                await store.WriteJsonAsync(TestContext.TestName, now);
            }
            finally {
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [DataTestMethod]
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
                await store.WriteJsonAsync(TestContext.TestName, now);
                
                var value = await store.ReadJsonAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(now, value);
            }
            finally {
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [DataTestMethod]
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
                await store.WriteJsonAsync(TestContext.TestName, now);
                
                var value = await store.ReadJsonAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(now, value);

                var delete = await store.DeleteAsync(TestContext.TestName);
                Assert.IsTrue(delete);

                var value2 = await store.ReadJsonAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(default(DateTime), value2);
            }
            finally {
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [DataTestMethod]
        [DataRow(CompressionLevel.NoCompression)]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
#if NET6_0_OR_GREATER
        [DataRow(CompressionLevel.SmallestSize)]
#endif
        public async Task ShouldListKeys(CompressionLevel compressionLevel) {
            var store = CreateStore(compressionLevel);
            try {
                var keys = Enumerable.Range(1, 10).Select(x => $"key:{x}").ToArray();

                foreach (var key in keys) {
                    await store.WriteJsonAsync(key, 0);
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


        [DataTestMethod]
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

                await scopedStore.WriteJsonAsync(TestContext.TestName, now);
                
                var value = await scopedStore.ReadJsonAsync<DateTime>(TestContext.TestName);
                Assert.AreEqual(now, value);

                var value2 = await store.ReadJsonAsync<DateTime>("INNER 1:INNER 2:" + TestContext.TestName);
                Assert.AreEqual(now, value2);
            }
            finally {
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [DataTestMethod]
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

                await scopedStore.WriteJsonAsync(TestContext.TestName, now);
                
                var keys = await scopedStore.GetKeysAsStrings().ToEnumerable();
                Assert.IsTrue(keys.Contains(TestContext.TestName));
            }
            finally {
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }


        [DataTestMethod]
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
                if (store is IDisposable disposable) {
                    disposable.Dispose();
                }
            }
        }

    }

}
