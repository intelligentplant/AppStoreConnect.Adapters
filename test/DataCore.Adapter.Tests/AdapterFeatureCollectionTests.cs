using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Events;
using DataCore.Adapter.RealTimeData;

using IntelligentPlant.BackgroundTasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AdapterFeatureCollectionTests : TestsBase {

        [TestMethod]
        public void AdapterFeatureCollectionShouldBePrePopulated() {
            using (var adapter = new TestAdapter(TestContext.TestName)) {
                adapter.AddFeatures(new FeatureProvider());
                var featureCollection = adapter.Features;
                Assert.IsNotNull(featureCollection.Get<IReadSnapshotTagValues>(), $"{nameof(IReadSnapshotTagValues)} feature should be defined.");
                Assert.IsNotNull(featureCollection.Get<IReadEventMessagesForTimeRange>(), $"{nameof(IReadEventMessagesForTimeRange)} feature should be defined.");
            }

        }

        [TestMethod]
        public void AdapterFeaturesCollectionShouldBeEmptyWhenProviderDoesNotImplementFeatures() {
            using (var adapter = new TestAdapter(TestContext.TestName)) {
                var featureCollection = adapter.Features;
                Assert.IsNull(featureCollection.Get<IReadSnapshotTagValues>(), $"{nameof(IReadSnapshotTagValues)} feature should not be defined.");
                Assert.IsNull(featureCollection.Get<IReadEventMessagesForTimeRange>(), $"{nameof(IReadEventMessagesForTimeRange)} feature should not be defined.");
            }
        }


        [TestMethod]
        public void AdapterFeaturesCollectionShouldResolveExtensionUsingAbsoluteUri() {
            using (var adapter = new TestAdapter(TestContext.TestName)) {
                adapter.AddFeatures(new PingPongExtension(null!, Array.Empty<Common.IObjectEncoder>()));
                var featureCollection = adapter.Features;

                Assert.IsNotNull(featureCollection.GetExtension(new Uri(PingPongExtension.FeatureUri)));
                Assert.IsTrue(featureCollection.TryGetExtension(new Uri(PingPongExtension.FeatureUri), out var f));
                Assert.IsNotNull(f);
            }
        }


        [TestMethod]
        public void AdapterFeaturesCollectionShouldResolveExtensionUsingRelativeUri() {
            using (var adapter = new TestAdapter(TestContext.TestName)) {
                adapter.AddFeatures(new PingPongExtension(null!, Array.Empty<Common.IObjectEncoder>()));
                var featureCollection = adapter.Features;

                Assert.IsNotNull(featureCollection.GetExtension(new Uri(PingPongExtension.RelativeFeatureUri, UriKind.Relative)));
                Assert.IsTrue(featureCollection.TryGetExtension(new Uri(PingPongExtension.RelativeFeatureUri, UriKind.Relative), out var f));
                Assert.IsNotNull(f);
            }
        }


        [TestMethod]
        public void AdapterFeaturesCollectionShouldResolveExtensionUsingAbsoluteUriString() {
            using (var adapter = new TestAdapter(TestContext.TestName)) {
                adapter.AddFeatures(new PingPongExtension(null!, Array.Empty<Common.IObjectEncoder>()));
                var featureCollection = adapter.Features;

                Assert.IsNotNull(featureCollection.GetExtension(PingPongExtension.FeatureUri));
                Assert.IsTrue(featureCollection.TryGetExtension(PingPongExtension.FeatureUri, out var f));
                Assert.IsNotNull(f);
            }
        }


        [TestMethod]
        public void AdapterFeaturesCollectionShouldResolveExtensionUsingRelativeUriString() {
            using (var adapter = new TestAdapter(TestContext.TestName)) {
                adapter.AddFeatures(new PingPongExtension(null!, Array.Empty<Common.IObjectEncoder>()));
                var featureCollection = adapter.Features;

                Assert.IsNotNull(featureCollection.GetExtension(PingPongExtension.RelativeFeatureUri));
                Assert.IsTrue(featureCollection.TryGetExtension(PingPongExtension.RelativeFeatureUri, out var f));
                Assert.IsNotNull(f);
            }
        }


        private class TestAdapter : AdapterBase {

            internal TestAdapter(string id) : base(id, null, null, null, null) { }


            protected override Task StartAsync(CancellationToken cancellationToken) {
                return Task.CompletedTask;
            }

            protected override Task StopAsync(CancellationToken cancellationToken) {
                return Task.CompletedTask;
            }
        }


        private class FeatureProvider : IReadSnapshotTagValues, IReadEventMessagesForTimeRange {

            public IBackgroundTaskService BackgroundTaskService => IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;


            IAsyncEnumerable<TagValueQueryResult> IReadSnapshotTagValues.ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
                throw new NotImplementedException();
            }

            IAsyncEnumerable<EventMessage> IReadEventMessagesForTimeRange.ReadEventMessagesForTimeRange(IAdapterCallContext context, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
                throw new NotImplementedException();
            }
        }

    }
}
