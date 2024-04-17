using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DataCore.Adapter.KeyValueStore.FASTER;
using DataCore.Adapter.RealTimeData;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class SnapshotTagValueManagerTests : TestsBase {

        [TestMethod]
        public async Task ShouldReturnCachedValuesByTagName() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(SnapshotTagValueManagerTests), Guid.NewGuid().ToString()));
            try {
                var val1 = new TagValueBuilder().WithValue(99.999).Build();
                var val2 = new TagValueBuilder().WithValue(Guid.NewGuid().ToString()).Build();

                await using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var stvm = ActivatorUtilities.CreateInstance<SnapshotTagValueManager>(AssemblyInitializer.ApplicationServices, new SnapshotTagValueManagerOptions() { 
                    AdapterId = TestContext.TestName
                })) {
                    await stvm.ValueReceived(new TagValueQueryResult($"id-{nameof(val1)}", $"name-{nameof(val1)}", val1));
                    await stvm.ValueReceived(new TagValueQueryResult($"id-{nameof(val2)}", $"name-{nameof(val2)}", val2));

                    var valsActual = await stvm.ReadSnapshotTagValues(
                        new DefaultAdapterCallContext(),
                        new ReadSnapshotTagValuesRequest() {
                            Tags = new[] {
                                $"name-{nameof(val1)}",
                                $"name-{nameof(val2)}"
                            }
                        },
                        default
                    ).ToEnumerable(default).ConfigureAwait(false);

                    Assert.AreEqual(2, valsActual.Count());

                    var actualVal1 = valsActual.FirstOrDefault(x => string.Equals(x.TagName, $"name-{nameof(val1)}"));
                    Assert.IsNotNull(actualVal1);
                    Assert.AreEqual(val1.UtcSampleTime, actualVal1.Value.UtcSampleTime);
                    Assert.AreEqual(val1.Value, actualVal1.Value.Value);

                    var actualVal2 = valsActual.FirstOrDefault(x => string.Equals(x.TagName, $"name-{nameof(val2)}"));
                    Assert.IsNotNull(actualVal2);
                    Assert.AreEqual(val2.UtcSampleTime, actualVal2.Value.UtcSampleTime);
                    Assert.AreEqual(val2.Value, actualVal2.Value.Value);
                }
            }
            finally {
                tmpPath.Delete();
            }
        }


        [TestMethod]
        public async Task ShouldReturnCachedValuesByTagId() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(SnapshotTagValueManagerTests), Guid.NewGuid().ToString()));
            try {
                var val1 = new TagValueBuilder().WithValue(99.999).Build();
                var val2 = new TagValueBuilder().WithValue(Guid.NewGuid().ToString()).Build();

                await using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var stvm = ActivatorUtilities.CreateInstance<SnapshotTagValueManager>(AssemblyInitializer.ApplicationServices, new SnapshotTagValueManagerOptions() {
                    AdapterId = TestContext.TestName
                })) {
                    await stvm.ValueReceived(new TagValueQueryResult($"id-{nameof(val1)}", $"name-{nameof(val1)}", val1));
                    await stvm.ValueReceived(new TagValueQueryResult($"id-{nameof(val2)}", $"name-{nameof(val2)}", val2));

                    var valsActual = await stvm.ReadSnapshotTagValues(
                        new DefaultAdapterCallContext(),
                        new ReadSnapshotTagValuesRequest() {
                            Tags = new[] {
                                $"id-{nameof(val1)}",
                                $"id-{nameof(val2)}"
                            }
                        },
                        default
                    ).ToEnumerable(default).ConfigureAwait(false);

                    Assert.AreEqual(2, valsActual.Count());

                    var actualVal1 = valsActual.FirstOrDefault(x => string.Equals(x.TagId, $"id-{nameof(val1)}"));
                    Assert.IsNotNull(actualVal1);
                    Assert.AreEqual(val1.UtcSampleTime, actualVal1.Value.UtcSampleTime);
                    Assert.AreEqual(val1.Value, actualVal1.Value.Value);

                    var actualVal2 = valsActual.FirstOrDefault(x => string.Equals(x.TagId, $"id-{nameof(val2)}"));
                    Assert.IsNotNull(actualVal2);
                    Assert.AreEqual(val2.UtcSampleTime, actualVal2.Value.UtcSampleTime);
                    Assert.AreEqual(val2.Value, actualVal2.Value.Value);
                }
            }
            finally {
                tmpPath.Delete();
            }
        }


        [TestMethod]
        public async Task ShouldReturnCachedValuesByTagNameAfterRestore() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(SnapshotTagValueManagerTests), Guid.NewGuid().ToString()));
            try {
                var val1 = new TagValueBuilder().WithValue(99.999).Build();
                var val2 = new TagValueBuilder().WithValue(Guid.NewGuid().ToString()).Build();

                await using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var stvm = ActivatorUtilities.CreateInstance<SnapshotTagValueManager>(AssemblyInitializer.ApplicationServices, new SnapshotTagValueManagerOptions() {
                    AdapterId = TestContext.TestName
                })) {
                    await stvm.ValueReceived(new TagValueQueryResult($"id-{nameof(val1)}", $"name-{nameof(val1)}", val1));
                    await stvm.ValueReceived(new TagValueQueryResult($"id-{nameof(val2)}", $"name-{nameof(val2)}", val2));
                }


                await using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var stvm = ActivatorUtilities.CreateInstance<SnapshotTagValueManager>(AssemblyInitializer.ApplicationServices, new SnapshotTagValueManagerOptions() {
                    AdapterId = TestContext.TestName
                })) {
                    var valsActual = await stvm.ReadSnapshotTagValues(
                        new DefaultAdapterCallContext(),
                        new ReadSnapshotTagValuesRequest() {
                            Tags = new[] {
                                $"name-{nameof(val1)}",
                                $"name-{nameof(val2)}"
                            }
                        },
                        default
                    ).ToEnumerable(default).ConfigureAwait(false);

                    Assert.AreEqual(2, valsActual.Count());

                    var actualVal1 = valsActual.FirstOrDefault(x => string.Equals(x.TagName, $"name-{nameof(val1)}"));
                    Assert.IsNotNull(actualVal1);
                    Assert.AreEqual(val1.UtcSampleTime, actualVal1.Value.UtcSampleTime);
                    Assert.AreEqual(val1.Value, actualVal1.Value.Value);

                    var actualVal2 = valsActual.FirstOrDefault(x => string.Equals(x.TagName, $"name-{nameof(val2)}"));
                    Assert.IsNotNull(actualVal2);
                    Assert.AreEqual(val2.UtcSampleTime, actualVal2.Value.UtcSampleTime);
                    Assert.AreEqual(val2.Value, actualVal2.Value.Value);
                }
            }
            finally {
                tmpPath.Delete();
            }
        }


        [TestMethod]
        public async Task ShouldReturnCachedValuesByTagIdAfterRestore() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(SnapshotTagValueManagerTests), Guid.NewGuid().ToString()));
            try {
                var val1 = new TagValueBuilder().WithValue(99.999).Build();
                var val2 = new TagValueBuilder().WithValue(Guid.NewGuid().ToString()).Build();

                await using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var stvm = ActivatorUtilities.CreateInstance<SnapshotTagValueManager>(AssemblyInitializer.ApplicationServices, new SnapshotTagValueManagerOptions() {
                    AdapterId = TestContext.TestName
                })) {
                    await stvm.ValueReceived(new TagValueQueryResult($"id-{nameof(val1)}", $"name-{nameof(val1)}", val1));
                    await stvm.ValueReceived(new TagValueQueryResult($"id-{nameof(val2)}", $"name-{nameof(val2)}", val2));
                }


                await using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var stvm = ActivatorUtilities.CreateInstance<SnapshotTagValueManager>(AssemblyInitializer.ApplicationServices, new SnapshotTagValueManagerOptions() {
                    AdapterId = TestContext.TestName
                })) {
                    var valsActual = await stvm.ReadSnapshotTagValues(
                        new DefaultAdapterCallContext(),
                        new ReadSnapshotTagValuesRequest() {
                            Tags = new[] {
                                $"id-{nameof(val1)}",
                                $"id-{nameof(val2)}"
                            }
                        },
                        default
                    ).ToEnumerable(default).ConfigureAwait(false);

                    Assert.AreEqual(2, valsActual.Count());

                    var actualVal1 = valsActual.FirstOrDefault(x => string.Equals(x.TagId, $"id-{nameof(val1)}"));
                    Assert.IsNotNull(actualVal1);
                    Assert.AreEqual(val1.UtcSampleTime, actualVal1.Value.UtcSampleTime);
                    Assert.AreEqual(val1.Value, actualVal1.Value.Value);

                    var actualVal2 = valsActual.FirstOrDefault(x => string.Equals(x.TagId, $"id-{nameof(val2)}"));
                    Assert.IsNotNull(actualVal2);
                    Assert.AreEqual(val2.UtcSampleTime, actualVal2.Value.UtcSampleTime);
                    Assert.AreEqual(val2.Value, actualVal2.Value.Value);
                }
            }
            finally {
                tmpPath.Delete();
            }
        }

    }

}
