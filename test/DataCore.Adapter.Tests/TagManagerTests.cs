using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.KeyValueStore.FASTER;
using DataCore.Adapter.Tags;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class TagManagerTests : TestsBase {

        [TestMethod]
        public async Task ShouldGetTagByName() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(SnapshotTagValueManagerTests), Guid.NewGuid().ToString()));
            try {
                var tag1 = new TagDefinitionBuilder().WithId(Guid.NewGuid().ToString()).WithName(TestContext.TestName +  "-1").Build();

                using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var tm = ActivatorUtilities.CreateInstance<TagManager>(AssemblyInitializer.ApplicationServices, (IEnumerable<AdapterProperty>) Array.Empty<AdapterProperty>())) {
                    await tm.InitAsync();
                    await tm.AddOrUpdateTagAsync(tag1);

                    var tags = await tm.GetTags(new DefaultAdapterCallContext(), new GetTagsRequest() { 
                        Tags = new [] { tag1.Name }
                    }, default).ToEnumerable();

                    Assert.AreEqual(1, tags.Count());
                    
                    var tagActual = tags.First();
                    Assert.AreEqual(tag1.Id, tagActual.Id);
                    Assert.AreEqual(tag1.Name, tagActual.Name);
                }
            }
            finally {
                tmpPath.Delete();
            }
        }


        [TestMethod]
        public async Task ShouldGetTagById() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(SnapshotTagValueManagerTests), Guid.NewGuid().ToString()));
            try {
                var tag1 = new TagDefinitionBuilder().WithId(Guid.NewGuid().ToString()).WithName(TestContext.TestName + "-1").Build();

                using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var tm = ActivatorUtilities.CreateInstance<TagManager>(AssemblyInitializer.ApplicationServices, (IEnumerable<AdapterProperty>) Array.Empty<AdapterProperty>())) {
                    await tm.InitAsync();
                    await tm.AddOrUpdateTagAsync(tag1);

                    var tags = await tm.GetTags(new DefaultAdapterCallContext(), new GetTagsRequest() {
                        Tags = new[] { tag1.Id }
                    }, default).ToEnumerable();

                    Assert.AreEqual(1, tags.Count());

                    var tagActual = tags.First();
                    Assert.AreEqual(tag1.Id, tagActual.Id);
                    Assert.AreEqual(tag1.Name, tagActual.Name);
                }
            }
            finally {
                tmpPath.Delete();
            }
        }


        [TestMethod]
        public async Task ShouldFindTags() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(SnapshotTagValueManagerTests), Guid.NewGuid().ToString()));
            try {
                var tag1 = new TagDefinitionBuilder().WithId(Guid.NewGuid().ToString()).WithName(TestContext.TestName + "-1").Build();
                var tag2 = new TagDefinitionBuilder().WithId(Guid.NewGuid().ToString()).WithName(TestContext.TestName + "-2").Build();

                using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var tm = ActivatorUtilities.CreateInstance<TagManager>(AssemblyInitializer.ApplicationServices, (IEnumerable<AdapterProperty>) Array.Empty<AdapterProperty>())) {
                    await tm.InitAsync();
                    await tm.AddOrUpdateTagAsync(tag1);
                    await tm.AddOrUpdateTagAsync(tag2);

                    var tags = await tm.FindTags(new DefaultAdapterCallContext(), new FindTagsRequest() {
                        Name = TestContext.TestName + "*"
                    }, default).ToEnumerable();

                    Assert.AreEqual(2, tags.Count());

                    var tagActual1 = tags.First();
                    Assert.AreEqual(tag1.Id, tagActual1.Id);
                    Assert.AreEqual(tag1.Name, tagActual1.Name);

                    var tagActual2 = tags.Last();
                    Assert.AreEqual(tag2.Id, tagActual2.Id);
                    Assert.AreEqual(tag2.Name, tagActual2.Name);
                }
            }
            finally {
                tmpPath.Delete();
            }
        }


        [TestMethod]
        public async Task ShouldGetTagByNameAfterRestore() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(SnapshotTagValueManagerTests), Guid.NewGuid().ToString()));
            try {
                var tag1 = new TagDefinitionBuilder().WithId(Guid.NewGuid().ToString()).WithName(TestContext.TestName + "-1").Build();

                using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var tm = ActivatorUtilities.CreateInstance<TagManager>(AssemblyInitializer.ApplicationServices, (IEnumerable<AdapterProperty>) Array.Empty<AdapterProperty>())) {
                    await tm.InitAsync();
                    await tm.AddOrUpdateTagAsync(tag1);
                }

                using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var tm = ActivatorUtilities.CreateInstance<TagManager>(AssemblyInitializer.ApplicationServices, (IEnumerable<AdapterProperty>) Array.Empty<AdapterProperty>())) {
                    await tm.InitAsync();
                    var tags = await tm.GetTags(new DefaultAdapterCallContext(), new GetTagsRequest() {
                        Tags = new[] { tag1.Name }
                    }, default).ToEnumerable();

                    Assert.AreEqual(1, tags.Count());

                    var tagActual = tags.First();
                    Assert.AreEqual(tag1.Id, tagActual.Id);
                    Assert.AreEqual(tag1.Name, tagActual.Name);
                }
            }
            finally {
                tmpPath.Delete();
            }
        }


        [TestMethod]
        public async Task ShouldGetTagByIdAfterRestore() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(SnapshotTagValueManagerTests), Guid.NewGuid().ToString()));
            try {
                var tag1 = new TagDefinitionBuilder().WithId(Guid.NewGuid().ToString()).WithName(TestContext.TestName + "-1").Build();

                using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var tm = ActivatorUtilities.CreateInstance<TagManager>(AssemblyInitializer.ApplicationServices, (IEnumerable<AdapterProperty>) Array.Empty<AdapterProperty>())) {
                    await tm.InitAsync();
                    await tm.AddOrUpdateTagAsync(tag1);
                }

                using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var tm = ActivatorUtilities.CreateInstance<TagManager>(AssemblyInitializer.ApplicationServices, (IEnumerable<AdapterProperty>) Array.Empty<AdapterProperty>())) {
                    await tm.InitAsync();
                    var tags = await tm.GetTags(new DefaultAdapterCallContext(), new GetTagsRequest() {
                        Tags = new[] { tag1.Id }
                    }, default).ToEnumerable();

                    Assert.AreEqual(1, tags.Count());

                    var tagActual = tags.First();
                    Assert.AreEqual(tag1.Id, tagActual.Id);
                    Assert.AreEqual(tag1.Name, tagActual.Name);
                }
            }
            finally {
                tmpPath.Delete();
            }
        }


        [TestMethod]
        public async Task ShouldFindTagsAfterRestore() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(SnapshotTagValueManagerTests), Guid.NewGuid().ToString()));
            try {
                var tag1 = new TagDefinitionBuilder().WithId(Guid.NewGuid().ToString()).WithName(TestContext.TestName + "-1").Build();
                var tag2 = new TagDefinitionBuilder().WithId(Guid.NewGuid().ToString()).WithName(TestContext.TestName + "-2").Build();

                using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var tm = ActivatorUtilities.CreateInstance<TagManager>(AssemblyInitializer.ApplicationServices, (IEnumerable<AdapterProperty>) Array.Empty<AdapterProperty>())) {
                    await tm.InitAsync();
                    await tm.AddOrUpdateTagAsync(tag1);
                    await tm.AddOrUpdateTagAsync(tag2);
                }

                using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var tm = ActivatorUtilities.CreateInstance<TagManager>(AssemblyInitializer.ApplicationServices, (IEnumerable<AdapterProperty>) Array.Empty<AdapterProperty>())) {
                    await tm.InitAsync();
                    var tags = await tm.FindTags(new DefaultAdapterCallContext(), new FindTagsRequest() {
                        Name = TestContext.TestName + "*"
                    }, default).ToEnumerable();

                    Assert.AreEqual(2, tags.Count());

                    var tagActual1 = tags.First();
                    Assert.AreEqual(tag1.Id, tagActual1.Id);
                    Assert.AreEqual(tag1.Name, tagActual1.Name);

                    var tagActual2 = tags.Last();
                    Assert.AreEqual(tag2.Id, tagActual2.Id);
                    Assert.AreEqual(tag2.Name, tagActual2.Name);
                }
            }
            finally {
                tmpPath.Delete();
            }
        }


        [TestMethod]
        public async Task ShouldDeleteTag() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(SnapshotTagValueManagerTests), Guid.NewGuid().ToString()));
            try {
                var tag1 = new TagDefinitionBuilder().WithId(Guid.NewGuid().ToString()).WithName(TestContext.TestName + "-1").Build();

                using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var tm = ActivatorUtilities.CreateInstance<TagManager>(AssemblyInitializer.ApplicationServices, (IEnumerable<AdapterProperty>) Array.Empty<AdapterProperty>())) {
                    await tm.InitAsync();
                    await tm.AddOrUpdateTagAsync(tag1);

                    var tags = await tm.GetTags(new DefaultAdapterCallContext(), new GetTagsRequest() {
                        Tags = new[] { tag1.Id }
                    }, default).ToEnumerable();

                    Assert.AreEqual(1, tags.Count());

                    var tagActual = tags.First();
                    Assert.AreEqual(tag1.Id, tagActual.Id);
                    Assert.AreEqual(tag1.Name, tagActual.Name);

                    await tm.DeleteTagAsync(tag1.Id);

                    tags = await tm.GetTags(new DefaultAdapterCallContext(), new GetTagsRequest() {
                        Tags = new[] { tag1.Id }
                    }, default).ToEnumerable();

                    Assert.AreEqual(0, tags.Count());
                }
            }
            finally {
                tmpPath.Delete();
            }
        }


        [TestMethod]
        public async Task ShouldRenameTag() {
            var tmpPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), nameof(SnapshotTagValueManagerTests), Guid.NewGuid().ToString()));
            try {
                var id = Guid.NewGuid().ToString();
                var tag1 = new TagDefinitionBuilder().WithId(id).WithName(TestContext.TestName + "-1").Build();
                var tag2 = new TagDefinitionBuilder().WithId(id).WithName(TestContext.TestName + "-2").Build();

                using (var store = new FasterKeyValueStore(new FasterKeyValueStoreOptions() {
                    CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(tmpPath.FullName)
                }))
                using (var tm = ActivatorUtilities.CreateInstance<TagManager>(AssemblyInitializer.ApplicationServices, (IEnumerable<AdapterProperty>) Array.Empty<AdapterProperty>())) {
                    await tm.InitAsync();

                    // Register the tag.

                    await tm.AddOrUpdateTagAsync(tag1);

                    // Ensure that we can get the tag using the original name.

                    var tags = await tm.GetTags(new DefaultAdapterCallContext(), new GetTagsRequest() {
                        Tags = new[] { tag1.Name }
                    }, default).ToEnumerable();

                    Assert.AreEqual(1, tags.Count());

                    var tagActual = tags.First();
                    Assert.AreEqual(tag1.Id, tagActual.Id);
                    Assert.AreEqual(tag1.Name, tagActual.Name);

                    // Replace the tag definition.

                    await tm.AddOrUpdateTagAsync(tag2);

                    // Ensure that we can no longer get the tag using the original name.

                    tags = await tm.GetTags(new DefaultAdapterCallContext(), new GetTagsRequest() {
                        Tags = new[] { tag1.Name }
                    }, default).ToEnumerable();

                    Assert.AreEqual(0, tags.Count());

                    // Ensure that we can get the tag using the new name.

                    tags = await tm.GetTags(new DefaultAdapterCallContext(), new GetTagsRequest() {
                        Tags = new[] { tag2.Name }
                    }, default).ToEnumerable();

                    Assert.AreEqual(1, tags.Count());

                    tagActual = tags.First();
                    Assert.AreEqual(tag2.Id, tagActual.Id);
                    Assert.AreEqual(tag2.Name, tagActual.Name);
                }
            }
            finally {
                tmpPath.Delete();
            }
        }

    }

}
