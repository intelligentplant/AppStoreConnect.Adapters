using System;
using System.Collections.Generic;
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
                TagResolver = (ctx, name, ct) => new ValueTask<TagIdentifier>(new TagIdentifier(name, name))
            };

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                var subscription = await feature.Subscribe(ExampleCallContext.ForPrincipal(null), new CreateSnapshotTagValueSubscriptionRequest() {
                    Tag = TestContext.TestName
                }, CancellationToken);

                var val = TagValueBuilder.Create().WithUtcSampleTime(now).WithValue(now.Ticks).Build();
                await feature.ValueReceived(TagValueQueryResult.Create(TestContext.TestName, TestContext.TestName, val));

                CancelAfter(TimeSpan.FromSeconds(1));

                var emitted = await subscription.ReadAsync(CancellationToken);
                Assert.IsNotNull(emitted);
                Assert.AreEqual(TestContext.TestName, emitted.TagId);
                Assert.AreEqual(TestContext.TestName, emitted.TagName);
                Assert.AreEqual(now, emitted.Value.UtcSampleTime);
                Assert.AreEqual(now.Ticks, emitted.Value.GetValueOrDefault<long>());
            }
        }


        [TestMethod]
        public async Task SnapshotSubscriptionShouldRespectPublishInterval() {
            var generationInterval = TimeSpan.FromMilliseconds(50);
            var publishInterval = TimeSpan.FromSeconds(1);

            var options = new SnapshotTagValuePushOptions() { 
                AdapterId = TestContext.TestName,
                TagResolver = (ctx, name, ct) => new ValueTask<TagIdentifier>(new TagIdentifier(name, name))
            };

            var valueCount = 0;

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                var subscription = await feature.Subscribe(
                    ExampleCallContext.ForPrincipal(null), 
                        new CreateSnapshotTagValueSubscriptionRequest() { 
                        PublishInterval = TimeSpan.FromSeconds(1),
                        Tag = TestContext.TestName
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
        public async Task EventSubscriptionShouldEmitValues() {
            var now = DateTime.UtcNow;

            var options = new EventMessagePushOptions() {
                AdapterId = TestContext.TestName
            };

            using (var feature = new EventMessagePush(options, null, null)) {
                var subscription = await feature.Subscribe(ExampleCallContext.ForPrincipal(null), new CreateEventMessageSubscriptionRequest(), CancellationToken);
                var msg = EventMessageBuilder.Create().WithUtcEventTime(now).WithMessage(TestContext.TestName).Build();
                await feature.ValueReceived(msg);

                CancelAfter(TimeSpan.FromSeconds(1));

                var emitted = await subscription.ReadAsync(CancellationToken);
                Assert.IsNotNull(emitted);
                Assert.AreEqual(msg.Message, emitted.Message);
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
                        Topic = topic
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
                        Topic = topic
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

    }
}
