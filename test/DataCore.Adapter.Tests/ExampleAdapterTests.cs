using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Events;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class ExampleAdapterTests : AdapterTestsBase<ExampleAdapter> {

        #region [ AdapterTestsBase<TAdapter> Overrides ]

        protected override IServiceScope CreateServiceScope(TestContext context) {
            return AssemblyInitializer.ApplicationServices.CreateScope();
        }


        protected override ExampleAdapter CreateAdapter(TestContext context, IServiceProvider serviceProvider) {
            return new ExampleAdapter();
        }


        protected override GetTagPropertiesRequest CreateGetTagPropertiesRequest(TestContext context) {
            return new GetTagPropertiesRequest();
        }


        protected override GetTagsRequest CreateGetTagsRequest(TestContext context) {
            return new GetTagsRequest() {
                Tags = new[] { context.TestName }
            };
        }


        protected override ReadSnapshotTagValuesRequest CreateReadSnapshotTagValuesRequest(TestContext context) {
            return new ReadSnapshotTagValuesRequest() { 
                Tags = new[] { context.TestName }
            };
        }


        protected override CreateSnapshotTagValueSubscriptionRequest CreateSnapshotTagValueSubscriptionRequest(TestContext context) {
            return new CreateSnapshotTagValueSubscriptionRequest() {
                Tags = new[] { context.TestName }
            };
        }


        protected override Task<bool> EmitTestSnapshotValue(TestContext context, ExampleAdapter adapter, IEnumerable<string> tags, CancellationToken cancellationToken) {
            return Task.FromResult(true);
        }


        protected override async Task<bool> EmitTestEvent(TestContext context, ExampleAdapter adapter, CancellationToken cancellationToken) {
            await adapter.WriteTestEventMessage(
                EventMessageBuilder
                    .Create()
                    .WithTopic(context.TestName)
                    .WithUtcEventTime(DateTime.UtcNow)
                    .WithCategory(TestContext.FullyQualifiedTestClassName)
                    .WithMessage(TestContext.TestName)
                    .WithPriority(EventPriority.Low)
                    .Build()
            );
            return true;
        }


        protected override CreateEventMessageSubscriptionRequest CreateEventMessageSubscriptionRequest(TestContext context) {
            return new CreateEventMessageSubscriptionRequest() { 
                SubscriptionType = EventMessageSubscriptionType.Active
            };
        }


        protected override CreateEventMessageTopicSubscriptionRequest CreateEventMessageTopicSubscriptionRequest(TestContext context) {
            return new CreateEventMessageTopicSubscriptionRequest() { 
                SubscriptionType = EventMessageSubscriptionType.Active,
                Topics = new[] { context.TestName }
            };
        }

        #endregion

        #region [ Additional Tests ]

        [TestMethod]
        public Task UnsupportedFeatureShouldNotBeFound() {
            return RunAdapterTest((adapter, context, ct) => {
                var feature = adapter.Features.Get<IFakeAdapterFeature>();
                Assert.IsNull(feature);
                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task SupportedFeatureShouldBeFound() {
            return RunAdapterTest((adapter, context, ct) => {
                var feature = adapter.Features.Get<IReadSnapshotTagValues>();
                Assert.IsNotNull(feature);
                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task SnapshotSubscriptionShouldReceiveAdditionalValues() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<ISnapshotTagValuePush>();

                var now = DateTime.UtcNow;

                _ = Task.Run(async () => { 
                    try {
                        await Task.Delay(100, ct).ConfigureAwait(false);

                        // Write a couple of values that we should then be able to read out again via 
                        // the subscription's channel.
                        
                        await adapter.WriteSnapshotValue(
                            TagValueQueryResult.Create(
                                TestContext.TestName,
                                TestContext.TestName,
                                new TagValueBuilder()
                                    .WithUtcSampleTime(now.AddSeconds(-5))
                                    .WithValue(100)
                                    .Build()
                            )
                        );
                        await adapter.WriteSnapshotValue(
                            TagValueQueryResult.Create(
                                TestContext.TestName,
                                TestContext.TestName,
                                new TagValueBuilder()
                                    .WithUtcSampleTime(now.AddSeconds(-1))
                                    .WithValue(99)
                                    .Build()
                            )
                        );
                    }
                    catch (OperationCanceledException) { }
                }, ct);

                using (var ctSource = new CancellationTokenSource(1000)) {
                    await using (var enumerator = feature.Subscribe(
                        ExampleCallContext.ForPrincipal(null),
                        new CreateSnapshotTagValueSubscriptionRequest() {
                            Tags = new[] { TestContext.TestName }
                        },
                        ct
                    ).GetAsyncEnumerator(ct)) {
                        // Read initial value.
                        Assert.IsTrue(await enumerator.MoveNextAsync().ConfigureAwait(false));
                        var value = enumerator.Current;
                        Assert.IsNotNull(value);

                        // Read first value written above.
                        Assert.IsTrue(await enumerator.MoveNextAsync().ConfigureAwait(false));
                        value = enumerator.Current;
                        Assert.IsNotNull(value);
                        Assert.AreEqual(now.AddSeconds(-5), value.Value.UtcSampleTime);
                        Assert.AreEqual(100, value.Value.GetValueOrDefault<int>());

                        // Read second value written above.
                        Assert.IsTrue(await enumerator.MoveNextAsync().ConfigureAwait(false));
                        value = enumerator.Current;
                        Assert.IsNotNull(value);
                        Assert.AreEqual(now.AddSeconds(-1), value.Value.UtcSampleTime);
                        Assert.AreEqual(99, value.Value.GetValueOrDefault<int>());
                    }
                }
            });
        }

        #endregion

    }
}
