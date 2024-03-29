﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AssetModelManagerTests : TestsBase {

        [TestMethod]
        public async Task ShouldGetNodeById() {
            var node = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName)
                .Build();

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices, new InMemoryKeyValueStore())) {
                await manager.InitAsync();
                await manager.AddOrUpdateNodeAsync(node);

                var nodeActual = await manager.GetNodeAsync(node.Id);
                Assert.IsNotNull(nodeActual);
                Assert.AreEqual(node.Id, nodeActual.Id);
                Assert.AreEqual(node.Name, nodeActual.Name);
            }
        }


        [TestMethod]
        public async Task ParentNodeShouldHaveChildren() {
            var node1 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName + "_Parent")
                .Build();

            var node2 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName + "_Child")
                .WithParent(node1.Id)
                .Build();

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices, new InMemoryKeyValueStore())) {
                await manager.InitAsync();

                await manager.AddOrUpdateNodeAsync(node1);
                await manager.AddOrUpdateNodeAsync(node2);

                var nodeActual = await manager.GetNodeAsync(node1.Id);
                Assert.IsNotNull(nodeActual);
                Assert.AreEqual(node1.Id, nodeActual.Id);
                Assert.AreEqual(node1.Name, nodeActual.Name);
                Assert.IsTrue(nodeActual.HasChildren);
            }
        }


        [TestMethod]
        public async Task ParentNodeShouldNotHaveChildren() {
            var node1 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName + "_Parent")
                .WithChildren(true)
                .Build();

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices, new InMemoryKeyValueStore())) {
                await manager.InitAsync();

                await manager.AddOrUpdateNodeAsync(node1);

                var nodeActual = await manager.GetNodeAsync(node1.Id);
                Assert.IsNotNull(nodeActual);
                Assert.AreEqual(node1.Id, nodeActual.Id);
                Assert.AreEqual(node1.Name, nodeActual.Name);
                Assert.IsFalse(nodeActual.HasChildren);
            }
        }


        [TestMethod]
        public async Task ParentNodeShouldNotHaveChildrenAfterChildDeleted() {
            var node1 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName + "_Parent")
                .Build();

            var node2 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName + "_Child")
                .WithParent(node1.Id)
                .Build();

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices, new InMemoryKeyValueStore())) {
                await manager.InitAsync();

                await manager.AddOrUpdateNodeAsync(node1);
                await manager.AddOrUpdateNodeAsync(node2);

                var nodeActual = await manager.GetNodeAsync(node1.Id);
                Assert.IsNotNull(nodeActual);
                Assert.AreEqual(node1.Id, nodeActual.Id);
                Assert.AreEqual(node1.Name, nodeActual.Name);
                Assert.IsTrue(nodeActual.HasChildren);

                var deleted = await manager.DeleteNodeAsync(node2.Id);
                Assert.IsTrue(deleted);

                nodeActual = await manager.GetNodeAsync(node1.Id);
                Assert.IsNotNull(nodeActual);
                Assert.AreEqual(node1.Id, nodeActual.Id);
                Assert.AreEqual(node1.Name, nodeActual.Name);
                Assert.IsFalse(nodeActual.HasChildren);
            }
        }


        [TestMethod]
        public async Task ChildNodeShouldBeDeletedWhenParentIsDeleted() {
            var node1 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName + "_Parent")
                .Build();

            var node2 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName + "_Child")
                .WithParent(node1.Id)
                .Build();

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices, new InMemoryKeyValueStore())) {
                await manager.InitAsync();

                await manager.AddOrUpdateNodeAsync(node1);
                await manager.AddOrUpdateNodeAsync(node2);

                // Confirm parent is created.
                var nodeActual = await manager.GetNodeAsync(node1.Id);
                Assert.IsNotNull(nodeActual);
                Assert.AreEqual(node1.Id, nodeActual.Id);
                Assert.AreEqual(node1.Name, nodeActual.Name);
                Assert.IsTrue(nodeActual.HasChildren);

                // Confirm child is created.
                nodeActual = await manager.GetNodeAsync(node2.Id);
                Assert.IsNotNull(nodeActual);
                Assert.AreEqual(node2.Id, nodeActual.Id);
                Assert.AreEqual(node2.Name, nodeActual.Name);
                Assert.IsFalse(nodeActual.HasChildren);

                var deleted = await manager.DeleteNodeAsync(node1.Id);
                Assert.IsTrue(deleted);

                // Confirm parent is deleted.
                nodeActual = await manager.GetNodeAsync(node1.Id);
                Assert.IsNull(nodeActual);

                // Confirm child is deleted.
                nodeActual = await manager.GetNodeAsync(node2.Id);
                Assert.IsNull(nodeActual);
            }
        }


        [TestMethod]
        public async Task NewParentShouldHaveChildrenAfterMove() {
            var node1 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName + "_Parent1")
                .Build();

            var node2 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName + "_Parent2")
                .Build();

            var node3 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName + "_Child")
                .WithParent(node1.Id)
                .Build();

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices, new InMemoryKeyValueStore())) {
                await manager.InitAsync();

                await manager.AddOrUpdateNodeAsync(node1);
                await manager.AddOrUpdateNodeAsync(node2);
                await manager.AddOrUpdateNodeAsync(node3);

                // Confirm parent1 is created.
                var nodeActual = await manager.GetNodeAsync(node1.Id);
                Assert.IsNotNull(nodeActual);
                Assert.AreEqual(node1.Id, nodeActual.Id);
                Assert.AreEqual(node1.Name, nodeActual.Name);
                Assert.IsTrue(nodeActual.HasChildren);

                // Confirm parent2 is created.
                nodeActual = await manager.GetNodeAsync(node2.Id);
                Assert.IsNotNull(nodeActual);
                Assert.AreEqual(node2.Id, nodeActual.Id);
                Assert.AreEqual(node2.Name, nodeActual.Name);
                Assert.IsFalse(nodeActual.HasChildren);

                // Confirm child is created.
                nodeActual = await manager.GetNodeAsync(node3.Id);
                Assert.IsNotNull(nodeActual);
                Assert.AreEqual(node3.Id, nodeActual.Id);
                Assert.AreEqual(node3.Name, nodeActual.Name);
                Assert.IsFalse(nodeActual.HasChildren);

                // Move child
                await manager.MoveNodeAsync(node3.Id, node2.Id);

                // Confirm that child's parent ID has been updated.
                nodeActual = await manager.GetNodeAsync(node3.Id);
                Assert.AreEqual(node2.Id, nodeActual.Parent);

                // Confirm that parent1 no longer has children.
                nodeActual = await manager.GetNodeAsync(node1.Id);
                Assert.IsFalse(nodeActual.HasChildren);

                // Confirm that parent2 now has children.
                nodeActual = await manager.GetNodeAsync(node2.Id);
                Assert.IsTrue(nodeActual.HasChildren);
            }
        }


        [TestMethod]
        public async Task ShouldEmitCreatedConfigurationChange() {
            var node = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName)
                .Build();

            var notificationReceived = false;

            Func<ConfigurationChange, CancellationToken, ValueTask> onNotificationReceived = (change, ct) => {
                if (change.ItemType.Equals(ConfigurationChangeItemTypes.AssetModelNode, StringComparison.Ordinal) && change.ChangeType == ConfigurationChangeType.Created && change.ItemId.Equals(node.Id, StringComparison.Ordinal) && change.ItemName.Equals(node.Name, StringComparison.Ordinal)) {
                    notificationReceived = true;
                }
                return default;
            };

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices, new InMemoryKeyValueStore(), onNotificationReceived)) {
                await manager.InitAsync();
                await manager.AddOrUpdateNodeAsync(node);

                Assert.IsTrue(notificationReceived);
            }
        }


        [TestMethod]
        public async Task ShouldEmitDeletedConfigurationChange() {
            var node = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName)
                .Build();

            var notificationReceived = false;

            Func<ConfigurationChange, CancellationToken, ValueTask> onNotificationReceived = (change, ct) => {
                if (change.ItemType.Equals(ConfigurationChangeItemTypes.AssetModelNode, StringComparison.Ordinal) && change.ChangeType == ConfigurationChangeType.Deleted && change.ItemId.Equals(node.Id, StringComparison.Ordinal) && change.ItemName.Equals(node.Name, StringComparison.Ordinal)) {
                    notificationReceived = true;
                }
                return default;
            };

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices, new InMemoryKeyValueStore(), onNotificationReceived)) {
                await manager.InitAsync();
                await manager.AddOrUpdateNodeAsync(node);
                await manager.DeleteNodeAsync(node.Id);

                Assert.IsTrue(notificationReceived);
            }
        }


        [TestMethod]
        public async Task ShouldEmitUpdatedConfigurationChange() {
            var node1 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName)
                .Build();

            var node2 = new AssetModelNodeBuilder(node1)
                .WithName(TestContext.TestName + "_UPDATED")
                .Build();

            var notificationReceived = false;

            Func<ConfigurationChange, CancellationToken, ValueTask> onNotificationReceived = (change, ct) => {
                if (change.ItemType.Equals(ConfigurationChangeItemTypes.AssetModelNode, StringComparison.Ordinal) && change.ChangeType == ConfigurationChangeType.Updated && change.ItemId.Equals(node2.Id, StringComparison.Ordinal) && change.ItemName.Equals(node2.Name, StringComparison.Ordinal)) {
                    notificationReceived = true;
                }
                return default;
            };

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices, new InMemoryKeyValueStore(), onNotificationReceived)) {
                await manager.InitAsync();
                // Create
                await manager.AddOrUpdateNodeAsync(node1);
                // Update
                await manager.AddOrUpdateNodeAsync(node2);

                Assert.IsTrue(notificationReceived);
            }
        }


        [TestMethod]
        public async Task ShouldUseCustomSorting() {
            var node1 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName + "_1")
                .Build();

            var node2 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName(TestContext.TestName + "_2")
                .Build();

            var node3 = new AssetModelNodeBuilder()
                .WithId(Guid.NewGuid().ToString())
                .WithName("$" + TestContext.TestName + "_3")
                .Build();

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices, new InMemoryKeyValueStore(), CustomNodeNameComparer.Instance)) {
                await manager.InitAsync();

                await manager.AddOrUpdateNodeAsync(node1);
                await manager.AddOrUpdateNodeAsync(node2);
                await manager.AddOrUpdateNodeAsync(node3);

                var nodes = await manager.BrowseAssetModelNodes(new DefaultAdapterCallContext(), new BrowseAssetModelNodesRequest() {
                    PageSize = 10
                }, default).ToEnumerable();

                Assert.AreEqual(3, nodes.Count());

                // node 3 should be first, because its name starts with '$'. The other two nodes
                // should be sorted alphabetically.
                Assert.AreEqual(node3.Id, nodes.ElementAt(0).Id);
                Assert.AreEqual(node1.Id, nodes.ElementAt(1).Id);
                Assert.AreEqual(node2.Id, nodes.ElementAt(2).Id);
            }
        }


        private class CustomNodeNameComparer : IComparer<string> {

            public static IComparer<string> Instance { get; } = new CustomNodeNameComparer();


            public int Compare(string x, string y) {
                if (x.StartsWith("$") && y.StartsWith("$")) {
                    return StringComparer.OrdinalIgnoreCase.Compare(x, y);
                }
                if (x.StartsWith("$")) {
                    return -1;
                }
                if (y.StartsWith("$")) {
                    return 1;
                }
                return StringComparer.OrdinalIgnoreCase.Compare(x, y);
            }
        }

    }

}
