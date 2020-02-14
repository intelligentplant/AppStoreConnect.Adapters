using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {
    public abstract class AdapterTests<TAdapter> : TestsBase where TAdapter : class, IAdapter {

        private IServiceScope _scope;

        protected IServiceProvider ServiceProvider { get { return _scope?.ServiceProvider; } }

        protected abstract TAdapter CreateAdapter();

        protected abstract string GetTestTagId();


        [TestInitialize]
        public void TestInitialize() {
            _scope = WebHostInitializer.ApplicationServices.CreateScope();
        }


        [TestCleanup]
        public void TestCleanup() {
            _scope.Dispose();
        }


        protected async Task RunAdapterTest(Func<TAdapter, IAdapterCallContext, Task> callback) {
            var adapter = CreateAdapter();
            try {
                await adapter.StartAsync(default);
                var context = ExampleCallContext.ForPrincipal(null);
                await callback(adapter, context);
            }
            finally {
                if (adapter is IAsyncDisposable iad) {
                    await iad.DisposeAsync();
                }
                else if (adapter is IDisposable id) {
                    id.Dispose();
                }
            }
        }


        protected void AssertFeatureNotImplemented<TFeature>() {
            Assert.Inconclusive($"Feature not implemented: {typeof(TFeature).Name}");
        }


        protected void AssertFeatureNotImplemented(string feature) {
            Assert.Inconclusive($"Feature not implemented: {feature}");
        }


        [TestMethod]
        public Task FindTagsShouldReturnResults() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<ITagSearch>();
                if (feature == null) {
                    AssertFeatureNotImplemented<ITagSearch>();
                    return;
                }

                var tags = await feature.FindTags(context, new FindTagsRequest(), default).ToEnumerable();
                Assert.IsTrue(tags.Any());
            });
        }


        [TestMethod]
        public Task GetTagsShouldReturnResults() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<ITagSearch>();
                if (feature == null) {
                    AssertFeatureNotImplemented<ITagSearch>();
                    return;
                }

                var tagId = GetTestTagId();
                var tags = await feature.GetTags(context, new GetTagsRequest() { 
                    Tags = new [] { tagId }
                }, default).ToEnumerable();

                Assert.AreEqual(1, tags.Count());
                Assert.AreEqual(tagId, tags.First().Id);
            });
        }


        [TestMethod]
        public Task ReadSnapshotValuesShouldReturnResults() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IReadSnapshotTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadSnapshotTagValues>();
                    return;
                }

                var tagId = GetTestTagId();
                var values = await feature.ReadSnapshotTagValues(context, new ReadSnapshotTagValuesRequest() { 
                    Tags = new[] { tagId }
                }, default).ToEnumerable();

                Assert.AreEqual(1, values.Count());

                var val = values.First();
                Assert.IsNotNull(val);
                Assert.AreEqual(tagId, val.TagId);
            });
        }


        [TestMethod]
        public Task SnapshotSubscriptionShouldReceiveInitialValues() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<ISnapshotTagValuePush>();
                if (feature == null) {
                    AssertFeatureNotImplemented<ISnapshotTagValuePush>();
                    return;
                }

                using (var subscription = await feature.Subscribe(context, default)) {
                    Assert.IsNotNull(subscription);
                    Assert.AreEqual(0, subscription.Count);

                    // TODO: 
                    // Remove this delay; it is required for the gRPC proxy, because the invocation 
                    // of the add tags to subscription seems to start before the actual call to 
                    // establish the subscription has finished.
                    await Task.Delay(100);

                    var tagId = GetTestTagId();

                    var subscribedTagCount = await subscription.AddTagsToSubscription(context, new[] { tagId }, default);
                    Assert.AreEqual(1, subscribedTagCount);
                    Assert.AreEqual(subscribedTagCount, subscription.Count);

                    using (var ctSource = new CancellationTokenSource(1000)) {
                        var val = await subscription.Reader.ReadAsync(ctSource.Token);
                        Assert.IsNotNull(val);
                        Assert.AreEqual(tagId, val.TagId);
                    }
                }
            });
        }

    }
}
