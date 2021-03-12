using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Events;
using DataCore.Adapter.Extensions;
using DataCore.Adapter.RealTimeData;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {
    public abstract class AdapterTests<TAdapter> : AdapterTestsBase<TAdapter> where TAdapter : class, IAdapter {

        protected virtual IEnumerable<ExtensionFeatureOperationType> ExpectedExtensionFeatureOperationTypes() {
            return new[] { 
                ExtensionFeatureOperationType.Invoke,
                ExtensionFeatureOperationType.Stream,
                ExtensionFeatureOperationType.DuplexStream
            };
        }


        protected override IServiceScope CreateServiceScope(TestContext context) {
            return AssemblyInitializer.ApplicationServices.CreateScope();
        }

        #region [ IEventMessagePush ]

        [TestMethod]
        public Task ActiveEventMessageSubscriptionShouldReceiveMessages() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IEventMessagePush>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IEventMessagePush>();
                    return;
                }

                var subscription = await feature.Subscribe(context, new CreateEventMessageSubscriptionRequest() { SubscriptionType = EventMessageSubscriptionType.Active }, ct);
                Assert.IsNotNull(subscription);

                await Task.Delay(1000, ct);
                await EmitTestEvent(TestContext, adapter, ct).ConfigureAwait(false);

                using (var ctSource = new CancellationTokenSource(1000)) {
                    var val = await subscription.ReadAsync(ctSource.Token);
                    Assert.IsNotNull(val);
                }
            });
        }


        [TestMethod]
        public Task PassiveEventMessageSubscriptionShouldReceiveMessages() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IEventMessagePush>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IEventMessagePush>();
                    return;
                }

                var subscription = await feature.Subscribe(
                    context, 
                    new CreateEventMessageSubscriptionRequest() { SubscriptionType = EventMessageSubscriptionType.Passive }, 
                    ct
                );
                Assert.IsNotNull(subscription);

                await Task.Delay(1000, ct);
                await EmitTestEvent(TestContext, adapter, ct).ConfigureAwait(false);


                using (var ctSource = new CancellationTokenSource(1000)) {
                    var val = await subscription.ReadAsync(ctSource.Token);
                    Assert.IsNotNull(val);
                }
            });
        }

        #endregion

        #region [ IWriteSnapshotTagValues / IWriteHistoricalTagValues ]

        [TestMethod]
        public Task WriteSnapshotTagValuesViaChannelShouldSucceed() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IWriteSnapshotTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IWriteSnapshotTagValues>();
                    return;
                }

                var now = DateTime.UtcNow;
                var values = new List<WriteTagValueItem>();
                for (var i = 0; i < 5; i++) {
                    values.Add(new WriteTagValueItem() {  
                        CorrelationId = Guid.NewGuid().ToString(),
                        TagId = TestContext.TestName,
                        Value = new TagValueBuilder().WithUtcSampleTime(now.AddMinutes(-1 * (5 - i))).WithValue(i).Build()
                    });
                }

                var writeResults = await feature.WriteSnapshotTagValues(context, values.PublishToChannel(), ct);
                var index = 0;

                while (await writeResults.WaitToReadAsync(ct)) {
                    while (writeResults.TryRead(out var item)) {
                        if (index > values.Count) {
                            Assert.Fail("Too many results received");
                        }
                        var expected = values[index];

                        Assert.IsNotNull(item);
                        Assert.AreEqual(expected.CorrelationId, item.CorrelationId);

                        ++index;
                    }
                }
            });
        }


        [TestMethod]
        public Task WriteSnapshotTagValuesViaEnumerableShouldSucceed() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IWriteSnapshotTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IWriteSnapshotTagValues>();
                    return;
                }

                var now = DateTime.UtcNow;
                var values = new List<WriteTagValueItem>();
                for (var i = 0; i < 5; i++) {
                    values.Add(new WriteTagValueItem() {
                        CorrelationId = Guid.NewGuid().ToString(),
                        TagId = TestContext.TestName,
                        Value = new TagValueBuilder().WithUtcSampleTime(now.AddMinutes(-1 * (5 - i))).WithValue(i).Build()
                    });
                }

                var writeResults = await feature.WriteSnapshotTagValues(context, values, ct);
                
                Assert.AreEqual(values.Count, writeResults.Count());

                var index = 0;
                foreach (var item in writeResults) {
                    var expected = values[index];

                    Assert.IsNotNull(item);
                    Assert.AreEqual(expected.CorrelationId, item.CorrelationId);

                    ++index;
                }
            });
        }


        [TestMethod]
        public Task WriteHistoricalTagValuesViaChannelShouldSucceed() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IWriteHistoricalTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IWriteHistoricalTagValues>();
                    return;
                }

                var now = DateTime.UtcNow;
                var values = new List<WriteTagValueItem>();
                for (var i = 0; i < 5; i++) {
                    values.Add(new WriteTagValueItem() {
                        CorrelationId = Guid.NewGuid().ToString(),
                        TagId = TestContext.TestName,
                        Value = new TagValueBuilder().WithUtcSampleTime(now.AddDays(-1).AddMinutes(-1 * (5 - i))).WithValue(i).Build()
                    });
                }

                var writeResults = await feature.WriteHistoricalTagValues(context, values.PublishToChannel(), ct);
                var index = 0;

                while (await writeResults.WaitToReadAsync(ct)) {
                    while (writeResults.TryRead(out var item)) {
                        if (index > values.Count) {
                            Assert.Fail("Too many results received");
                        }
                        var expected = values[index];

                        Assert.IsNotNull(item);
                        Assert.AreEqual(expected.CorrelationId, item.CorrelationId);

                        ++index;
                    }
                }
            });
        }


        [TestMethod]
        public Task WriteHistoricalTagValuesViaEnumerableShouldSucceed() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IWriteHistoricalTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IWriteHistoricalTagValues>();
                    return;
                }

                var now = DateTime.UtcNow;
                var values = new List<WriteTagValueItem>();
                for (var i = 0; i < 5; i++) {
                    values.Add(new WriteTagValueItem() {
                        CorrelationId = Guid.NewGuid().ToString(),
                        TagId = TestContext.TestName,
                        Value = new TagValueBuilder().WithUtcSampleTime(now.AddDays(-1).AddMinutes(-1 * (5 - i))).WithValue(i).Build()
                    });
                }

                var writeResults = await feature.WriteHistoricalTagValues(context, values, ct);

                Assert.AreEqual(values.Count, writeResults.Count());

                var index = 0;
                foreach (var item in writeResults) {
                    var expected = values[index];

                    Assert.IsNotNull(item);
                    Assert.AreEqual(expected.CorrelationId, item.CorrelationId);

                    ++index;
                }
            });
        }

        #endregion

        #region [ IWriteEventMessages ]

        [TestMethod]
        public Task WriteEventMessagesViaChannelShouldSucceed() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IWriteEventMessages>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IWriteEventMessages>();
                    return;
                }

                var now = DateTime.UtcNow;
                var values = new List<WriteEventMessageItem>();
                for (var i = 0; i < 5; i++) {
                    values.Add(new WriteEventMessageItem() {
                        CorrelationId = Guid.NewGuid().ToString(),
                        EventMessage = EventMessageBuilder.Create().Build()
                    });
                }

                var writeResults = await feature.WriteEventMessages(context, values.PublishToChannel(), ct);
                var index = 0;

                while (await writeResults.WaitToReadAsync(ct)) {
                    while (writeResults.TryRead(out var item)) {
                        if (index > values.Count) {
                            Assert.Fail("Too many results received");
                        }
                        var expected = values[index];

                        Assert.IsNotNull(item);
                        Assert.AreEqual(expected.CorrelationId, item.CorrelationId);

                        ++index;
                    }
                }
            });
        }


        [TestMethod]
        public Task WriteEventMessagesViaEnumerableShouldSucceed() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IWriteEventMessages>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IWriteEventMessages>();
                    return;
                }

                var now = DateTime.UtcNow;
                var values = new List<WriteEventMessageItem>();
                for (var i = 0; i < 5; i++) {
                    values.Add(new WriteEventMessageItem() {
                        CorrelationId = Guid.NewGuid().ToString(),
                        EventMessage = EventMessageBuilder.Create().Build()
                    });
                }

                var writeResults = await feature.WriteEventMessages(context, values, ct);

                Assert.AreEqual(values.Count, writeResults.Count());

                var index = 0;
                foreach (var item in writeResults) {
                    var expected = values[index];

                    Assert.IsNotNull(item);
                    Assert.AreEqual(expected.CorrelationId, item.CorrelationId);

                    ++index;
                }
            });
        }

        #endregion

        #region [ Extensions ]

        [TestMethod]
        public Task ExtensionShouldBeResolvedViaAbsoluteUri() {
            return RunAdapterTest((adapter, context, ct) => {
                Assert.IsNotNull(adapter.GetExtensionFeature(new Uri(PingPongExtension.FeatureUri)));
                Assert.IsTrue(adapter.TryGetExtensionFeature(new Uri(PingPongExtension.FeatureUri), out var f));
                Assert.IsNotNull(f);

                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task ExtensionShouldBeResolvedViaRelativeUri() {
            return RunAdapterTest((adapter, context, ct) => {
                Assert.IsNotNull(adapter.GetExtensionFeature(new Uri(PingPongExtension.RelativeFeatureUri, UriKind.Relative)));
                Assert.IsTrue(adapter.TryGetExtensionFeature(new Uri(PingPongExtension.RelativeFeatureUri, UriKind.Relative), out var f));
                Assert.IsNotNull(f);

                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task ExtensionShouldBeResolvedViaAbsoluteUriString() {
            return RunAdapterTest((adapter, context, ct) => {
                Assert.IsNotNull(adapter.GetExtensionFeature(PingPongExtension.FeatureUri));
                Assert.IsTrue(adapter.TryGetExtensionFeature(PingPongExtension.FeatureUri, out var f));
                Assert.IsNotNull(f);

                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task ExtensionShouldBeResolvedViaRelativeUriString() {
            return RunAdapterTest((adapter, context, ct) => {
                Assert.IsNotNull(adapter.GetExtensionFeature(PingPongExtension.RelativeFeatureUri));
                Assert.IsTrue(adapter.TryGetExtensionFeature(PingPongExtension.RelativeFeatureUri, out var f));
                Assert.IsNotNull(f);

                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task PingPongExtensionShouldReturnDescriptor() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var expected = typeof(PingPongExtension).CreateFeatureDescriptor();
                var actual = await feature.GetDescriptor(context, PingPongExtension.FeatureUri, ct).ConfigureAwait(false);

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
        public Task HelloWorldExtensionShouldReturnDescriptor() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get(HelloWorldConstants.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(HelloWorldConstants.FeatureUri);
                    return;
                }

                var expected = typeof(IHelloWorld).CreateFeatureDescriptor();
                var actual = await feature.GetDescriptor(context, HelloWorldConstants.FeatureUri, ct).ConfigureAwait(false);

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
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, PingPongExtension.FeatureUri, ct).ConfigureAwait(false);
                var expectedOperations = ExpectedExtensionFeatureOperationTypes();

                foreach (var type in expectedOperations) {
                    Assert.IsTrue(operations.Any(op => op.OperationType == type), $"Expected to find operation type {type}.");
                }
            });
        }


        [TestMethod]
        public Task HelloWorldExtensionShouldReturnAvailableOperations() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get(HelloWorldConstants.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(HelloWorldConstants.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, HelloWorldConstants.FeatureUri, ct).ConfigureAwait(false);
                Assert.AreEqual(1, operations.Count());

                var op = operations.First();
                Assert.AreEqual(ExtensionFeatureOperationType.Invoke, op.OperationType);
                var expectedOpId = new Uri(string.Concat(
                    HelloWorldConstants.FeatureUri,
                    ExtensionFeatureOperationType.Invoke.ToString().ToLowerInvariant(),
                    "/",
                    nameof(IHelloWorld.Greet),
                    "/"
                ));
                Assert.AreEqual(expectedOpId, op.OperationId);
            });
        }


        [TestMethod]
        public Task PingPongExtensionShouldReturnAvailableOperationsViaInvoke() {
            var encoder = AssemblyInitializer.ApplicationServices.GetRequiredService<IObjectEncoder>();

            return RunAdapterTest(async (adapter, context, ct) => {
                if (!ExpectedExtensionFeatureOperationTypes().Contains(ExtensionFeatureOperationType.Invoke)) {
                    Assert.Inconclusive("Invoke operation not available.");
                }

                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, PingPongExtension.FeatureUri, ct).ConfigureAwait(false);
                var expectedOperations = ExpectedExtensionFeatureOperationTypes();

                operations = operations.Where(x => expectedOperations.Contains(x.OperationType)).ToArray();

                var operationId = new Uri(string.Concat(
                    PingPongExtension.FeatureUri,
                    ExtensionFeatureOperationType.Invoke.ToString().ToLowerInvariant(),
                    "/",
                    nameof(IAdapterExtensionFeature.GetOperations),
                    "/"
                ));

                var invokeResult = await feature.Invoke(context, new InvocationRequest() { 
                    OperationId = operationId
                }, ct).ConfigureAwait(false);

                var operationsFromInvoke = invokeResult.Results.Select(x => encoder.Decode<ExtensionFeatureOperationDescriptor>(x)).ToArray();

                Assert.IsNotNull(operationsFromInvoke);
                foreach (var op in operations) {
                    var invokeOp = operationsFromInvoke.FirstOrDefault(x => x.OperationId.Equals(op.OperationId));
                    Assert.IsNotNull(invokeOp, $"Expected to find operation '{op.OperationId}'.");
                }
            });
        }


        [TestMethod]
        public Task HelloWorldExtensionShouldReturnAvailableOperationsViaInvoke() {
            var encoder = AssemblyInitializer.ApplicationServices.GetRequiredService<IObjectEncoder>();

            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get(HelloWorldConstants.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(HelloWorldConstants.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, HelloWorldConstants.FeatureUri, ct).ConfigureAwait(false);

                var operationId = new Uri(string.Concat(
                    HelloWorldConstants.FeatureUri,
                    ExtensionFeatureOperationType.Invoke.ToString().ToLowerInvariant(),
                    "/", 
                    nameof(IAdapterExtensionFeature.GetOperations),
                    "/"
                ));

                var invokeResult = await feature.Invoke(context, new InvocationRequest() {
                    OperationId = operationId
                }, ct).ConfigureAwait(false);

                var operationsFromInvoke = invokeResult.Results.Select(x => encoder.Decode<ExtensionFeatureOperationDescriptor>(x)).ToArray();

                Assert.IsNotNull(operationsFromInvoke);
                foreach (var op in operations) {
                    var invokeOp = operationsFromInvoke.FirstOrDefault(x => x.OperationId.Equals(op.OperationId));
                    Assert.IsNotNull(invokeOp, $"Expected to find operation '{op.OperationId}'.");
                }
            });
        }


        [TestMethod]
        public Task PingPongInvokeMethodShouldReturnCorrectValue() {
            var encoder = AssemblyInitializer.ApplicationServices.GetRequiredService<IObjectEncoder>();

            return RunAdapterTest(async (adapter, context, ct) => {
                if (!ExpectedExtensionFeatureOperationTypes().Contains(ExtensionFeatureOperationType.Invoke)) {
                    Assert.Inconclusive("Invoke operation not available.");
                }

                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, PingPongExtension.FeatureUri, ct).ConfigureAwait(false);

                var operationId = operations.FirstOrDefault(x => x.OperationType == ExtensionFeatureOperationType.Invoke && x.Name.Contains(nameof(PingPongExtension.PingInvoke)))?.OperationId;
                if (operationId == null) {
                    Assert.Fail("Invoke operation should be available.");
                }

                var pingMessage = new PingMessage() { 
                    CorrelationId = Guid.NewGuid(),
                    UtcClientTime = DateTime.UtcNow
                };

                var invokeResult = await feature.Invoke(context, new InvocationRequest() { 
                    OperationId = operationId,
                    Arguments = new Variant[] { encoder.Encode(pingMessage) }
                }, ct).ConfigureAwait(false);

                var pongMessage = encoder.Decode<PongMessage>(invokeResult.Results[0]);

                Assert.IsNotNull(pongMessage);
                Assert.AreEqual(pingMessage.CorrelationId, pongMessage.CorrelationId);
            });
        }


        [TestMethod]
        public Task PingPongStreamMethodShouldReturnCorrectValue() {
            var encoder = AssemblyInitializer.ApplicationServices.GetRequiredService<IObjectEncoder>();

            return RunAdapterTest(async (adapter, context, ct) => {
                if (!ExpectedExtensionFeatureOperationTypes().Contains(ExtensionFeatureOperationType.Stream)) {
                    Assert.Inconclusive("Stream operation not available.");
                }

                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, PingPongExtension.FeatureUri, ct).ConfigureAwait(false);

                var operationId = operations.FirstOrDefault(x => x.OperationType == ExtensionFeatureOperationType.Stream)?.OperationId;
                if (operationId == null) {
                    Assert.Fail("Stream operation should be available.");
                }

                var pingMessage = new PingMessage() {
                    CorrelationId = Guid.NewGuid(),
                    UtcClientTime = DateTime.UtcNow
                };

                var reader = await feature.Stream(context, new InvocationRequest() {
                    OperationId = operationId,
                    Arguments = new Variant[] { encoder.Encode(pingMessage) }
                }, ct).ConfigureAwait(false);

                var streamResult = await reader.ReadAsync(ct).ConfigureAwait(false);
                var pongMessage = encoder.Decode<PongMessage>(streamResult.Results[0]);

                Assert.IsNotNull(pongMessage);
                Assert.AreEqual(pingMessage.CorrelationId, pongMessage.CorrelationId);

                // Should be no more values in the stream
                Assert.IsFalse(await reader.WaitToReadAsync(ct));
            });
        }


        [TestMethod]
        public Task PingPongDuplexStreamMethodShouldReturnCorrectValue() {
            var encoder = AssemblyInitializer.ApplicationServices.GetRequiredService<IObjectEncoder>();

            return RunAdapterTest(async (adapter, context, ct) => {
                if (!ExpectedExtensionFeatureOperationTypes().Contains(ExtensionFeatureOperationType.Stream)) {
                    Assert.Inconclusive("DuplexStream operation not available.");
                }

                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, PingPongExtension.FeatureUri, ct).ConfigureAwait(false);

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

                var reader = await feature.DuplexStream(
                    context,
                    new InvocationRequest() { 
                        OperationId = operationId
                    },
                    pingMessages.Select(x => new InvocationStreamItem() {
                        Arguments = new Variant[] { encoder.Encode(x) }
                    }).PublishToChannel(),
                    ct
                );

                var messagesRead = 0;

                while (await reader.WaitToReadAsync(ct)) {
                    while (reader.TryRead(out var streamResult)) {
                        var pongMessage = encoder.Decode<PongMessage>(streamResult.Results[0]);

                        ++messagesRead;
                        if (messagesRead > pingMessages.Count) {
                            Assert.Fail("Incorrect number of pong messages received.");
                        }

                        var pingMessage = pingMessages[messagesRead - 1];

                        Assert.IsNotNull(pongMessage);
                        Assert.AreEqual(pingMessage.CorrelationId, pongMessage.CorrelationId);
                    }
                }

                Assert.AreEqual(pingMessages.Count, messagesRead, "Incorrect number of pong messages received.");
            });
        }


        [TestMethod]
        public Task PingPongArray1DInvokeMethodShouldReturnCorrectValue() {
            var encoder = AssemblyInitializer.ApplicationServices.GetRequiredService<IObjectEncoder>();

            return RunAdapterTest(async (adapter, context, ct) => {
                if (!ExpectedExtensionFeatureOperationTypes().Contains(ExtensionFeatureOperationType.Invoke)) {
                    Assert.Inconclusive("Invoke operation not available.");
                }

                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, PingPongExtension.FeatureUri, ct).ConfigureAwait(false);

                var operationId = operations.FirstOrDefault(x => x.OperationType == ExtensionFeatureOperationType.Invoke && x.Name.Contains(nameof(PingPongExtension.PingArray1D)))?.OperationId;
                if (operationId == null) {
                    Assert.Fail("Invoke operation should be available.");
                }

                var pingMessages = new[] {
                    new PingMessage() {
                        CorrelationId = Guid.NewGuid(),
                        UtcClientTime = DateTime.UtcNow
                    },
                    new PingMessage() {
                        CorrelationId = Guid.NewGuid(),
                        UtcClientTime = DateTime.UtcNow
                    },
                    new PingMessage() {
                        CorrelationId = Guid.NewGuid(),
                        UtcClientTime = DateTime.UtcNow
                    }
                };

                var invokeResult = await feature.Invoke(context, new InvocationRequest() {
                    OperationId = operationId,
                    Arguments = new Variant[] { 
                        encoder.Encode(pingMessages)
                    }
                }, ct).ConfigureAwait(false);

                var pongMessages = encoder.Decode<PongMessage[]>(invokeResult.Results[0]);

                Assert.AreEqual(pingMessages.Length, pongMessages.Length);

                for (var i = 0; i < pingMessages.Length; i++) {
                    var ping = pingMessages[i];
                    var pong = pongMessages[i];
                    Assert.IsNotNull(pong);
                    Assert.AreEqual(ping.CorrelationId, pong.CorrelationId);
                }
            });
        }


        [TestMethod]
        public Task PingPongArray2DInvokeMethodShouldReturnCorrectValue() {
            var encoder = AssemblyInitializer.ApplicationServices.GetRequiredService<IObjectEncoder>();

            return RunAdapterTest(async (adapter, context, ct) => {
                if (!ExpectedExtensionFeatureOperationTypes().Contains(ExtensionFeatureOperationType.Invoke)) {
                    Assert.Inconclusive("Invoke operation not available.");
                }

                var feature = adapter.Features.Get(PingPongExtension.FeatureUri) as IAdapterExtensionFeature;
                if (feature == null) {
                    AssertFeatureNotImplemented(PingPongExtension.FeatureUri);
                    return;
                }

                var operations = await feature.GetOperations(context, PingPongExtension.FeatureUri, ct).ConfigureAwait(false);

                var operationId = operations.FirstOrDefault(x => x.OperationType == ExtensionFeatureOperationType.Invoke && x.Name.Contains(nameof(PingPongExtension.PingArray2D)))?.OperationId;
                if (operationId == null) {
                    Assert.Fail("Invoke operation should be available.");
                }

                var pingMessages = new[,] {
                    {
                        new PingMessage() {
                            CorrelationId = Guid.NewGuid(),
                            UtcClientTime = DateTime.UtcNow
                        },
                        new PingMessage() {
                            CorrelationId = Guid.NewGuid(),
                            UtcClientTime = DateTime.UtcNow
                        },
                        new PingMessage() {
                            CorrelationId = Guid.NewGuid(),
                            UtcClientTime = DateTime.UtcNow
                        }
                    },
                    {
                        new PingMessage() {
                            CorrelationId = Guid.NewGuid(),
                            UtcClientTime = DateTime.UtcNow
                        },
                        new PingMessage() {
                            CorrelationId = Guid.NewGuid(),
                            UtcClientTime = DateTime.UtcNow
                        },
                        new PingMessage() {
                            CorrelationId = Guid.NewGuid(),
                            UtcClientTime = DateTime.UtcNow
                        }
                    }
                };

                var invokeResult = await feature.Invoke(context, new InvocationRequest() {
                    OperationId = operationId,
                    Arguments = new Variant[] {
                        new Variant(encoder.Encode(pingMessages))
                    }
                }, ct).ConfigureAwait(false);

                var pongMessages = encoder.Decode<PongMessage[,]>(invokeResult.Results[0]);

                Assert.AreEqual(pingMessages.Rank, pongMessages.Rank);

                for (var i = 0; i < pingMessages.Rank; i++) {
                    Assert.AreEqual(pingMessages.GetLength(i), pongMessages.GetLength(i));
                }

                var len0 = pingMessages.GetLength(0);
                var len1 = pingMessages.GetLength(1);

                for (var i = 0; i < len0; i++) {
                    for (var j = 0; j < len1; j++) {
                        var ping = pingMessages[i, j];
                        var pong = pongMessages[i, j];
                        Assert.IsNotNull(pong);
                        Assert.AreEqual(ping.CorrelationId, pong.CorrelationId);
                    }
                }
            });
        }

        #endregion

    }

}
