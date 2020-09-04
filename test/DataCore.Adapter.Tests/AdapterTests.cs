using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Events;
using DataCore.Adapter.Extensions;
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

        protected abstract Task EmitTestEvent(TAdapter adapter, EventMessageSubscriptionType subscriptionType, string topic);

        protected virtual IEnumerable<ExtensionFeatureOperationType> ExpectedExtensionFeatureOperationTypes() {
            return new[] { 
                ExtensionFeatureOperationType.Invoke,
                ExtensionFeatureOperationType.Stream,
                ExtensionFeatureOperationType.DuplexStream
            };
        }


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
                await adapter.StartAsync(CancellationToken);
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

                var health = await feature.CheckHealthAsync(context, CancellationToken);
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
                var feature = adapter.Features.Get<IHealthCheck>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IHealthCheck>();
                    return;
                }

                var subscription = await feature.Subscribe(context, CancellationToken);
                Assert.IsNotNull(subscription);

                await Task.Delay(1000, CancellationToken);

                using (var ctSource = new CancellationTokenSource(1000)) {
                    var health = await subscription.ReadAsync(ctSource.Token);
                    VerifyHealthCheckResult(health);
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

                var channel = await feature.FindTags(context, new FindTagsRequest(), CancellationToken);
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
                }, CancellationToken);
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
                }, CancellationToken);
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

                var tagDetails = GetReadTagValuesQueryDetails();

                var subscription = await feature.Subscribe(context, new CreateSnapshotTagValueSubscriptionRequest() { 
                    Tags = new[] { tagDetails.Id }
                }, CancellationToken);
                Assert.IsNotNull(subscription);

                using (var ctSource = new CancellationTokenSource(1000)) {
                    var val = await subscription.ReadAsync(ctSource.Token);
                    Assert.IsNotNull(val);
                    Assert.IsTrue(tagDetails.Id.Equals(val.TagId) || tagDetails.Id.Equals(val.TagName, StringComparison.OrdinalIgnoreCase));
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
                    CancellationToken
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
                    CancellationToken
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

                var channel = await feature.GetSupportedDataFunctions(context, CancellationToken);
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
                    CancellationToken
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
                    CancellationToken
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

                var subscription = await feature.Subscribe(context, new CreateEventMessageSubscriptionRequest() { SubscriptionType = EventMessageSubscriptionType.Active }, CancellationToken);
                Assert.IsNotNull(subscription);

                await Task.Delay(1000, CancellationToken);
                await EmitTestEvent(adapter, EventMessageSubscriptionType.Active, null);

                using (var ctSource = new CancellationTokenSource(1000)) {
                    var val = await subscription.ReadAsync(ctSource.Token);
                    Assert.IsNotNull(val);
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

                var subscription = await feature.Subscribe(
                    context, 
                    new CreateEventMessageSubscriptionRequest() { SubscriptionType = EventMessageSubscriptionType.Passive }, 
                    CancellationToken
                );
                Assert.IsNotNull(subscription);

                await Task.Delay(1000, CancellationToken);
                await EmitTestEvent(adapter, EventMessageSubscriptionType.Passive, null);

                using (var ctSource = new CancellationTokenSource(1000)) {
                    var val = await subscription.ReadAsync(ctSource.Token);
                    Assert.IsNotNull(val);
                }
            });
        }

        #endregion

        #region [ IEventMessagePushWithTopics ]

        [TestMethod]
        public Task EventTopicSubscriptionShouldReceiveMessages() {
            var topic = TestContext.TestName;

            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get<IEventMessagePushWithTopics>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IEventMessagePushWithTopics>();
                    return;
                }

                var subscription = await feature.Subscribe(
                    context, 
                    new CreateEventMessageTopicSubscriptionRequest() { 
                        SubscriptionType = EventMessageSubscriptionType.Active,
                        Topics = new[] { topic }
                    }, 
                    CancellationToken
                );

                Assert.IsNotNull(subscription);

                await Task.Delay(1000, CancellationToken);
                await EmitTestEvent(adapter, EventMessageSubscriptionType.Active, topic);

                using (var ctSource = new CancellationTokenSource(1000)) {
                    var val = await subscription.ReadAsync(ctSource.Token);
                    Assert.IsNotNull(val);
                    Assert.AreEqual(topic, val.Topic);
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
                    CancellationToken
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
                    CancellationToken
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
                    CancellationToken
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
                    CancellationToken
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

        #region [ Ping-Pong Extension ]

        [TestMethod]
        public Task PingPongExtensionShouldReturnDescriptor() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var expected = typeof(PingPongExtension).CreateFeatureDescriptor();
                var actual = await feature.GetDescriptor(context, null, default).ConfigureAwait(false);

                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Uri, actual.Uri);
                Assert.AreEqual(expected.DisplayName, actual.DisplayName);
                if (expected.Description == null) {
                    // If the expected description is null, some adapters (e.g. gRPC proxy) will 
                    // return string.Empty as null values are not allowed in gRPC messages.
                    Assert.IsTrue(string.IsNullOrEmpty(actual.Description));
                }
                else {
                    Assert.AreEqual(expected.Description, actual.Description);
                }
            });
        }


        [TestMethod]
        public Task PingPongExtensionShouldReturnAvailableOperations() {
            return RunAdapterTest(async (adapter, context) => {
                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, null, default).ConfigureAwait(false);
                var expectedOperations = ExpectedExtensionFeatureOperationTypes();

                foreach (var type in expectedOperations) {
                    Assert.IsTrue(operations.Any(op => op.OperationType == type), $"Expected to find operation type {type}.");
                }
            });
        }


        [TestMethod]
        public Task PingPongExtensionShouldReturnAvailableOperationsViaInvoke() {
            return RunAdapterTest(async (adapter, context) => {
                if (!ExpectedExtensionFeatureOperationTypes().Contains(ExtensionFeatureOperationType.Invoke)) {
                    Assert.Inconclusive("Invoke operation not available.");
                }

                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, null, default).ConfigureAwait(false);
                var expectedOperations = ExpectedExtensionFeatureOperationTypes();

                operations = operations.Where(x => expectedOperations.Contains(x.OperationType)).ToArray();

                var operationId = new Uri(string.Concat(
                    PingPongExtension.FeatureUri,
                    nameof(IAdapterExtensionFeature.GetOperations),
                    "/",
                    ExtensionFeatureOperationType.Invoke.ToString(),
                    "/"
                ));

                var operationsFromInvoke = await feature.Invoke<IEnumerable<ExtensionFeatureOperationDescriptor>>(context, operationId, default);

                Assert.IsNotNull(operationsFromInvoke);
                foreach (var op in operations) {
                    var invokeOp = operationsFromInvoke.FirstOrDefault(x => x.OperationId.Equals(op.OperationId));
                    Assert.IsNotNull(invokeOp, $"Expected to find operation '{op.OperationId}'.");
                }
            });
        }


        [TestMethod]
        public Task PingPongInvokeMethodShouldReturnCorrectValue() {
            return RunAdapterTest(async (adapter, context) => {
                if (!ExpectedExtensionFeatureOperationTypes().Contains(ExtensionFeatureOperationType.Invoke)) {
                    Assert.Inconclusive("Invoke operation not available.");
                }

                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, null, default).ConfigureAwait(false);

                var operationId = operations.FirstOrDefault(x => x.OperationType == ExtensionFeatureOperationType.Invoke)?.OperationId;
                if (operationId == null) {
                    Assert.Fail("Invoke operation should be available.");
                }

                var pingMessage = new PingMessage() { 
                    CorrelationId = Guid.NewGuid(),
                    UtcClientTime = DateTime.UtcNow
                };

                var pongMessage = await feature.Invoke<PingMessage, PongMessage>(
                    context,
                    operationId,
                    pingMessage,
                    default
                );

                Assert.IsNotNull(pongMessage);
                Assert.AreEqual(pingMessage.CorrelationId, pongMessage.CorrelationId);
            });
        }


        [TestMethod]
        public Task PingPongStreamMethodShouldReturnCorrectValue() {
            return RunAdapterTest(async (adapter, context) => {
                if (!ExpectedExtensionFeatureOperationTypes().Contains(ExtensionFeatureOperationType.Stream)) {
                    Assert.Inconclusive("Stream operation not available.");
                }

                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, null, default).ConfigureAwait(false);

                var operationId = operations.FirstOrDefault(x => x.OperationType == ExtensionFeatureOperationType.Stream)?.OperationId;
                if (operationId == null) {
                    Assert.Fail("Stream operation should be available.");
                }

                var pingMessage = new PingMessage() {
                    CorrelationId = Guid.NewGuid(),
                    UtcClientTime = DateTime.UtcNow
                };

                var reader = await feature.Stream<PingMessage, PongMessage>(
                    context,
                    operationId,
                    pingMessage,
                    default
                );

                using (var ctSource = new CancellationTokenSource(TimeSpan.FromSeconds(5))) {
                    var ct = ctSource.Token;
                    var pongMessage = await reader.ReadAsync(ct);

                    Assert.IsNotNull(pongMessage);
                    Assert.AreEqual(pingMessage.CorrelationId, pongMessage.CorrelationId);

                    // Should be no more values in the stream
                    Assert.IsFalse(await reader.WaitToReadAsync(ct));
                }
            });
        }


        [TestMethod]
        public Task PingPongDuplexStreamMethodShouldReturnCorrectValue() {
            return RunAdapterTest(async (adapter, context) => {
                if (!ExpectedExtensionFeatureOperationTypes().Contains(ExtensionFeatureOperationType.Stream)) {
                    Assert.Inconclusive("DuplexStream operation not available.");
                }

                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, null, default).ConfigureAwait(false);

                var operationId = operations.FirstOrDefault(x => x.OperationType == ExtensionFeatureOperationType.DuplexStream)?.OperationId;
                if (operationId == null) {
                    Assert.Fail("DuplexStream operation should be available.");
                }

                var pingMessages = new List<PingMessage>();
                for (var i = 0; i < 5; i++) {
                    pingMessages.Add(new PingMessage() {
                        CorrelationId = Guid.NewGuid(),
                        UtcClientTime = DateTime.UtcNow
                    });
                }

                var reader = await feature.DuplexStream<PingMessage, PongMessage>(
                    context,
                    operationId,
                    pingMessages.PublishToChannel(),
                    default
                );

                using (var ctSource = new CancellationTokenSource(TimeSpan.FromSeconds(5))) {
                    var ct = ctSource.Token;

                    var messagesRead = 0;
                    await foreach(var pongMessage in reader.ReadAllAsync(ct).ConfigureAwait(false)) {
                        ++messagesRead;
                        if (messagesRead > pingMessages.Count) {
                            Assert.Fail("Incorrect number of pong messages received.");
                        }

                        var pingMessage = pingMessages[messagesRead - 1];

                        Assert.IsNotNull(pongMessage);
                        Assert.AreEqual(pingMessage.CorrelationId, pongMessage.CorrelationId);
                    }

                    Assert.AreEqual(pingMessages.Count, messagesRead, "Incorrect number of pong messages received.");
                }
            });
        }

        #endregion

        #region [ Miscellaneous Other Tests ]

        [TestMethod]
        public Task BackgroundTaskShouldCancelWhenAdapterIsStopped() {
            return RunAdapterTest(async (adapter, context) => { 
                if (!(adapter is AdapterBase adapterBase)) {
                    Assert.Inconclusive("Test is only applicable to implementations of AdapterBase.");
                    return;
                }

                var tcs = new TaskCompletionSource<bool>();

                adapterBase.BackgroundTaskService.QueueBackgroundWorkItem(new IntelligentPlant.BackgroundTasks.BackgroundWorkItem(async ct => {
                    try {
                        await Task.Delay(-1, ct);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception e) {
                        tcs.TrySetException(e);
                    }
                    finally {
                        tcs.TrySetResult(true);
                    }
                }));

                await adapter.StopAsync(CancellationToken);

                using (var ctSource = new CancellationTokenSource(1000)) {
                    await Task.WhenAny(Task.Delay(-1, ctSource.Token), tcs.Task);
                    ctSource.Token.ThrowIfCancellationRequested();

                    await tcs.Task;
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
