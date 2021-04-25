using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class SubscriptionTests : TestsBase {

        [TestMethod]
        public async Task SnapshotSubscriptionManagerShouldNotifyWhenSubscriptionIsAdded() {
            var options = new SnapshotTagValuePushOptions() {
                TagResolver = (ctx, names, ct) => new ValueTask<IEnumerable<TagIdentifier>>(names.Select(name => new TagIdentifier(name, name)).ToArray())
            };

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                feature.SubscriptionAdded += sub => tcs.TrySetResult(true);

                _ = Task.Run(async () => {
                    try {
                        await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateSnapshotTagValueSubscriptionRequest() { 
                                Tags = new[] { TestContext.TestName } 
                            },
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken);
                    }
                    catch { }
                });

                CancelAfter(TimeSpan.FromSeconds(1));
                var success = await tcs.Task.WithCancellation(CancellationToken);
                Assert.IsTrue(success);
            }
        }


        [TestMethod]
        public async Task SnapshotSubscriptionManagerShouldNotifyWhenSubscriptionIsCancelled() {
            var options = new SnapshotTagValuePushOptions() {
                TagResolver = (ctx, names, ct) => new ValueTask<IEnumerable<TagIdentifier>>(names.Select(name => new TagIdentifier(name, name)).ToArray())
            };

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                feature.SubscriptionCancelled += sub => tcs.TrySetResult(true);

                _ = Task.Run(async () => {
                    try {
                        await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateSnapshotTagValueSubscriptionRequest() { 
                                Tags = new[] { TestContext.TestName }
                            },
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken);
                    }
                    catch { }
                });

                await Task.Delay(100, CancellationToken).ConfigureAwait(false);
                Cancel();
                var success = await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                Assert.IsTrue(success);
            }
        }


        [TestMethod]
        public async Task SnapshotSubscriptionShouldEmitValues() {
            var now = DateTime.UtcNow;

            var options = new SnapshotTagValuePushOptions() {
                TagResolver = (ctx, names, ct) => new ValueTask<IEnumerable<TagIdentifier>>(names.Select(name => new TagIdentifier(name, name)).ToArray())
            };

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                var tcs = new TaskCompletionSource<TagValueQueryResult>();

                _ = Task.Run(async () => {
                    try {
                        tcs.TrySetResult(await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateSnapshotTagValueSubscriptionRequest() {
                                Tags = new[] { TestContext.TestName }
                            },
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken));
                    }
                    catch (OperationCanceledException) {
                        tcs.TrySetCanceled(CancellationToken);
                    }
                    catch (Exception e) {
                        tcs.TrySetException(e);
                    }
                });

                await Task.Delay(100, CancellationToken).ConfigureAwait(false);

                var val = new TagValueBuilder().WithUtcSampleTime(now).WithValue(now.Ticks).Build();
                await feature.ValueReceived(TagValueQueryResult.Create(TestContext.TestName, TestContext.TestName, val));

                CancelAfter(TimeSpan.FromSeconds(1));

                var emitted = await tcs.Task;
                Assert.IsNotNull(emitted);
                Assert.AreEqual(TestContext.TestName, emitted.TagId);
                Assert.AreEqual(TestContext.TestName, emitted.TagName);
                Assert.AreEqual(val.UtcSampleTime, emitted.Value.UtcSampleTime);
                Assert.AreEqual(val.UtcSampleTime.Ticks, emitted.Value.GetValueOrDefault<long>());
            }
        }


        [TestMethod]
        public async Task SnapshotSubscriptionShouldEmitValuesAfterSubscriptionChange() {
            var now = DateTime.UtcNow;

            var options = new SnapshotTagValuePushOptions() {
                TagResolver = (ctx, names, ct) => new ValueTask<IEnumerable<TagIdentifier>>(names.Select(name => new TagIdentifier(name, name)).ToArray())
            };

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                var channel = Channel.CreateUnbounded<TagValueSubscriptionUpdate>();

                var val1 = new TagValueQueryResult(TestContext.TestName, TestContext.TestName, new TagValueBuilder().WithUtcSampleTime(now).WithValue(now.Ticks).Build());
                var val2 = new TagValueQueryResult(TestContext.TestName, TestContext.TestName, new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(1)).WithValue(now.Ticks + TimeSpan.TicksPerSecond).Build());

                _ = Task.Run(async () => {
                    try {
                        // val1 should not be received by the subscription.
                        await feature.ValueReceived(val1, CancellationToken);

                        // val2 should be received by the subscription.
                        await feature.ValueReceived(val2, CancellationToken);

                        await Task.Delay(100, CancellationToken);

                        // Add the subscription change - we should receive the current value for
                        // the tag at the point of subscription.
                        await channel.Writer.WriteAsync(new TagValueSubscriptionUpdate() {
                            Tags = new[] { TestContext.TestName },
                            Action = Common.SubscriptionUpdateAction.Subscribe
                        }, CancellationToken);
                    }
                    catch { }
                }, CancellationToken);

                CancelAfter(TimeSpan.FromSeconds(1));

                var emitted = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateSnapshotTagValueSubscriptionRequest(),
                    channel.Reader.ReadAllAsync(CancellationToken),
                    CancellationToken
                ).FirstOrDefaultAsync(CancellationToken).ConfigureAwait(false);

                Assert.IsNotNull(emitted);
                Assert.AreEqual(TestContext.TestName, emitted.TagId);
                Assert.AreEqual(TestContext.TestName, emitted.TagName);
                Assert.AreEqual(val2.Value.UtcSampleTime, emitted.Value.UtcSampleTime);
                Assert.AreEqual(val2.Value.UtcSampleTime.Ticks, emitted.Value.GetValueOrDefault<long>());
            }
        }


        [TestMethod]
        public async Task SnapshotSubscriptionShouldNotEmitValuesAfterSubscriptionChange() {
            var now = DateTime.UtcNow;

            var options = new SnapshotTagValuePushOptions() {
                TagResolver = (ctx, names, ct) => new ValueTask<IEnumerable<TagIdentifier>>(names.Select(name => new TagIdentifier(name, name)).ToArray())
            };

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                var channel = Channel.CreateUnbounded<TagValueSubscriptionUpdate>();

                var val1 = new TagValueQueryResult(TestContext.TestName, TestContext.TestName, new TagValueBuilder().WithUtcSampleTime(now).WithValue(now.Ticks).Build());
                var val2 = new TagValueQueryResult(TestContext.TestName, TestContext.TestName, new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(1)).WithValue(now.Ticks + TimeSpan.TicksPerSecond).Build());

                _ = Task.Run(async () => {
                    try {
                        await Task.Delay(100, CancellationToken);
                        // val1 should be received by the subscription.
                        await feature.ValueReceived(val1, CancellationToken);

                        channel.Writer.TryWrite(new TagValueSubscriptionUpdate() {
                            Tags = new[] { TestContext.TestName },
                            Action = Common.SubscriptionUpdateAction.Unsubscribe
                        });

                        await Task.Delay(100, CancellationToken);

                        // val2 should not be received by the subscription.
                        await feature.ValueReceived(val2, CancellationToken);
                    }
                    catch { }
                }, CancellationToken);

                CancelAfter(TimeSpan.FromSeconds(1));

                var emittedCount = 0;

                CancelAfter(TimeSpan.FromSeconds(1));

                await using (var enumerator = feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateSnapshotTagValueSubscriptionRequest() {
                        Tags = new[] { TestContext.TestName }
                    },
                    channel.Reader.ReadAllAsync(CancellationToken),
                    CancellationToken
                ).GetAsyncEnumerator(CancellationToken)) {
                    var emitted = await enumerator.MoveNextAsync().ConfigureAwait(false)
                        ? enumerator.Current
                        : null;

                    Assert.IsNotNull(emitted);
                    ++emittedCount;
                    Assert.AreEqual(TestContext.TestName, emitted.TagId);
                    Assert.AreEqual(TestContext.TestName, emitted.TagName);
                    Assert.AreEqual(val1.Value.UtcSampleTime, emitted.Value.UtcSampleTime);
                    Assert.AreEqual(val1.Value.UtcSampleTime.Ticks, emitted.Value.GetValueOrDefault<long>());
                }

                Assert.AreEqual(1, emittedCount, "Only one value should have been emitted.");
            }
        }


        [TestMethod]
        public async Task SnapshotSubscriptionShouldRespectPublishInterval() {
            var generationInterval = TimeSpan.FromMilliseconds(50);
            var publishInterval = TimeSpan.FromSeconds(1);

            var options = new SnapshotTagValuePushOptions() {
                TagResolver = (ctx, names, ct) => new ValueTask<IEnumerable<TagIdentifier>>(names.Select(name => new TagIdentifier(name, name)).ToArray())
            };

            var valueCount = 0;

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                _ = Task.Run(async () => {
                    try {
                        while (!CancellationToken.IsCancellationRequested) {
                            await Task.Delay(50, CancellationToken).ConfigureAwait(false);
                            var val = new TagValueBuilder().WithValue(DateTime.UtcNow.Ticks).Build();
                            await feature.ValueReceived(TagValueQueryResult.Create(TestContext.TestName, TestContext.TestName, val));
                        }
                    }
                    catch (OperationCanceledException) { }
                }, CancellationToken);

                CancelAfter(publishInterval);
                try {
                    await foreach (var val in feature.Subscribe(
                        ExampleCallContext.ForPrincipal(null),
                            new CreateSnapshotTagValueSubscriptionRequest() {
                                PublishInterval = TimeSpan.FromSeconds(1),
                                Tags = new[] { TestContext.TestName }
                            },
                        CancellationToken
                    ).ConfigureAwait(false)) {
                        ++valueCount;
                    }
                }
                catch (OperationCanceledException) { }
            }

            Assert.IsTrue(valueCount <= (publishInterval.TotalSeconds * 2), "Received value count should not be more than 2x publish interval.");
        }


        [TestMethod]
        public async Task SnapshotSubscriptionShouldApplyConcurrencyLimit() {
            var now = DateTime.UtcNow;

            var options = new SnapshotTagValuePushOptions() {
                MaxSubscriptionCount = 1,
                TagResolver = (ctx, names, ct) => new ValueTask<IEnumerable<TagIdentifier>>(names.Select(name => new TagIdentifier(name, name)).ToArray())
            };

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                var tcs1 = new TaskCompletionSource<TagValueQueryResult>();
                var tcs2 = new TaskCompletionSource<TagValueQueryResult>();

                _ = Task.Run(async () => {
                    try {
                        tcs1.TrySetResult(await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateSnapshotTagValueSubscriptionRequest(),
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken));
                    }
                    catch (OperationCanceledException) {
                        tcs1.TrySetCanceled(CancellationToken);
                    }
                    catch (Exception e) {
                        tcs1.TrySetException(e);
                    }
                });

                _ = Task.Run(async () => {
                    try {
                        // Wait for a short while to ensure that this task runs after the first one
                        await Task.Delay(100, CancellationToken).ConfigureAwait(false);
                        tcs2.TrySetResult(await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateSnapshotTagValueSubscriptionRequest(),
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken));
                    }
                    catch (OperationCanceledException) {
                        tcs2.TrySetCanceled(CancellationToken);
                    }
                    catch (Exception e) {
                        tcs2.TrySetException(e);
                    }
                });

                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => tcs2.Task);
            }
        }


        [TestMethod]
        public async Task SnapshotWildcardSubscriptionShouldEmitValues() {
            var now = DateTime.UtcNow;

            var options = new SnapshotTagValuePushOptions() {
                TagResolver = (ctx, names, ct) => new ValueTask<IEnumerable<TagIdentifier>>(names.Select(name => new TagIdentifier(name, name)).ToArray()),
                IsTopicMatch = (subscribed, received, ct) => {
                    // If we subscribe to "tag_root", we should receive messages with a topic of 
                    // e.g. "tag_root/sub_tag".
                    return new ValueTask<bool>(received.Id.Equals(subscribed.Id) || received.Id.StartsWith(subscribed.Id + "/"));
                }
            };

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                // We should receive this message due to the IsTopicMatch delegate.
                var id1 = $"{TestContext.TestName}/Should_Match";
                var msg1 = new TagValueQueryResult(id1, id1, new TagValueBuilder()
                    .WithValue(DateTime.UtcNow.Ticks)
                    .Build()
                );

                // We should not receive this message.
                var id2 = "Should_Not_Match";
                var msg2 = new TagValueQueryResult(id2, id2, new TagValueBuilder()
                    .WithValue(DateTime.UtcNow.Ticks)
                    .Build()
                );

                // We should receive this message since the topic exactly matches our subscription.
                var id3 = TestContext.TestName;
                var msg3 = new TagValueQueryResult(id3, id3, new TagValueBuilder()
                    .WithValue(DateTime.UtcNow.Ticks)
                    .Build()
                );

                _ = Task.Run(async () => {
                    try {
                        await Task.Delay(100, CancellationToken);
                        await feature.ValueReceived(msg1, CancellationToken);
                        await feature.ValueReceived(msg2, CancellationToken);
                        await feature.ValueReceived(msg3, CancellationToken);
                    }
                    catch { }
                }, CancellationToken);

                var messagesReceived = 0;

                CancelAfter(TimeSpan.FromSeconds(1));

                await using (var enumerator = feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateSnapshotTagValueSubscriptionRequest() {
                        Tags = new[] { TestContext.TestName }
                    },
                    CancellationToken
                ).GetAsyncEnumerator(CancellationToken)) {
                    var emitted = await enumerator.MoveNextAsync().ConfigureAwait(false)
                        ? enumerator.Current
                        : null;

                    ++messagesReceived;
                    Assert.IsNotNull(emitted);
                    Assert.AreEqual(msg1.TagId, emitted.TagId);
                    Assert.AreEqual(msg1.TagName, emitted.TagName);
                    Assert.AreEqual(msg1.Value.UtcSampleTime, emitted.Value.UtcSampleTime);
                    Assert.AreEqual(msg1.Value.Value, emitted.Value.Value);

                    emitted = await enumerator.MoveNextAsync().ConfigureAwait(false)
                        ? enumerator.Current
                        : null;

                    ++messagesReceived;
                    Assert.IsNotNull(emitted);
                    Assert.AreEqual(msg3.TagId, emitted.TagId);
                    Assert.AreEqual(msg3.TagName, emitted.TagName);
                    Assert.AreEqual(msg3.Value.UtcSampleTime, emitted.Value.UtcSampleTime);
                    Assert.AreEqual(msg3.Value.Value, emitted.Value.Value);
                }

                Assert.AreEqual(2, messagesReceived, "2 messages should have been received.");
            }
        }


        [TestMethod]
        public async Task EventSubscriptionManagerShouldNotifyWhenSubscriptionIsAdded() {
            var options = new EventMessagePushOptions();

            using (var feature = new EventMessagePush(options, null, null)) {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                feature.SubscriptionAdded += sub => tcs.TrySetResult(true);

                _ = Task.Run(async () => {
                    try {
                        await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateEventMessageSubscriptionRequest(),
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken);
                    }
                    catch { }
                });

                CancelAfter(TimeSpan.FromSeconds(1));
                var success = await tcs.Task.WithCancellation(CancellationToken);
                Assert.IsTrue(success);
            }
        }


        [TestMethod]
        public async Task EventSubscriptionManagerShouldNotifyWhenSubscriptionIsCancelled() {
            var options = new EventMessagePushOptions();

            using (var feature = new EventMessagePush(options, null, null)) {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                feature.SubscriptionCancelled += sub => tcs.TrySetResult(true);

                _ = Task.Run(async () => {
                    try {
                        await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateEventMessageSubscriptionRequest(),
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken);
                    }
                    catch { }
                });

                await Task.Delay(100, CancellationToken).ConfigureAwait(false);
                Cancel();
                var success = await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                Assert.IsTrue(success);
            }
        }


        [TestMethod]
        public async Task EventSubscriptionShouldEmitValues() {
            var now = DateTime.UtcNow;

            var options = new EventMessagePushOptions();

            using (var feature = new EventMessagePush(options, null, null)) {
                var tcs = new TaskCompletionSource<EventMessage>();

                _ = Task.Run(async () => {
                    try {
                        tcs.TrySetResult(await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateEventMessageSubscriptionRequest(),
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken));
                    }
                    catch (OperationCanceledException) {
                        tcs.TrySetCanceled(CancellationToken);
                    }
                    catch (Exception e) {
                        tcs.TrySetException(e);
                    }
                });

                await Task.Delay(100, CancellationToken).ConfigureAwait(false);
                var msg = EventMessageBuilder.Create().WithUtcEventTime(now).WithMessage(TestContext.TestName).Build();
                await feature.ValueReceived(msg);

                CancelAfter(TimeSpan.FromSeconds(1));

                var emitted = await tcs.Task;
                Assert.IsNotNull(emitted);
                Assert.AreEqual(msg.Message, emitted.Message);
            }
        }


        [TestMethod]
        public async Task EventSubscriptionShouldApplyConcurrencyLimit() {
            var now = DateTime.UtcNow;

            var options = new EventMessagePushOptions() {
                MaxSubscriptionCount = 1
            };

            using (var feature = new EventMessagePush(options, null, null)) {
                var tcs1 = new TaskCompletionSource<EventMessage>();
                var tcs2 = new TaskCompletionSource<EventMessage>();

                _ = Task.Run(async () => {
                    try {
                        tcs1.TrySetResult(await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateEventMessageSubscriptionRequest(),
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken));
                    }
                    catch (OperationCanceledException) {
                        tcs1.TrySetCanceled(CancellationToken);
                    }
                    catch (Exception e) {
                        tcs1.TrySetException(e);
                    }
                });

                _ = Task.Run(async () => {
                    try {
                        // Wait for a short while to ensure that this task runs after the first one
                        await Task.Delay(100, CancellationToken).ConfigureAwait(false);
                        tcs2.TrySetResult(await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateEventMessageSubscriptionRequest(),
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken));
                    }
                    catch (OperationCanceledException) {
                        tcs2.TrySetCanceled(CancellationToken);
                    }
                    catch (Exception e) {
                        tcs2.TrySetException(e);
                    }
                });

                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => tcs2.Task);
            }
        }


        [TestMethod]
        public async Task EventTopicSubscriptionManagerShouldNotifyWhenSubscriptionIsAdded() {
            var options = new EventMessagePushWithTopicsOptions();

            using (var feature = new EventMessagePushWithTopics(options, null, null)) {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                feature.SubscriptionAdded += sub => tcs.TrySetResult(true);

                _ = Task.Run(async () => {
                    try {
                        await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateEventMessageTopicSubscriptionRequest(),
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken);
                    }
                    catch { }
                });

                CancelAfter(TimeSpan.FromSeconds(1));
                var success = await tcs.Task.WithCancellation(CancellationToken);
                Assert.IsTrue(success);
            }
        }


        [TestMethod]
        public async Task EventTopicSubscriptionManagerShouldNotifyWhenSubscriptionIsCancelled() {
            var options = new EventMessagePushWithTopicsOptions();

            using (var feature = new EventMessagePushWithTopics(options, null, null)) {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                feature.SubscriptionCancelled += sub => tcs.TrySetResult(true);

                _ = Task.Run(async () => {
                    try {
                        await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateEventMessageTopicSubscriptionRequest(),
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken);
                    }
                    catch { }
                });

                await Task.Delay(100, CancellationToken).ConfigureAwait(false);
                Cancel();
                var success = await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                Assert.IsTrue(success);
            }
        }


        [TestMethod]
        public async Task EventTopicSubscriptionShouldEmitValues() {
            var now = DateTime.UtcNow;
            var topic = Guid.NewGuid().ToString();

            var options = new EventMessagePushWithTopicsOptions();

            using (var feature = new EventMessagePushWithTopics(options, null, null)) {
                var tcs = new TaskCompletionSource<EventMessage>();

                _ = Task.Run(async () => {
                    try {
                        tcs.TrySetResult(await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateEventMessageTopicSubscriptionRequest() { 
                                Topics = new[] { topic }
                            },
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken));
                    }
                    catch (OperationCanceledException) {
                        tcs.TrySetCanceled(CancellationToken);
                    }
                    catch (Exception e) {
                        tcs.TrySetException(e);
                    }
                });

                await Task.Delay(100, CancellationToken).ConfigureAwait(false);

                var msg = EventMessageBuilder
                    .Create()
                    .WithTopic(topic)
                    .WithUtcEventTime(now)
                    .WithMessage(TestContext.TestName)
                    .Build();

                await feature.ValueReceived(msg, CancellationToken);

                CancelAfter(TimeSpan.FromSeconds(1));

                var emitted = await tcs.Task;
                Assert.IsNotNull(emitted);
                Assert.AreEqual(msg.Message, emitted.Message);
            }
        }


        [TestMethod]
        public async Task EventTopicSubscriptionShouldEmitValuesAfterSubscriptionChange() {
            var now = DateTime.UtcNow;
            var topic = Guid.NewGuid().ToString();

            var options = new EventMessagePushWithTopicsOptions();

            using (var feature = new EventMessagePushWithTopics(options, null, null)) {
                var channel = Channel.CreateUnbounded<EventMessageSubscriptionUpdate>();

                var msg1 = EventMessageBuilder
                    .Create()
                    .WithTopic(topic)
                    .WithUtcEventTime(now)
                    .WithMessage(TestContext.TestName)
                    .Build();

                var msg2 = EventMessageBuilder
                    .Create()
                    .WithTopic(topic)
                    .WithUtcEventTime(now.AddSeconds(1))
                    .WithMessage(TestContext.TestName)
                    .Build();

                _ = Task.Run(async () => { 
                    try {
                        await Task.Delay(100, CancellationToken);
                        // msg1 should not be received by the subscription.
                        await feature.ValueReceived(msg1, CancellationToken);

                        await Task.Delay(100, CancellationToken);

                        await channel.Writer.WriteAsync(new EventMessageSubscriptionUpdate() {
                            Topics = new[] { topic }
                        }, CancellationToken);

                        await Task.Delay(100, CancellationToken);

                        // msg2 should be received by the subscription.
                        await feature.ValueReceived(msg2, CancellationToken);
                    }
                    catch { }
                }, CancellationToken);

                CancelAfter(TimeSpan.FromSeconds(1));

                var emitted = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateEventMessageTopicSubscriptionRequest(),
                    channel.Reader.ReadAllAsync(CancellationToken),
                    CancellationToken
                ).FirstOrDefaultAsync(CancellationToken).ConfigureAwait(false);

                Assert.IsNotNull(emitted);
                Assert.AreEqual(msg2.UtcEventTime, emitted.UtcEventTime);
                Assert.AreEqual(msg2.Message, emitted.Message);
            }
        }


        [TestMethod]
        public async Task EventTopicSubscriptionShouldNotEmitValuesAfterSubscriptionChange() {
            var now = DateTime.UtcNow;
            var topic = Guid.NewGuid().ToString();

            var options = new EventMessagePushWithTopicsOptions();

            using (var feature = new EventMessagePushWithTopics(options, null, null)) {
                var channel = Channel.CreateUnbounded<EventMessageSubscriptionUpdate>();

                var msg1 = EventMessageBuilder
                    .Create()
                    .WithTopic(topic)
                    .WithUtcEventTime(now)
                    .WithMessage(TestContext.TestName)
                    .Build();

                var msg2 = EventMessageBuilder
                    .Create()
                    .WithTopic(topic)
                    .WithUtcEventTime(now.AddSeconds(1))
                    .WithMessage(TestContext.TestName)
                    .Build();

                _ = Task.Run(async () => {
                    try {
                        await Task.Delay(100, CancellationToken);
                        // msg1 should be received by the subscription.
                        await feature.ValueReceived(msg1, CancellationToken);

                        channel.Writer.TryWrite(new EventMessageSubscriptionUpdate() {
                            Topics = new[] { topic },
                            Action = Common.SubscriptionUpdateAction.Unsubscribe
                        });

                        await Task.Delay(100, CancellationToken);

                        // msg2 should not be received by the subscription.
                        await feature.ValueReceived(msg2, CancellationToken);
                    }
                    catch { }
                }, CancellationToken);

                var emittedCount = 0;

                CancelAfter(TimeSpan.FromSeconds(1));

                await using (var enumerator = feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateEventMessageTopicSubscriptionRequest() {
                        Topics = new[] { topic }
                    },
                    channel.Reader.ReadAllAsync(CancellationToken),
                    CancellationToken
                ).GetAsyncEnumerator(CancellationToken)) {
                    var emitted = await enumerator.MoveNextAsync().ConfigureAwait(false)
                        ? enumerator.Current
                        : null;

                    Assert.IsNotNull(emitted);
                    ++emittedCount;
                    Assert.AreEqual(msg1.UtcEventTime, emitted.UtcEventTime);
                    Assert.AreEqual(msg1.Message, emitted.Message);
                }

                Assert.AreEqual(1, emittedCount, "Only one value should have been emitted.");
            }
        }


        [TestMethod]
        public async Task EventTopicWildcardSubscriptionShouldReceiveMessages() {
            var now = DateTime.UtcNow;
            var topicRoot = Guid.NewGuid().ToString();

            var options = new EventMessagePushWithTopicsOptions() {
                IsTopicMatch = (subscribed, received, ct) => {
                    // If we subscribe to "topic_root", we should receive messages with a topic of 
                    // e.g. "topic_root/sub_topic".
                    return new ValueTask<bool>(received != null && received.StartsWith(subscribed));
                }
            };

            using (var feature = new EventMessagePushWithTopics(options, null, null)) {
                // We should receive this message due to the IsTopicMatch delegate.
                var msg1 = EventMessageBuilder
                    .Create()
                    .WithTopic(topicRoot + "/" + Guid.NewGuid().ToString())
                    .WithUtcEventTime(now)
                    .WithMessage(TestContext.TestName + "_1")
                    .Build();

                // We should not receive this message.
                var msg2 = EventMessageBuilder
                    .Create()
                    .WithTopic(null)
                    .WithUtcEventTime(now)
                    .WithMessage(TestContext.TestName + "_2")
                    .Build();

                // We should receive this message since the topic exactly matches our subscription.
                var msg3 = EventMessageBuilder
                    .Create()
                    .WithTopic(topicRoot)
                    .WithUtcEventTime(now)
                    .WithMessage(TestContext.TestName + "_3")
                    .Build();

                _ = Task.Run(async () => {
                    try {
                        await Task.Delay(100, CancellationToken);
                        await feature.ValueReceived(msg1, CancellationToken);
                        await feature.ValueReceived(msg2, CancellationToken);
                        await feature.ValueReceived(msg3, CancellationToken);
                    }
                    catch { }
                }, CancellationToken);

                var messagesReceived = 0;

                CancelAfter(TimeSpan.FromSeconds(1));

                await using (var enumerator = feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateEventMessageTopicSubscriptionRequest() {
                        Topics = new[] { topicRoot }
                    },
                    CancellationToken
                ).GetAsyncEnumerator(CancellationToken)) {
                    var emitted = await enumerator.MoveNextAsync().ConfigureAwait(false)
                        ? enumerator.Current
                        : null;

                    ++messagesReceived;
                    Assert.IsNotNull(emitted);
                    Assert.AreEqual(msg1.UtcEventTime, emitted.UtcEventTime);
                    Assert.AreEqual(msg1.Message, emitted.Message);

                    emitted = await enumerator.MoveNextAsync().ConfigureAwait(false)
                        ? enumerator.Current
                        : null;

                    ++messagesReceived;
                    Assert.IsNotNull(emitted);
                    Assert.AreEqual(msg3.UtcEventTime, emitted.UtcEventTime);
                    Assert.AreEqual(msg3.Message, emitted.Message);
                }

                Assert.AreEqual(2, messagesReceived, "2 messages should have been received.");
            }
        }


        [TestMethod]
        public async Task EventTopicSubscriptionShouldNotReceiveMessagesFromOtherTopics() {
            var now = DateTime.UtcNow;
            var topic = Guid.NewGuid().ToString();

            var options = new EventMessagePushWithTopicsOptions();

            using (var feature = new EventMessagePushWithTopics(options, null, null)) {
                var msg1 = EventMessageBuilder
                    .Create()
                    .WithTopic(topic)
                    .WithUtcEventTime(now)
                    .WithMessage(TestContext.TestName)
                    .Build();

                var msg2 = EventMessageBuilder
                    .Create()
                    .WithTopic(null)
                    .WithUtcEventTime(now)
                    .WithMessage(TestContext.TestName)
                    .Build();

                _ = Task.Run(async () => {
                    try {
                        await Task.Delay(100, CancellationToken);
                        await feature.ValueReceived(msg1, CancellationToken);
                        await feature.ValueReceived(msg2, CancellationToken);
                    }
                    catch { }
                }, CancellationToken);

                var messagesReceived = 0;

                CancelAfter(TimeSpan.FromSeconds(1));

                try {
                    await foreach (var emitted in feature.Subscribe(
                        ExampleCallContext.ForPrincipal(null),
                        new CreateEventMessageTopicSubscriptionRequest() {
                            Topics = new[] { topic }
                        },
                        CancellationToken
                    ).ConfigureAwait(false)) {
                        ++messagesReceived;
                        if (messagesReceived > 1) {
                            Assert.Fail("Only one value should have been emitted.");
                        }

                        Assert.IsNotNull(emitted);
                        Assert.AreEqual(msg1.UtcEventTime, emitted.UtcEventTime);
                        Assert.AreEqual(msg1.Message, emitted.Message);
                    }
                }
                catch (OperationCanceledException) { }

                Assert.AreEqual(1, messagesReceived, "One value should have been emitted.");
            }
        }


        [TestMethod]
        public async Task EventTopicSubscriptionShouldApplyConcurrencyLimit() {
            var now = DateTime.UtcNow;
            var topic = Guid.NewGuid().ToString();

            var options = new EventMessagePushWithTopicsOptions() {
                MaxSubscriptionCount = 1
            };

            using (var feature = new EventMessagePushWithTopics(options, null, null)) {
                var tcs1 = new TaskCompletionSource<EventMessage>();
                var tcs2 = new TaskCompletionSource<EventMessage>();

                _ = Task.Run(async () => {
                    try {
                        tcs1.TrySetResult(await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateEventMessageTopicSubscriptionRequest(),
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken));
                    }
                    catch (OperationCanceledException) {
                        tcs1.TrySetCanceled(CancellationToken);
                    }
                    catch (Exception e) {
                        tcs1.TrySetException(e);
                    }
                });

                _ = Task.Run(async () => {
                    try {
                        // Wait for a short while to ensure that this task runs after the first one
                        await Task.Delay(100, CancellationToken).ConfigureAwait(false);
                        tcs2.TrySetResult(await feature.Subscribe(
                            ExampleCallContext.ForPrincipal(null),
                            new CreateEventMessageTopicSubscriptionRequest(),
                            CancellationToken
                        ).FirstOrDefaultAsync(CancellationToken));
                    }
                    catch (OperationCanceledException) {
                        tcs2.TrySetCanceled(CancellationToken);
                    }
                    catch (Exception e) {
                        tcs2.TrySetException(e);
                    }
                });

                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => tcs2.Task);
            }
        }

    }
}
