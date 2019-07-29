using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Features;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {
    [TestClass]
    public class AdapterTests {

        [TestMethod]
        public void UnsupportedFeatureShouldNotBeFound() {
            using (var adapter = new ExampleAdapter()) {
                var feature = adapter.Features.Get<IFakeAdapterFeature>();
                Assert.IsNull(feature);
            }
        }


        [TestMethod]
        public void SupportedFeatureShouldBeFound() {
            using (var adapter = new ExampleAdapter()) {
                var feature = adapter.Features.Get<IReadSnapshotTagValues>();
                Assert.IsNotNull(feature);
            }
        }


        [TestMethod]
        public async Task SnapshotSubscriptionShouldReceiveNewValues() {
            using (var adapter = new ExampleAdapter()) {
                var feature = adapter.Features.Get<ISnapshotTagValuePush>();

                using (var subscription = await feature.Subscribe(ExampleCallContext.ForPrincipal(null), CancellationToken.None).ConfigureAwait(false)) {
                    var subscribedTagCount = await subscription.AddTagsToSubscription(ExampleCallContext.ForPrincipal(null), new[] { "Test Tag 1", "Test Tag 2" }, CancellationToken.None).ConfigureAwait(false);
                    Assert.AreEqual(2, subscribedTagCount, "Incorrect subscribed tag count");

                    // Write a couple of values that we should then be able to read out again via 
                    // the subscription's channel.
                    var now = System.DateTime.UtcNow;
                    adapter.WriteSnapshotValue(
                        new RealTimeData.Models.TagValueQueryResult(
                            "Test Tag 1", 
                            "Test Tag 1", 
                            RealTimeData.Models.TagValueBuilder
                                .Create()
                                .WithUtcSampleTime(now.AddSeconds(-5))
                                .WithNumericValue(100)
                                .Build()
                        )
                    );
                    adapter.WriteSnapshotValue(
                        new RealTimeData.Models.TagValueQueryResult(
                            "Test Tag 1",
                            "Test Tag 1",
                            RealTimeData.Models.TagValueBuilder
                                .Create()
                                .WithUtcSampleTime(now.AddSeconds(-1))
                                .WithNumericValue(99)
                                .Build()
                        )
                    );

                    using (var ctSource = new CancellationTokenSource(1000)) {
                        var value = await subscription.Reader.ReadAsync(ctSource.Token).ConfigureAwait(false);
                        ctSource.Token.ThrowIfCancellationRequested();
                        Assert.AreEqual(now.AddSeconds(-5), value.Value.UtcSampleTime);
                        Assert.AreEqual(100, value.Value.NumericValue);
                    }

                    using (var ctSource = new CancellationTokenSource(1000)) {
                        var value = await subscription.Reader.ReadAsync(ctSource.Token).ConfigureAwait(false);
                        ctSource.Token.ThrowIfCancellationRequested();
                        Assert.AreEqual(now.AddSeconds(-1), value.Value.UtcSampleTime);
                        Assert.AreEqual(99, value.Value.NumericValue);
                    }
                }
            }
        }

    }
}
