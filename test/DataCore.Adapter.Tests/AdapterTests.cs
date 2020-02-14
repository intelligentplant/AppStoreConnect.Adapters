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

        protected abstract TestTagDetails GetTestTagDetails();


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

        #region [ ITagSearch Tests ]

        [TestMethod]
        public Task FindTagsRequestShouldReturnResults() {
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
        public Task GetTagsRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<ITagSearch>();
                if (feature == null) {
                    AssertFeatureNotImplemented<ITagSearch>();
                    return;
                }

                var tagDetails = GetTestTagDetails();
                var tags = await feature.GetTags(context, new GetTagsRequest() {
                    Tags = new[] { tagDetails.Id }
                }, default).ToEnumerable();

                Assert.AreEqual(1, tags.Count());
                Assert.AreEqual(tagDetails.Id, tags.First().Id);
            });
        }

        #endregion

        #region [ IReadSnapshotTagValues ]

        [TestMethod]
        public Task ReadSnapshotTagValuesRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IReadSnapshotTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadSnapshotTagValues>();
                    return;
                }

                var tagDetails = GetTestTagDetails();
                var values = await feature.ReadSnapshotTagValues(context, new ReadSnapshotTagValuesRequest() {
                    Tags = new[] { tagDetails.Id }
                }, default).ToEnumerable();

                Assert.AreEqual(1, values.Count());

                var val = values.First();
                Assert.IsNotNull(val);
                Assert.IsTrue(tagDetails.Id.Equals(val.TagId) || tagDetails.Id.Equals(val.TagName, StringComparison.OrdinalIgnoreCase));
            });
        }

        #endregion

        #region [ ISnapshotTagValuePush ]

        [TestMethod]
        public Task SnapshotTagValueSubscriptionShouldReceiveInitialValues() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<ISnapshotTagValuePush>();
                if (feature == null) {
                    AssertFeatureNotImplemented<ISnapshotTagValuePush>();
                    return;
                }

                using (var subscription = await feature.Subscribe(context, default)) {
                    Assert.IsNotNull(subscription);
                    Assert.AreEqual(0, subscription.Count);

                    var tagDetails = GetTestTagDetails();

                    var subscribedTagCount = await subscription.AddTagsToSubscription(context, new[] { tagDetails.Id }, default);
                    Assert.AreEqual(1, subscribedTagCount);
                    Assert.AreEqual(subscribedTagCount, subscription.Count);

                    using (var ctSource = new CancellationTokenSource(1000)) {
                        var val = await subscription.Reader.ReadAsync(ctSource.Token);
                        Assert.IsNotNull(val);
                        Assert.IsTrue(tagDetails.Id.Equals(val.TagId) || tagDetails.Id.Equals(val.TagName, StringComparison.OrdinalIgnoreCase));
                    }
                }
            });
        }

        #endregion

        #region [ IReadRawTagValues ]

        [TestMethod]
        public Task ReadRawTagValuesRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IReadRawTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadRawTagValues>();
                    return;
                }

                var tagDetails = GetTestTagDetails();

                var values = await feature.ReadRawTagValues(
                    context,
                    new ReadRawTagValuesRequest() { 
                        Tags = new[] { tagDetails.Id },
                        UtcStartTime = tagDetails.HistoryStartTime,
                        UtcEndTime = tagDetails.HistoryEndTime,
                        BoundaryType = RawDataBoundaryType.Inside,
                        SampleCount = 0
                    },
                    default
                ).ToEnumerable();

                Assert.IsTrue(values.Any());
                Assert.IsTrue(values.First().Value.UtcSampleTime >= tagDetails.HistoryStartTime);
                Assert.IsTrue(values.Last().Value.UtcSampleTime <= tagDetails.HistoryEndTime);
                Assert.IsTrue(values.All(v => tagDetails.Id.Equals(v.TagId) || tagDetails.Id.Equals(v.TagName, StringComparison.OrdinalIgnoreCase)));
            });
        }

        #endregion

        #region [ IReadRawTagValues ]

        [TestMethod]
        public Task ReadPlotTagValuesRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IReadPlotTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadPlotTagValues>();
                    return;
                }

                var tagDetails = GetTestTagDetails();

                var values = await feature.ReadPlotTagValues(
                    context,
                    new ReadPlotTagValuesRequest() {
                        Tags = new[] { tagDetails.Id },
                        UtcStartTime = tagDetails.HistoryStartTime,
                        UtcEndTime = tagDetails.HistoryEndTime,
                        Intervals = 10
                    },
                    default
                ).ToEnumerable();

                Assert.IsTrue(values.Any());
                Assert.IsTrue(values.First().Value.UtcSampleTime >= tagDetails.HistoryStartTime);
                Assert.IsTrue(values.Last().Value.UtcSampleTime <= tagDetails.HistoryEndTime);
                Assert.IsTrue(values.All(v => tagDetails.Id.Equals(v.TagId) || tagDetails.Id.Equals(v.TagName, StringComparison.OrdinalIgnoreCase)));
            });
        }

        #endregion

        #region [ IReadProcessedTagValues ]

        [DataTestMethod]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdAverage)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdCount)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdInterpolate)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMaximum)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMinimum)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdPercentBad)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdPercentGood)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdRange)]
        public Task ReadProcessedTagValuesRequestShouldReturnResults(string dataFunction) {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IReadProcessedTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadProcessedTagValues>();
                    return;
                }

                var supportedDataFunctions = await feature.GetSupportedDataFunctions(context, default);
                if (!supportedDataFunctions.Any(f => f.Id.Equals(dataFunction))) {
                    Assert.Inconclusive($"Data function {dataFunction} is not supported.");
                    return;
                }

                var tagDetails = GetTestTagDetails();
                // Calculate sample interval for 10 buckets.
                var sampleInterval = TimeSpan.FromSeconds((tagDetails.HistoryEndTime - tagDetails.HistoryStartTime).TotalSeconds / 10);

                var values = await feature.ReadProcessedTagValues(
                    context,
                    new ReadProcessedTagValuesRequest() {
                        Tags = new[] { tagDetails.Id },
                        UtcStartTime = tagDetails.HistoryStartTime,
                        UtcEndTime = tagDetails.HistoryEndTime,
                        DataFunctions = new [] { dataFunction },
                        SampleInterval = sampleInterval
                    },
                    default
                ).ToEnumerable();

                Assert.IsTrue(values.Any());
                Assert.IsTrue(values.First().Value.UtcSampleTime >= tagDetails.HistoryStartTime);
                Assert.IsTrue(values.Last().Value.UtcSampleTime <= tagDetails.HistoryEndTime);
                Assert.IsTrue(values.All(v => dataFunction.Equals(v.DataFunction)));
                Assert.IsTrue(values.All(v => tagDetails.Id.Equals(v.TagId) || tagDetails.Id.Equals(v.TagName, StringComparison.OrdinalIgnoreCase)));
            });
        }

        #endregion

    }


    public class TestTagDetails {

        public string Id { get; }

        public DateTime HistoryStartTime { get; set; }

        public DateTime HistoryEndTime { get; set; }


        public TestTagDetails(string id) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

    }

}
