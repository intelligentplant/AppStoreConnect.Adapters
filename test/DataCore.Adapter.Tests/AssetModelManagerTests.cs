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

    }

}
