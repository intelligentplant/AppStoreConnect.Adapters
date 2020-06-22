using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Events;
using DataCore.Adapter.RealTimeData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {
    public abstract class AdapterTests<TAdapter> : TestsBase where TAdapter : class, IAdapter {

        private IServiceScope _scope;

        protected IServiceProvider ServiceProvider { get { return _scope?.ServiceProvider; } }

        protected abstract TAdapter CreateAdapter();

        protected abstract ReadTagValuesQueryDetails GetReadTagValuesQueryDetails();

        protected abstract ReadEventMessagesQueryDetails GetReadEventMessagesQueryDetails();

        protected abstract Task EmitTestEvent(TAdapter adapter, EventMessageSubscriptionType subscriptionType);

        protected abstract Task<bool> EmitHealthStatus(TAdapter adapter);


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

        #region [ IHealthCheck ]

        [TestMethod]
        public Task CheckHealthRequestShouldSucceed() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IHealthCheck>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IHealthCheck>();
                    return;
                }

                var health = await feature.CheckHealthAsync(context, default);
                VerifyHealthCheckResult(health);
            });
        }


        private void VerifyHealthCheckResult(HealthCheckResult health) {
            if (health.InnerResults != null && health.InnerResults.Any()) {
                foreach (var item in health.InnerResults) {
                    VerifyHealthCheckResult(item);
                }

                // If there are any inner results, ensure that the overall status matches the 
                // aggregate status of the inner results.
                Assert.AreEqual(health.Status, HealthCheckResult.GetAggregateHealthStatus(health.InnerResults.Select(x => x.Status)));
            }
        }


        [TestMethod]
        public Task HealthCheckSubscriptionShouldReceiveUpdates() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IHealthCheckPush>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IHealthCheckPush>();
                    return;
                }

                using (var subscription = await feature.Subscribe(context)) {
                    Assert.IsNotNull(subscription);
                    Assert.IsTrue(subscription.IsStarted);

                    await Task.Delay(1000, default);
                    var success = await EmitHealthStatus(adapter);
                    if (!success) {
                        Assert.Inconclusive("Test harness has indicated that a health status update will not be emitted.");
                    }

                    using (var ctSource = new CancellationTokenSource(1000)) {
                        var health = await subscription.Reader.ReadAsync(ctSource.Token);
                        VerifyHealthCheckResult(health);
                    }
                }
            });
        }

        #endregion

        #region [ ITagSearch ]

        [TestMethod]
        public Task FindTagsRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<ITagSearch>();
                if (feature == null) {
                    AssertFeatureNotImplemented<ITagSearch>();
                    return;
                }

                var channel = await feature.FindTags(context, new FindTagsRequest(), default);
                var tags = await channel.ToEnumerable();
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

                var tagDetails = GetReadTagValuesQueryDetails();
                var channel = await feature.GetTags(context, new GetTagsRequest() {
                    Tags = new[] { tagDetails.Id }
                }, default);
                var tags = await channel.ToEnumerable();

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

                var tagDetails = GetReadTagValuesQueryDetails();
                var channel = await feature.ReadSnapshotTagValues(context, new ReadSnapshotTagValuesRequest() {
                    Tags = new[] { tagDetails.Id }
                }, default);
                var values = await channel.ToEnumerable();

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

                using (var subscription = await feature.Subscribe(context)) {
                    Assert.IsNotNull(subscription);
                    Assert.IsTrue(subscription.IsStarted);

                    var tagDetails = GetReadTagValuesQueryDetails();

                    var subscribeSucceeded = await subscription.AddTagToSubscription(tagDetails.Id);
                    Assert.IsTrue(subscribeSucceeded);

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

                var tagDetails = GetReadTagValuesQueryDetails();

                var channel = await feature.ReadRawTagValues(
                    context,
                    new ReadRawTagValuesRequest() {
                        Tags = new[] { tagDetails.Id },
                        UtcStartTime = tagDetails.HistoryStartTime,
                        UtcEndTime = tagDetails.HistoryEndTime,
                        BoundaryType = RawDataBoundaryType.Inside,
                        SampleCount = 0
                    },
                    default
                );
                var values = await channel.ToEnumerable();

                Assert.IsTrue(values.Any());
                Assert.IsTrue(values.First().Value.UtcSampleTime >= tagDetails.HistoryStartTime);
                Assert.IsTrue(values.Last().Value.UtcSampleTime <= tagDetails.HistoryEndTime);
                Assert.IsTrue(values.All(v => tagDetails.Id.Equals(v.TagId) || tagDetails.Id.Equals(v.TagName, StringComparison.OrdinalIgnoreCase)));
            });
        }

        #endregion

        #region [ IReadPlotTagValues ]

        [TestMethod]
        public Task ReadPlotTagValuesRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IReadPlotTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadPlotTagValues>();
                    return;
                }

                var tagDetails = GetReadTagValuesQueryDetails();

                var channel = await feature.ReadPlotTagValues(
                    context,
                    new ReadPlotTagValuesRequest() {
                        Tags = new[] { tagDetails.Id },
                        UtcStartTime = tagDetails.HistoryStartTime,
                        UtcEndTime = tagDetails.HistoryEndTime,
                        Intervals = 10
                    },
                    default
                );
                var values = await channel.ToEnumerable();

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
        [DataRow(DefaultDataFunctions.Constants.FunctionIdDelta)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdVariance)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdStandardDeviation)]
        public Task ReadProcessedTagValuesRequestShouldReturnResults(string dataFunction) {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IReadProcessedTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadProcessedTagValues>();
                    return;
                }

                var channel = await feature.GetSupportedDataFunctions(context, default);
                var supportedDataFunctions = await channel.ToEnumerable();
                if (!supportedDataFunctions.Any(f => f.Id.Equals(dataFunction))) {
                    Assert.Inconclusive($"Data function {dataFunction} is not supported.");
                    return;
                }

                var tagDetails = GetReadTagValuesQueryDetails();
                // Calculate sample interval for 10 buckets.
                var sampleInterval = TimeSpan.FromSeconds((tagDetails.HistoryEndTime - tagDetails.HistoryStartTime).TotalSeconds / 10);

                var channel2 = await feature.ReadProcessedTagValues(
                    context,
                    new ReadProcessedTagValuesRequest() {
                        Tags = new[] { tagDetails.Id },
                        UtcStartTime = tagDetails.HistoryStartTime,
                        UtcEndTime = tagDetails.HistoryEndTime,
                        DataFunctions = new[] { dataFunction },
                        SampleInterval = sampleInterval
                    },
                    default
                );
                var values = await channel2.ToEnumerable();

                Assert.IsTrue(values.Any());
                Assert.IsTrue(values.First().Value.UtcSampleTime >= tagDetails.HistoryStartTime);
                Assert.IsTrue(values.Last().Value.UtcSampleTime <= tagDetails.HistoryEndTime);
                Assert.IsTrue(values.All(v => dataFunction.Equals(v.DataFunction)));
                Assert.IsTrue(values.All(v => tagDetails.Id.Equals(v.TagId) || tagDetails.Id.Equals(v.TagName, StringComparison.OrdinalIgnoreCase)));
            });
        }

        #endregion

        #region [ IReadTagValuesAtTimes ]

        [TestMethod]
        public Task ReadTagValuesAtTimesRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IReadTagValuesAtTimes>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadTagValuesAtTimes>();
                    return;
                }

                var tagDetails = GetReadTagValuesQueryDetails();
                var sampleTimes = new List<DateTime>();
                var baseInterval = (tagDetails.HistoryEndTime - tagDetails.HistoryStartTime).TotalSeconds / 50;
                var counter = 0;

                for (var ts = tagDetails.HistoryStartTime; ts <= tagDetails.HistoryEndTime; ts = ts.AddSeconds(baseInterval * (counter % 10) + 1)) {
                    ++counter;
                    sampleTimes.Add(ts);
                }

                var channel = await feature.ReadTagValuesAtTimes(
                    context,
                    new ReadTagValuesAtTimesRequest() {
                        Tags = new[] { tagDetails.Id },
                        UtcSampleTimes = sampleTimes.ToArray()
                    },
                    default
                );
                var values = await channel.ToEnumerable();

                Assert.AreEqual(sampleTimes.Count, values.Count());
                Assert.IsTrue(values.All(v => tagDetails.Id.Equals(v.TagId) || tagDetails.Id.Equals(v.TagName, StringComparison.OrdinalIgnoreCase)));

                for (var i = 0; i < sampleTimes.Count; i++) {
                    var expectedSampleTime = sampleTimes[i];
                    var sample = values.ElementAt(i);

                    Assert.AreEqual(expectedSampleTime, sample.Value.UtcSampleTime);
                }
            });
        }

        #endregion

        #region [ IEventMessagePush ]

        [TestMethod]
        public Task ActiveEventMessageSubscriptionShouldReceiveMessages() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IEventMessagePush>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IEventMessagePush>();
                    return;
                }

                using (var subscription = await feature.Subscribe(context, EventMessageSubscriptionType.Active)) {
                    Assert.IsNotNull(subscription);
                    Assert.IsTrue(subscription.IsStarted);

                    await Task.Delay(1000, default);
                    await EmitTestEvent(adapter, EventMessageSubscriptionType.Active);

                    using (var ctSource = new CancellationTokenSource(1000)) {
                        var val = await subscription.Reader.ReadAsync(ctSource.Token);
                        Assert.IsNotNull(val);
                    }
                }
            });
        }


        [TestMethod]
        public Task PassiveEventMessageSubscriptionShouldReceiveMessages() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IEventMessagePush>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IEventMessagePush>();
                    return;
                }

                using (var subscription = await feature.Subscribe(context, EventMessageSubscriptionType.Passive)) {
                    Assert.IsNotNull(subscription);
                    Assert.IsTrue(subscription.IsStarted);

                    await Task.Delay(1000, default);
                    await EmitTestEvent(adapter, EventMessageSubscriptionType.Passive);

                    using (var ctSource = new CancellationTokenSource(1000)) {
                        var val = await subscription.Reader.ReadAsync(ctSource.Token);
                        Assert.IsNotNull(val);
                    }
                }
            });
        }

        #endregion

        #region [ IReadEventMessagesForTimeRange ]

        [DataTestMethod]
        [DataRow(EventReadDirection.Forwards)]
        [DataRow(EventReadDirection.Backwards)]
        public Task ReadEventMessagesForTimeRangeRequestShouldReturnResults(EventReadDirection direction) {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IReadEventMessagesForTimeRange>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadEventMessagesForTimeRange>();
                    return;
                }

                var queryDetails = GetReadEventMessagesQueryDetails();

                var channel = await feature.ReadEventMessagesForTimeRange(
                    context,
                    new ReadEventMessagesForTimeRangeRequest() {
                        UtcStartTime = queryDetails.HistoryStartTime,
                        UtcEndTime = queryDetails.HistoryEndTime,
                        PageSize = 10,
                        Page = 1,
                        Direction = direction
                    },
                    default
                );
                var messages = await channel.ToEnumerable();

                Assert.IsTrue(messages.Any());
                Assert.IsTrue(messages.All(m => m.UtcEventTime >= queryDetails.HistoryStartTime && m.UtcEventTime <= queryDetails.HistoryEndTime));

                // Ensure that messages were returned in the correct chronological order.
                if (direction == EventReadDirection.Forwards) {
                    Assert.IsTrue(messages.First().UtcEventTime <= messages.Last().UtcEventTime);
                }
                else {
                    Assert.IsTrue(messages.First().UtcEventTime >= messages.Last().UtcEventTime);
                }

                // Now ensure that the next page does not contain any messages returned in the 
                // first page, and that the timestamps of the event messages are correct in 
                // relation to the first page and the read direction.

                var channel2 = await feature.ReadEventMessagesForTimeRange(
                    context,
                    new ReadEventMessagesForTimeRangeRequest() {
                        UtcStartTime = queryDetails.HistoryStartTime,
                        UtcEndTime = queryDetails.HistoryEndTime,
                        PageSize = 10,
                        Page = 2,
                        Direction = direction
                    },
                    default
                );
                var messages2 = await channel2.ToEnumerable();

                if (messages2.Any()) {
                    if (direction == EventReadDirection.Forwards) {
                        Assert.IsTrue(messages2.First().UtcEventTime >= messages.Last().UtcEventTime);
                    }
                    else {
                        Assert.IsTrue(messages2.First().UtcEventTime <= messages.Last().UtcEventTime);
                    }
                }
            });
        }

        #endregion

        #region [ IReadEventMessagesForTimeRange ]

        [DataTestMethod]
        [DataRow(EventReadDirection.Forwards)]
        [DataRow(EventReadDirection.Backwards)]
        public Task ReadEventMessagesUsingCursorRequestShouldReturnResults(EventReadDirection direction) {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IReadEventMessagesUsingCursor>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadEventMessagesUsingCursor>();
                    return;
                }

                var queryDetails = GetReadEventMessagesQueryDetails();

                var channel = await feature.ReadEventMessagesUsingCursor(
                    context,
                    new ReadEventMessagesUsingCursorRequest() {
                        CursorPosition = null,
                        PageSize = 10,
                        Direction = direction
                    },
                    default
                );
                var messages = await channel.ToEnumerable();

                Assert.IsTrue(messages.Any());

                // Ensure that messages were returned in the correct chronological order.
                if (direction == EventReadDirection.Forwards) {
                    Assert.IsTrue(messages.First().UtcEventTime <= messages.Last().UtcEventTime);
                }
                else {
                    Assert.IsTrue(messages.First().UtcEventTime >= messages.Last().UtcEventTime);
                }

                var nextCursor = messages.Last().CursorPosition;
                Assert.IsNotNull(nextCursor);

                // Now ensure that the next page does not contain any messages returned in the 
                // first page, and that the timestamps of the event messages are correct in 
                // relation to the first page and the read direction.

                var channel2 = await feature.ReadEventMessagesUsingCursor(
                    context,
                    new ReadEventMessagesUsingCursorRequest() {
                        CursorPosition = nextCursor,
                        PageSize = 10,
                        Direction = direction
                    },
                    default
                );
                var messages2 = await channel2.ToEnumerable();

                if (messages2.Any()) {
                    if (direction == EventReadDirection.Forwards) {
                        Assert.IsTrue(messages2.First().UtcEventTime >= messages.Last().UtcEventTime);
                    }
                    else {
                        Assert.IsTrue(messages2.First().UtcEventTime <= messages.Last().UtcEventTime);
                    }
                }
            });
        }

        #endregion

    }


    public class ReadTagValuesQueryDetails {

        public string Id { get; }

        public DateTime HistoryStartTime { get; set; }

        public DateTime HistoryEndTime { get; set; }


        public ReadTagValuesQueryDetails(string id) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

    }


    public class ReadEventMessagesQueryDetails {

        public DateTime HistoryStartTime { get; set; }

        public DateTime HistoryEndTime { get; set; }

    }

}
