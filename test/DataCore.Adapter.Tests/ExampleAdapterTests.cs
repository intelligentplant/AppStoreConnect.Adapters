using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {
    [TestClass]
    public class ExampleAdapterTests : AdapterTests<ExampleAdapter> {

        private static readonly TestTagDetails TestTag1 = new TestTagDetails("Test Tag 1");


        protected override ExampleAdapter CreateAdapter() {
            return new ExampleAdapter();
        }

        protected override TestTagDetails GetTestTagDetails() {
            return TestTag1;
        }


        [TestMethod]
        public Task UnsupportedFeatureShouldNotBeFound() {
            return RunAdapterTest((adapter, context) => {
                var feature = adapter.Features.Get<IFakeAdapterFeature>();
                Assert.IsNull(feature);
                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task SupportedFeatureShouldBeFound() {
            return RunAdapterTest((adapter, context) => { 
                var feature = adapter.Features.Get<IReadSnapshotTagValues>();
                Assert.IsNotNull(feature);
                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task SnapshotSubscriptionShouldReceiveAdditionalValues() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<ISnapshotTagValuePush>();

                using (var subscription = await feature.Subscribe(context, default).ConfigureAwait(false)) {
                    var subscribedTagCount = await subscription.AddTagsToSubscription(
                        context,
                        new[] {
                            TestTag1.Id
                        },
                        CancellationToken.None
                    ).ConfigureAwait(false);
                    Assert.AreEqual(1, subscribedTagCount, "Incorrect subscribed tag count");

                    // Write a couple of values that we should then be able to read out again via 
                    // the subscription's channel.
                    var now = System.DateTime.UtcNow;
                    adapter.WriteSnapshotValue(
                        TagValueQueryResult.Create(
                            TestTag1.Id,
                            TestTag1.Id,
                            TagValueBuilder
                                .Create()
                                .WithUtcSampleTime(now.AddSeconds(-5))
                                .WithValue(100)
                                .Build()
                        )
                    );
                    adapter.WriteSnapshotValue(
                        TagValueQueryResult.Create(
                            TestTag1.Id,
                            TestTag1.Id,
                            TagValueBuilder
                                .Create()
                                .WithUtcSampleTime(now.AddSeconds(-1))
                                .WithValue(99)
                                .Build()
                        )
                    );

                    // Read initial value.
                    using (var ctSource = new CancellationTokenSource(1000)) {
                        var value = await subscription.Reader.ReadAsync(ctSource.Token).ConfigureAwait(false);
                        ctSource.Token.ThrowIfCancellationRequested();
                        Assert.IsNotNull(value);
                    }

                    // Read first value written above.
                    using (var ctSource = new CancellationTokenSource(1000)) {
                        var value = await subscription.Reader.ReadAsync(ctSource.Token).ConfigureAwait(false);
                        ctSource.Token.ThrowIfCancellationRequested();
                        Assert.AreEqual(now.AddSeconds(-5), value.Value.UtcSampleTime);
                        Assert.AreEqual(100, value.Value.Value.GetValueOrDefault<int>());
                    }

                    // Read second value written above.
                    using (var ctSource = new CancellationTokenSource(1000)) {
                        var value = await subscription.Reader.ReadAsync(ctSource.Token).ConfigureAwait(false);
                        ctSource.Token.ThrowIfCancellationRequested();
                        Assert.AreEqual(now.AddSeconds(-1), value.Value.UtcSampleTime);
                        Assert.AreEqual(99, value.Value.Value.GetValueOrDefault<int>());
                    }
                }
            });
        }

    }
}
