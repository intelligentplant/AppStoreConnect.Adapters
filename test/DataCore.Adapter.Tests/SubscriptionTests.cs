using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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
                using (var subscription = await feature.Subscribe(ExampleCallContext.ForPrincipal(null), new CreateSnapshotTagValueSubscriptionRequest())) {
                    await subscription.AddTagToSubscription(TestContext.TestName);

                    var val = TagValueBuilder.Create().WithUtcSampleTime(now).WithValue(now.Ticks).Build();
                    await feature.ValueReceived(TagValueQueryResult.Create(TestContext.TestName, TestContext.TestName, val));

                    using (var ctSource = new CancellationTokenSource(TimeSpan.FromSeconds(1))) {
                        var emitted = await subscription.Reader.ReadAsync(ctSource.Token);
                        Assert.IsNotNull(emitted);
                        Assert.AreEqual(TestContext.TestName, emitted.TagId);
                        Assert.AreEqual(TestContext.TestName, emitted.TagName);
                        Assert.AreEqual(now, emitted.Value.UtcSampleTime);
                        Assert.AreEqual(now.Ticks, emitted.Value.GetValueOrDefault<long>());
                    }
                }
            }
        }


        [TestMethod]
        public async Task SnapshotSubscriptionShouldRespectPublishInterval() {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            
            var generationInterval = TimeSpan.FromMilliseconds(50);
            var publishInterval = TimeSpan.FromSeconds(1);

            var options = new SnapshotTagValuePushOptions() { 
                AdapterId = TestContext.TestName,
                TagResolver = (ctx, name, ct) => new ValueTask<TagIdentifier>(new TagIdentifier(name, name))
            };

            var valueCount = 0;

            using (var feature = new SnapshotTagValuePush(options, null, null)) {
                using (var subscription = await feature.Subscribe(ExampleCallContext.ForPrincipal(null), new CreateSnapshotTagValueSubscriptionRequest() { PublishInterval = TimeSpan.FromSeconds(1) })) {
                    await subscription.AddTagToSubscription(TestContext.TestName);

                    _ = Task.Run(async () => {
                        try {
                            while (!cancellationToken.IsCancellationRequested) {
                                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                                var val = TagValueBuilder.Create().WithValue(DateTime.UtcNow.Ticks).Build();
                                await feature.ValueReceived(TagValueQueryResult.Create(TestContext.TestName, TestContext.TestName, val));
                            }
                        }
                        catch (OperationCanceledException) { }
                    }, cancellationToken);

                    cancellationTokenSource.CancelAfter(publishInterval);
                    try {
                        while (await subscription.Reader.WaitToReadAsync(cancellationToken)) {
                            if (subscription.Reader.TryRead(out var val)) {
                                ++valueCount;
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                }
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
                var subscription = await feature.Subscribe(ExampleCallContext.ForPrincipal(null), new CreateEventMessageSubscriptionRequest(), default);
                var msg = EventMessageBuilder.Create().WithUtcEventTime(now).WithMessage(TestContext.TestName).Build();
                await feature.ValueReceived(msg);

                using (var ctSource = new CancellationTokenSource(TimeSpan.FromSeconds(1))) {
                    var emitted = await subscription.ReadAsync(ctSource.Token);
                    Assert.IsNotNull(emitted);
                    Assert.AreEqual(msg.Message, emitted.Message);
                }
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
                using (var subscription = await feature.Subscribe(ExampleCallContext.ForPrincipal(null), new CreateEventMessageSubscriptionRequest())) {
                    await subscription.SubscribeToTopic(topic);

                    var msg = EventMessageBuilder
                        .Create()
                        .WithTopic(topic)
                        .WithUtcEventTime(now)
                        .WithMessage(TestContext.TestName)
                        .Build();

                    await feature.ValueReceived(msg);

                    using (var ctSource = new CancellationTokenSource(TimeSpan.FromSeconds(1))) {
                        var emitted = await subscription.Reader.ReadAsync(ctSource.Token);
                        Assert.IsNotNull(emitted);
                        Assert.AreEqual(msg.Message, emitted.Message);
                    }
                }
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
                using (var subscription = await feature.Subscribe(ExampleCallContext.ForPrincipal(null), new CreateEventMessageSubscriptionRequest())) {
                    await subscription.SubscribeToTopic(topic);

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

                    using (var ctSource = new CancellationTokenSource(TimeSpan.FromSeconds(1))) {
                        var emitted = await subscription.Reader.ReadAsync(ctSource.Token);
                        Assert.IsNotNull(emitted);
                        Assert.AreEqual(msg1.Message, emitted.Message);
                        ++messagesReceived;

                        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => {
                            emitted = await subscription.Reader.ReadAsync(ctSource.Token);
                            // Exception should be thrown before we get to here!
                            ++messagesReceived;
                        });
                    }
  
                    Assert.AreEqual(1, messagesReceived);
                }
            }
        }

    }
}
