using System;
using System.Threading;
using System.Threading.Channels;
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
            var featureCollection = new AdapterFeaturesCollection(new FeatureProvider());
            Assert.IsNotNull(featureCollection.Get<IReadSnapshotTagValues>(), $"{nameof(IReadSnapshotTagValues)} feature should be defined.");
            Assert.IsNotNull(featureCollection.Get<IReadEventMessagesForTimeRange>(), $"{nameof(IReadEventMessagesForTimeRange)} feature should be defined.");
        }


        [TestMethod]
        public void AdapterFeaturesCollectionShouldBeEmptyWhenProviderDoesNotImplementFeatures() {
            var featureCollection = new AdapterFeaturesCollection(new object());
            Assert.IsNull(featureCollection.Get<IReadSnapshotTagValues>(), $"{nameof(IReadSnapshotTagValues)} feature should not be defined.");
            Assert.IsNull(featureCollection.Get<IReadEventMessagesForTimeRange>(), $"{nameof(IReadEventMessagesForTimeRange)} feature should not be defined.");
        }


        [TestMethod]
        public void AdapterFeaturesCollectionShouldBeEmptyWhenProviderIsNull() {
            var featureCollection = new AdapterFeaturesCollection(null);
            Assert.IsNull(featureCollection.Get<IReadSnapshotTagValues>(), $"{nameof(IReadSnapshotTagValues)} feature should not be defined.");
            Assert.IsNull(featureCollection.Get<IReadEventMessagesForTimeRange>(), $"{nameof(IReadEventMessagesForTimeRange)} feature should not be defined.");
        }


        [TestMethod]
        public void AdapterFeaturesCollectionShouldResolveExtensionUsingAbsoluteUri() {
            var featureCollection = new AdapterFeaturesCollection();
            featureCollection.AddFromProvider(new PingPongExtension((IBackgroundTaskService) null!));

            Assert.IsNotNull(featureCollection.GetExtension(new Uri(PingPongExtension.FeatureUri)));
            Assert.IsTrue(featureCollection.TryGetExtension(new Uri(PingPongExtension.FeatureUri), out var f));
            Assert.IsNotNull(f);
        }


        [TestMethod]
        public void AdapterFeaturesCollectionShouldResolveExtensionUsingRelativeUri() {
            var featureCollection = new AdapterFeaturesCollection();
            featureCollection.AddFromProvider(new PingPongExtension((IBackgroundTaskService) null!));

            Assert.IsNotNull(featureCollection.GetExtension(new Uri(PingPongExtension.RelativeFeatureUri, UriKind.Relative)));
            Assert.IsTrue(featureCollection.TryGetExtension(new Uri(PingPongExtension.RelativeFeatureUri, UriKind.Relative), out var f));
            Assert.IsNotNull(f);
        }


        [TestMethod]
        public void AdapterFeaturesCollectionShouldResolveExtensionUsingAbsoluteUriString() {
            var featureCollection = new AdapterFeaturesCollection();
            featureCollection.AddFromProvider(new PingPongExtension((IBackgroundTaskService) null!));

            Assert.IsNotNull(featureCollection.GetExtension(PingPongExtension.FeatureUri));
            Assert.IsTrue(featureCollection.TryGetExtension(PingPongExtension.FeatureUri, out var f));
            Assert.IsNotNull(f);
        }


        [TestMethod]
        public void AdapterFeaturesCollectionShouldResolveExtensionUsingRelativeUriString() {
            var featureCollection = new AdapterFeaturesCollection();
            featureCollection.AddFromProvider(new PingPongExtension((IBackgroundTaskService) null!));

            Assert.IsNotNull(featureCollection.GetExtension(PingPongExtension.RelativeFeatureUri));
            Assert.IsTrue(featureCollection.TryGetExtension(PingPongExtension.RelativeFeatureUri, out var f));
            Assert.IsNotNull(f);
        }


        private class FeatureProvider : IReadSnapshotTagValues, IReadEventMessagesForTimeRange {

            Task<ChannelReader<TagValueQueryResult>> IReadSnapshotTagValues.ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
                throw new NotImplementedException();
            }

            Task<ChannelReader<EventMessage>> IReadEventMessagesForTimeRange.ReadEventMessagesForTimeRange(IAdapterCallContext context, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
                throw new NotImplementedException();
            }
        }

    }
}
