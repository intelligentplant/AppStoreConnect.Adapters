using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;
using DataCore.Adapter.RealTimeData;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class SubscriptionTests : TestsBase {

        [TestMethod]
        public async Task SnapshotSubscriptionShouldEmitValues() {
            var now = DateTime.UtcNow;

            var options = new SnapshotTagValuePushOptions() {
                AdapterId = TestContext.TestName,
                TagResolver = (ctx, names, ct) => new ValueTask<IEnumerable<TagIdentifier>>(names.Select(name => new TagIdentifier(name, name)).ToArray())
            };

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                var subscription = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null), 
                    new CreateSnapshotTagValueSubscriptionRequest() {
                        Tags = new[] { TestContext.TestName }
                    }, 
                    CancellationToken
                );

                var val = TagValueBuilder.Create().WithUtcSampleTime(now).WithValue(now.Ticks).Build();
                await feature.ValueReceived(TagValueQueryResult.Create(TestContext.TestName, TestContext.TestName, val));

                CancelAfter(TimeSpan.FromSeconds(1));

                var emitted = await subscription.ReadAsync(CancellationToken);
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
                AdapterId = TestContext.TestName,
                TagResolver = (ctx, names, ct) => new ValueTask<IEnumerable<TagIdentifier>>(names.Select(name => new TagIdentifier(name, name)).ToArray())
            };

            var channel = Channel.CreateUnbounded<TagValueSubscriptionUpdate>();

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                var subscription = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateSnapshotTagValueSubscriptionRequest(),
                    channel,
                    CancellationToken
                );

                var val1 = TagValueBuilder.Create().WithUtcSampleTime(now).WithValue(now.Ticks).Build();
                await feature.ValueReceived(TagValueQueryResult.Create(TestContext.TestName, TestContext.TestName, val1));

                channel.Writer.TryWrite(new TagValueSubscriptionUpdate() { 
                    Action = Common.SubscriptionUpdateAction.Subscribe,
                    Tags = new [] { TestContext.TestName }
                });

                var val2 = TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(1)).WithValue(now.Ticks + TimeSpan.TicksPerSecond).Build();
                await feature.ValueReceived(TagValueQueryResult.Create(TestContext.TestName, TestContext.TestName, val2));

                CancelAfter(TimeSpan.FromSeconds(1));

                try {
                    var count = 0;
                    while (await subscription.WaitToReadAsync(CancellationToken)) {
                        while (subscription.TryRead(out var emitted)) {
                            ++count;
                            if (count > 1) {
                                Assert.Fail("Only one value should be received.");
                            }
                            Assert.IsNotNull(emitted);
                            Assert.AreEqual(TestContext.TestName, emitted.TagId);
                            Assert.AreEqual(TestContext.TestName, emitted.TagName);
                            Assert.AreEqual(val2.UtcSampleTime, emitted.Value.UtcSampleTime);
                            Assert.AreEqual(val2.UtcSampleTime.Ticks, emitted.Value.GetValueOrDefault<long>());
                        }
                    }
                }
                catch (OperationCanceledException) { }
            }
        }


        [TestMethod]
        public async Task SnapshotSubscriptionShouldNotEmitValuesAfterSubscriptionChange() {
            var now = DateTime.UtcNow;

            var options = new SnapshotTagValuePushOptions() {
                AdapterId = TestContext.TestName,
                TagResolver = (ctx, names, ct) => new ValueTask<IEnumerable<TagIdentifier>>(names.Select(name => new TagIdentifier(name, name)).ToArray())
            };

            var channel = Channel.CreateUnbounded<TagValueSubscriptionUpdate>();

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                var subscription = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateSnapshotTagValueSubscriptionRequest() { 
                        Tags = new[] { TestContext.TestName }
                    },
                    channel,
                    CancellationToken
                );

                var val1 = TagValueBuilder.Create().WithUtcSampleTime(now).WithValue(now.Ticks).Build();
                await feature.ValueReceived(TagValueQueryResult.Create(TestContext.TestName, TestContext.TestName, val1));

                channel.Writer.TryWrite(new TagValueSubscriptionUpdate() {
                    Action = Common.SubscriptionUpdateAction.Unsubscribe,
                    Tags = new[] { TestContext.TestName }
                });

                await Task.Delay(1000, CancellationToken);

                var val2 = TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(1)).WithValue(now.Ticks + TimeSpan.TicksPerSecond).Build();
                await feature.ValueReceived(TagValueQueryResult.Create(TestContext.TestName, TestContext.TestName, val2));

                CancelAfter(TimeSpan.FromSeconds(1));

                try {
                    var count = 0;
                    while (await subscription.WaitToReadAsync(CancellationToken)) {
                        while (subscription.TryRead(out var emitted)) {
                            ++count;
                            if (count > 1) {
                                Assert.Fail("Only one value should be received.");
                            }
                            Assert.IsNotNull(emitted);
                            Assert.AreEqual(TestContext.TestName, emitted.TagId);
                            Assert.AreEqual(TestContext.TestName, emitted.TagName);
                            Assert.AreEqual(val1.UtcSampleTime, emitted.Value.UtcSampleTime);
                            Assert.AreEqual(val1.UtcSampleTime.Ticks, emitted.Value.GetValueOrDefault<long>());
                        }
                    }
                }
                catch (OperationCanceledException) { }
            }
        }


        [TestMethod]
        public async Task SnapshotSubscriptionShouldRespectPublishInterval() {
            var generationInterval = TimeSpan.FromMilliseconds(50);
            var publishInterval = TimeSpan.FromSeconds(1);

            var options = new SnapshotTagValuePushOptions() { 
                AdapterId = TestContext.TestName,
                TagResolver = (ctx, names, ct) => new ValueTask<IEnumerable<TagIdentifier>>(names.Select(name => new TagIdentifier(name, name)).ToArray())
            };

            var valueCount = 0;

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                var subscription = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null), 
                        new CreateSnapshotTagValueSubscriptionRequest() { 
                        PublishInterval = TimeSpan.FromSeconds(1),
                        Tags = new[] { TestContext.TestName }
                    }, 
                    CancellationToken
                );

                _ = Task.Run(async () => {
                    try {
                        while (!CancellationToken.IsCancellationRequested) {
                            await Task.Delay(50, CancellationToken).ConfigureAwait(false);
                            var val = TagValueBuilder.Create().WithValue(DateTime.UtcNow.Ticks).Build();
                            await feature.ValueReceived(TagValueQueryResult.Create(TestContext.TestName, TestContext.TestName, val));
                        }
                    }
                    catch (OperationCanceledException) { }
                }, CancellationToken);

                CancelAfter(publishInterval);
                try {
                    while (await subscription.WaitToReadAsync(CancellationToken)) {
                        if (subscription.TryRead(out var val)) {
                            ++valueCount;
                        }
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
                AdapterId = TestContext.TestName,
                MaxSubscriptionCount = 1,
                TagResolver = (ctx, names, ct) => new ValueTask<IEnumerable<TagIdentifier>>(names.Select(name => new TagIdentifier(name, name)).ToArray())
            };

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                var subscription = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null), 
                    new CreateSnapshotTagValueSubscriptionRequest() {
                        Tags = new[] { TestContext.TestName }
                    }, 
                    CancellationToken
                );

                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null), 
                    new CreateSnapshotTagValueSubscriptionRequest() {
                        Tags = new[] { TestContext.TestName }
                    }, 
                    CancellationToken
                ));
            }
        }


        [TestMethod]
        public async Task EventSubscriptionShouldEmitValues() {
            var now = DateTime.UtcNow;

            var options = new EventMessagePushOptions() {
                AdapterId = TestContext.TestName
            };

            using (var feature = new EventMessagePush(options, null, null)) {
                var subscription = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null), 
                    new CreateEventMessageSubscriptionRequest(), 
                    CancellationToken
                );
                var msg = EventMessageBuilder.Create().WithUtcEventTime(now).WithMessage(TestContext.TestName).Build();
                await feature.ValueReceived(msg);

                CancelAfter(TimeSpan.FromSeconds(1));

                var emitted = await subscription.ReadAsync(CancellationToken);
                Assert.IsNotNull(emitted);
                Assert.AreEqual(msg.Message, emitted.Message);
            }
        }


        [TestMethod]
        public async Task EventSubscriptionShouldApplyConcurrencyLimit() {
            var now = DateTime.UtcNow;

            var options = new EventMessagePushOptions() {
                AdapterId = TestContext.TestName,
                MaxSubscriptionCount = 1
            };

            using (var feature = new EventMessagePush(options, null, null)) {
                var subscription = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null), 
                    new CreateEventMessageSubscriptionRequest(), 
                    CancellationToken
                );

                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateEventMessageSubscriptionRequest(),
                    CancellationToken
                ));
            }
        }


        [TestMethod]
        public async Task EventTopicSubscriptionShouldEmitValues() {
            var now = DateTime.UtcNow;
            var topic = Guid.NewGuid().ToString();

            var options = new EventMessagePushWithTopicsOptions() {
                AdapterId = TestContext.TestName
            };

            using (var feature = new EventMessagePushWithTopics(options, null, null)) {
                var subscription = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateEventMessageTopicSubscriptionRequest() {
                        Topics = new[] { topic }
                    },
                    CancellationToken
                );

                var msg = EventMessageBuilder
                    .Create()
                    .WithTopic(topic)
                    .WithUtcEventTime(now)
                    .WithMessage(TestContext.TestName)
                    .Build();

                await feature.ValueReceived(msg, CancellationToken);

                CancelAfter(TimeSpan.FromSeconds(1));

                var emitted = await subscription.ReadAsync(CancellationToken);
                Assert.IsNotNull(emitted);
                Assert.AreEqual(msg.Message, emitted.Message);
            }
        }


        [TestMethod]
        public async Task EventTopicSubscriptionShouldEmitValuesAfterSubscriptionChange() {
            var now = DateTime.UtcNow;
            var topic = Guid.NewGuid().ToString();

            var options = new EventMessagePushWithTopicsOptions() {
                AdapterId = TestContext.TestName
            };

            using (var feature = new EventMessagePushWithTopics(options, null, null)) {
                var channel = Channel.CreateUnbounded<EventMessageSubscriptionUpdate>();

                var subscription = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateEventMessageTopicSubscriptionRequest(),
                    CancellationToken
                );

                var msg1 = EventMessageBuilder
                    .Create()
                    .WithTopic(topic)
                    .WithUtcEventTime(now)
                    .WithMessage(TestContext.TestName)
                    .Build();

                await feature.ValueReceived(msg1, CancellationToken);

                channel.Writer.TryWrite(new EventMessageSubscriptionUpdate() {
                    Action = Common.SubscriptionUpdateAction.Subscribe,
                    Topics = new[] { TestContext.TestName }
                });

                var msg2 = EventMessageBuilder
                    .Create()
                    .WithTopic(topic)
                    .WithUtcEventTime(now.AddSeconds(1))
                    .WithMessage(TestContext.TestName)
                    .Build();

                await feature.ValueReceived(msg2, CancellationToken);

                CancelAfter(TimeSpan.FromSeconds(1));

                try {
                    var count = 0;
                    while (await subscription.WaitToReadAsync(CancellationToken)) {
                        while (subscription.TryRead(out var emitted)) {
                            ++count;
                            if (count > 1) {
                                Assert.Fail("Only one value should be received.");
                            }

                            Assert.IsNotNull(emitted);
                            Assert.AreEqual(msg2.UtcEventTime, emitted.UtcEventTime);
                            Assert.AreEqual(msg2.Message, emitted.Message);
                        }
                    }
                }
                catch (OperationCanceledException) { }
            }
        }


        [TestMethod]
        public async Task EventTopicSubscriptionShouldNotEmitValuesAfterSubscriptionChange() {
            var now = DateTime.UtcNow;
            var topic = Guid.NewGuid().ToString();

            var options = new EventMessagePushWithTopicsOptions() {
                AdapterId = TestContext.TestName
            };

            using (var feature = new EventMessagePushWithTopics(options, null, null)) {
                var channel = Channel.CreateUnbounded<EventMessageSubscriptionUpdate>();

                var subscription = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateEventMessageTopicSubscriptionRequest() {
                        Topics = new[] { TestContext.TestName }
                    },
                    CancellationToken
                );

                var msg1 = EventMessageBuilder
                    .Create()
                    .WithTopic(topic)
                    .WithUtcEventTime(now)
                    .WithMessage(TestContext.TestName)
                    .Build();

                await feature.ValueReceived(msg1, CancellationToken);

                channel.Writer.TryWrite(new EventMessageSubscriptionUpdate() {
                    Action = Common.SubscriptionUpdateAction.Unsubscribe,
                    Topics = new[] { TestContext.TestName }
                });

                await Task.Delay(1000, CancellationToken);

                var msg2 = EventMessageBuilder
                    .Create()
                    .WithTopic(topic)
                    .WithUtcEventTime(now.AddSeconds(1))
                    .WithMessage(TestContext.TestName)
                    .Build();

                await feature.ValueReceived(msg2, CancellationToken);

                CancelAfter(TimeSpan.FromSeconds(1));

                try {
                    var count = 0;
                    while (await subscription.WaitToReadAsync(CancellationToken)) {
                        while (subscription.TryRead(out var emitted)) {
                            ++count;
                            if (count > 1) {
                                Assert.Fail("Only one value should be received.");
                            }

                            Assert.IsNotNull(emitted);
                            Assert.AreEqual(msg1.UtcEventTime, emitted.UtcEventTime);
                            Assert.AreEqual(msg1.Message, emitted.Message);
                        }
                    }
                }
                catch (OperationCanceledException) { }
            }
        }


        [TestMethod]
        public async Task EventTopicSubscriptionShouldNotReceiveMessagesFromOtherTopics() {
            var now = DateTime.UtcNow;
            var topic = Guid.NewGuid().ToString();

            var options = new EventMessagePushWithTopicsOptions() {
                AdapterId = TestContext.TestName
            };

            using (var feature = new EventMessagePushWithTopics(options, null, null)) {
                var subscription = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateEventMessageTopicSubscriptionRequest() {
                        Topics = new[] { topic }
                    },
                    CancellationToken
                );

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

                await feature.ValueReceived(msg1);
                await feature.ValueReceived(msg2);

                var messagesReceived = 0;

                CancelAfter(TimeSpan.FromSeconds(1));

                var emitted = await subscription.ReadAsync(CancellationToken);
                Assert.IsNotNull(emitted);
                Assert.AreEqual(msg1.Message, emitted.Message);
                ++messagesReceived;

                try {
                    emitted = await subscription.ReadAsync(CancellationToken);
                    // Exception should be thrown before we get to here!
                    ++messagesReceived;
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }

                Assert.AreEqual(1, messagesReceived);
            }
        }


        [TestMethod]
        public async Task EventTopicSubscriptionShouldApplyConcurrencyLimit() {
            var now = DateTime.UtcNow;
            var topic = Guid.NewGuid().ToString();

            var options = new EventMessagePushWithTopicsOptions() {
                AdapterId = TestContext.TestName,
                MaxSubscriptionCount = 1
            };

            using (var feature = new EventMessagePushWithTopics(options, null, null)) {
                var subscription = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateEventMessageTopicSubscriptionRequest() {
                        Topics = new[] { topic }
                    },
                    CancellationToken
                );

                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null),
                    new CreateEventMessageTopicSubscriptionRequest() {
                        Topics = new[] { topic }
                    },
                    CancellationToken
                ));
            }
        }

    }
}
