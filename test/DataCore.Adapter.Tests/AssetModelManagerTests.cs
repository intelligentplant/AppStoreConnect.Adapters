using System;
using System.Threading.Tasks;

using DataCore.Adapter.AssetModel;

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

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices)) {
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

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices)) {
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

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices)) {
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

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices)) {
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

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices)) {
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

            using (var manager = ActivatorUtilities.CreateInstance<AssetModelManager>(AssemblyInitializer.ApplicationServices)) {
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

    }

}
