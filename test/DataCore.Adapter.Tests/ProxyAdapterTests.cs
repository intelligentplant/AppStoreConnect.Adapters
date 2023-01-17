#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Events;
using DataCore.Adapter.Http.Client;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {
    public abstract class ProxyAdapterTests<TProxy> : AdapterTests<TProxy> where TProxy : class, IAdapterProxy {

        private static bool s_historicalTestEventsInitialized;

        private static DateTime s_historicalTestEventsStartTime;


        protected virtual IEnumerable<string> UnsupportedStandardFeatures => Array.Empty<string>();


        protected sealed override TProxy CreateAdapter(TestContext context, IServiceProvider serviceProvider) {
            return CreateProxy(context, AssemblyInitializer.AdapterId, serviceProvider);
        }


        protected override async Task BeforeAdapterTestAsync(TProxy adapter, IAdapterCallContext context, CancellationToken cancellationToken) {
            await base.BeforeAdapterTestAsync(adapter, context, cancellationToken).ConfigureAwait(false);

            switch (TestContext.TestName) {
                case nameof(UpdateTagValueAnnotationShouldSucceed):
                case nameof(DeleteTagValueAnnotationShouldSucceed):
                    var accessor = AssemblyInitializer.ApplicationServices.GetRequiredService<IAdapterAccessor>();
                    var remoteAdapter = await accessor.GetAdapter(context, AssemblyInitializer.AdapterId, cancellationToken).ConfigureAwait(false);
                    var annotationManager = remoteAdapter.GetFeature<IWriteTagValueAnnotations>().Unwrap() as InMemoryTagValueAnnotationManager;
                    if (annotationManager != null) {
                        await annotationManager.CreateOrUpdateAnnotationAsync(
                            AssemblyInitializer.TestTagId, 
                            new TagValueAnnotationBuilder()
                                .WithId(TestContext.TestName)
                                .WithValue(TestContext.TestName)
                                .Build(), 
                            cancellationToken
                        ).ConfigureAwait(false);
                    }
                    break;
            }
        }


        protected override ConfigurationChangesSubscriptionRequest CreateConfigurationChangesSubscriptionRequest(TestContext context) {
            return new ConfigurationChangesSubscriptionRequest() { 
                ItemTypes = new[] { ConfigurationChangeItemTypes.Tag }
            };
        }


        protected override async Task<bool> EmitTestConfigurationChanges(TestContext context, TProxy adapter, IEnumerable<string> itemTypes, ConfigurationChangeType changeType, CancellationToken cancellationToken) {
            var remoteAdapter = AssemblyInitializer.ApplicationServices.GetService<IAdapter>();
            if (remoteAdapter == null) {
                return false;
            }

            var feature = remoteAdapter.GetFeature<IConfigurationChanges>().Unwrap() as ConfigurationChanges;
            if (feature == null) {
                return false;
            }

            foreach (var itemType in itemTypes) {
                await feature.ValueReceived(new ConfigurationChange(itemType, context.TestName, context.TestName, changeType, null), cancellationToken).ConfigureAwait(false);
            }

            return true;
        }


        protected override BrowseAssetModelNodesRequest CreateBrowseAssetModelNodesRequest(TestContext context) {
            return new BrowseAssetModelNodesRequest();
        }


        protected override FindAssetModelNodesRequest CreateFindAssetModelNodesRequest(TestContext context) {
            return new FindAssetModelNodesRequest() { Name = "*child" };
        }


        protected override GetAssetModelNodesRequest CreateGetAssetModelNodesRequest(TestContext context) {
            return new GetAssetModelNodesRequest() {
                Nodes = new[] { "3" }
            };
        }


        protected override FindTagsRequest CreateFindTagsRequest(TestContext context) {
            return new FindTagsRequest() { 
                Name = "*"
            };
        }


        protected override GetTagsRequest CreateGetTagsRequest(TestContext context) {
            return new GetTagsRequest() {
                Tags = new[] { AssemblyInitializer.TestTagId }
            };
        }


        protected override GetTagPropertiesRequest CreateGetTagPropertiesRequest(TestContext context) {
            return new GetTagPropertiesRequest();
        }


        protected sealed override ReadSnapshotTagValuesRequest CreateReadSnapshotTagValuesRequest(TestContext context) {
            return new ReadSnapshotTagValuesRequest() { 
                Tags = new[] { AssemblyInitializer.TestTagId }
            };
        }


        protected override CreateSnapshotTagValueSubscriptionRequest CreateSnapshotTagValueSubscriptionRequest(TestContext context) {
            return new CreateSnapshotTagValueSubscriptionRequest() {
                Tags = new[] { AssemblyInitializer.TestTagId }
            };
        }


        protected override Task<bool> EmitTestSnapshotValue(TestContext context, TProxy adapter, IEnumerable<string> tags, CancellationToken cancellationToken) {
            return Task.FromResult(true);
        }


        protected override ReadRawTagValuesRequest CreateReadRawTagValuesRequest(TestContext context) {
            var now = DateTime.UtcNow;
            return new ReadRawTagValuesRequest() { 
                Tags = new[] { AssemblyInitializer.TestTagId },
                UtcStartTime = now.AddDays(-1),
                UtcEndTime = now
            };
        }


        protected override ReadPlotTagValuesRequest CreateReadPlotTagValuesRequest(TestContext context) {
            var now = DateTime.UtcNow;
            return new ReadPlotTagValuesRequest() {
                Tags = new[] { AssemblyInitializer.TestTagId },
                UtcStartTime = now.AddDays(-1),
                UtcEndTime = now,
                Intervals = 500
            };
        }


        protected override ReadProcessedTagValuesRequest CreateReadProcessedTagValuesRequest(TestContext context) {
            var now = DateTime.UtcNow;
            return new ReadProcessedTagValuesRequest() {
                Tags = new[] { AssemblyInitializer.TestTagId },
                UtcStartTime = now.AddDays(-1),
                UtcEndTime = now,
                SampleInterval = TimeSpan.FromHours(3),
                DataFunctions = new[] {
                    DefaultDataFunctions.Constants.FunctionIdAverage,
                    DefaultDataFunctions.Constants.FunctionIdCount,
                    DefaultDataFunctions.Constants.FunctionIdDelta,
                    DefaultDataFunctions.Constants.FunctionIdInterpolate,
                    DefaultDataFunctions.Constants.FunctionIdMaximum,
                    DefaultDataFunctions.Constants.FunctionIdMinimum,
                    DefaultDataFunctions.Constants.FunctionIdPercentBad,
                    DefaultDataFunctions.Constants.FunctionIdPercentGood,
                    DefaultDataFunctions.Constants.FunctionIdRange,
                    DefaultDataFunctions.Constants.FunctionIdStandardDeviation,
                    DefaultDataFunctions.Constants.FunctionIdVariance
                }
            };
        }


        protected override ReadTagValuesAtTimesRequest CreateReadTagValuesAtTimesRequest(TestContext context) {
            var now = DateTime.UtcNow;
            return new ReadTagValuesAtTimesRequest() { 
                Tags = new[] { AssemblyInitializer.TestTagId },
                UtcSampleTimes = new[] { 
                    now.AddHours(-17),
                    now.AddHours(-12.5),
                    now.AddHours(-11.223)
                }
            };
        }


        protected override IEnumerable<WriteTagValueItem> CreateWriteSnapshotTagValueItems(TestContext context) {
            var now = DateTime.UtcNow;
            var values = new List<WriteTagValueItem>();
            for (var i = 0; i < 5; i++) {
                values.Add(new WriteTagValueItem() {
                    CorrelationId = Guid.NewGuid().ToString(),
                    TagId = context.TestName,
                    Value = new TagValueBuilder().WithUtcSampleTime(now.AddDays(-1).AddMinutes(-1 * (5 - i))).WithValue(i).Build()
                });
            }
            return values;
        }


        protected override IEnumerable<WriteTagValueItem> CreateWriteHistoricalTagValueItems(TestContext context) {
            var now = DateTime.UtcNow;
            var values = new List<WriteTagValueItem>();
            for (var i = 0; i < 5; i++) {
                values.Add(new WriteTagValueItem() {
                    CorrelationId = Guid.NewGuid().ToString(),
                    TagId = context.TestName,
                    Value = new TagValueBuilder().WithUtcSampleTime(now.AddDays(-1).AddMinutes(-1 * (5 - i))).WithValue(i).Build()
                });
            }
            return values;
        }


        protected override ReadAnnotationsRequest CreateReadAnnotationsRequest(TestContext context) {
            var now = DateTime.UtcNow;

            return new ReadAnnotationsRequest() {
                Tags = new[] { AssemblyInitializer.TestTagId },
                UtcStartTime = now.AddDays(-1),
                UtcEndTime = now
            };
        }


        protected override ReadAnnotationRequest CreateReadAnnotationRequest(TestContext context) {
            return new ReadAnnotationRequest() { 
                Tag = AssemblyInitializer.TestTagId,
                AnnotationId = AssemblyInitializer.TestAnnotationId
            };
        }


        protected override CreateAnnotationRequest CreateCreateAnnotationRequest(TestContext context) {
            return new CreateAnnotationRequest() {
                Tag = AssemblyInitializer.TestTagId,
                Annotation = new TagValueAnnotationBuilder()
                    .WithId(context.TestName)
                    .WithValue(context.TestName)
                    .Build()
            };
        }


        protected override UpdateAnnotationRequest CreateUpdateAnnotationRequest(TestContext context) {
            return new UpdateAnnotationRequest() {
                Tag = AssemblyInitializer.TestTagId,
                AnnotationId = context.TestName,
                Annotation = new TagValueAnnotationBuilder()
                    .WithId(context.TestName)
                    .WithValue(context.TestName)
                    .Build()
            };
        }


        protected override DeleteAnnotationRequest CreateDeleteAnnotationRequest(TestContext context) {
            return new DeleteAnnotationRequest() {
                Tag = AssemblyInitializer.TestTagId,
                AnnotationId = context.TestName
            };
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


        protected override ReadEventMessagesForTimeRangeRequest CreateReadEventMessagesForTimeRangeRequest(TestContext context) {
            var now = DateTime.UtcNow;

            return new ReadEventMessagesForTimeRangeRequest() {
                UtcStartTime = now.AddMinutes(-10),
                UtcEndTime = now.AddMinutes(10)
            };
        }


        protected override ReadEventMessagesUsingCursorRequest CreateReadEventMessagesUsingCursorRequest(TestContext context) {
            return new ReadEventMessagesUsingCursorRequest();
        }


        protected override IEnumerable<WriteEventMessageItem> CreateWriteEventMessageItems(TestContext context) {
            var now = DateTime.UtcNow;
            var messages = Enumerable.Range(-200, 100).Select(x => EventMessageBuilder
                .Create()
                .WithUtcEventTime(now.AddMinutes(x))
                .WithCategory(context.FullyQualifiedTestClassName)
                .WithMessage($"Test message")
                .WithPriority(EventPriority.Low)
                .Build()
            ).ToArray();

            return messages.Select(x => new WriteEventMessageItem() {
                CorrelationId = Guid.NewGuid().ToString(),
                EventMessage = x
            }).ToArray();
        }


        public override async Task ReadEventMessagesForTimeRangeRequestShouldReturnResults() {
            await InitHistoricalEventMessages().ConfigureAwait(false);
            await base.ReadEventMessagesForTimeRangeRequestShouldReturnResults().ConfigureAwait(false);
        }


        public override async Task ReadEventMessagesUsingCursorRequestShouldReturnResults() {
            await InitHistoricalEventMessages().ConfigureAwait(false);
            await base.ReadEventMessagesUsingCursorRequestShouldReturnResults().ConfigureAwait(false);
        }


        private async Task InitHistoricalEventMessages() {
            if (!s_historicalTestEventsInitialized) {
                s_historicalTestEventsInitialized = true;

                var httpClient = ActivatorUtilities.CreateInstance<AdapterHttpClient>(
                    AssemblyInitializer.ApplicationServices,
                    AssemblyInitializer.ApplicationServices.GetRequiredService<IHttpClientFactory>().CreateClient(WebHostConfiguration.HttpClientName)
                );

                var now = DateTime.UtcNow;
                var messages = Enumerable.Range(-100, 100).Select(x => EventMessageBuilder
                    .Create()
                    .WithUtcEventTime(now.AddMinutes(x))
                    .WithCategory(TestContext.FullyQualifiedTestClassName)
                    .WithMessage($"Test message")
                    .WithPriority(EventPriority.Low)
                    .Build()
                ).ToArray();

                var writeResult = await httpClient.Events.WriteEventMessagesAsync(AssemblyInitializer.AdapterId, new WriteEventMessagesRequestExtended() {
                    Events = messages.Select(msg => new WriteEventMessageItem() {
                        CorrelationId = msg.Id,
                        EventMessage = msg
                    }).ToArray()
                }).ToArrayAsync().ConfigureAwait(false);

                Assert.IsNotNull(writeResult);
                Assert.AreEqual(messages.Length, writeResult.Count());

                s_historicalTestEventsStartTime = messages.First().UtcEventTime;
            }

        }


        protected override async Task<bool> EmitTestEvent(TestContext context, TProxy adapter, CancellationToken cancellationToken) {
            var httpClient = ActivatorUtilities.CreateInstance<AdapterHttpClient>(
                AssemblyInitializer.ApplicationServices,
                AssemblyInitializer.ApplicationServices.GetRequiredService<IHttpClientFactory>().CreateClient(WebHostConfiguration.HttpClientName)
            );
            
            var msg = EventMessageBuilder
                .Create()
                .WithTopic(context.TestName)
                .WithUtcEventTime(DateTime.UtcNow)
                .WithCategory(context.FullyQualifiedTestClassName)
                .WithMessage(context.TestName)
                .WithPriority(EventPriority.Low)
                .Build();

            var correlationId = Guid.NewGuid().ToString();

            var writeResult = await httpClient.Events.WriteEventMessagesAsync(AssemblyInitializer.AdapterId, new WriteEventMessagesRequestExtended() { 
                Events = new [] { 
                    new WriteEventMessageItem() {
                        EventMessage = msg,
                        CorrelationId = correlationId
                    }
                }
            }).ToArrayAsync().ConfigureAwait(false);

            return true;
        }


        protected abstract TProxy CreateProxy(TestContext context, string remoteAdapterId, IServiceProvider serviceProvider);


        [TestMethod]
        public Task ProxyShouldReceiveLargeRawDataSet() {
            return RunAdapterTest(async (proxy, context, ct) => {
                var feature = proxy.GetFeature<IReadRawTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadRawTagValues>();
                    return;
                }

                var end = DateTime.UtcNow;
                var start = end.AddYears(-1);

                var request = new ReadRawTagValuesRequest() { 
                    Tags = new[] { AssemblyInitializer.TestTagId },
                    SampleCount = 50000,
                    UtcStartTime = start,
                    UtcEndTime = end
                };

                var values = await feature.ReadRawTagValues(context, request, ct).ToEnumerable(-1, ct).ConfigureAwait(false);
                Assert.AreEqual(request.SampleCount, values.Count());
            });
        }



        [TestMethod]
        public Task ProxyShouldRetrieveRemoteAdapterDetails() {
            return RunAdapterTest((proxy, context, ct) => {
                Assert.IsNotNull(proxy.RemoteHostInfo);
                Assert.IsNotNull(proxy.RemoteDescriptor);
                Assert.IsNotNull(proxy.RemoteDescriptor.TypeDescriptor);

                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task ProxyShouldHaveLocalImplementationForAllRemoteStandardFeatures() {
            return RunAdapterTest((proxy, context, ct) => {
                foreach (var featureUriOrName in proxy.RemoteDescriptor.Features) {
                    if (UnsupportedStandardFeatures.Contains(featureUriOrName, StringComparer.OrdinalIgnoreCase)) {
                        continue;
                    }
                    Assert.IsTrue(proxy.HasFeature(featureUriOrName), $"Expected to find local implementation for remote feature: {featureUriOrName}");
                }

                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task ProxyShouldHaveLocalImplementationForAllRemoteExtensionFeatures() {
            return RunAdapterTest((proxy, context, ct) => {
                foreach (var featureUriOrName in proxy.RemoteDescriptor.Extensions) {
                    Assert.IsTrue(proxy.HasFeature(featureUriOrName), $"Expected to find local implementation for remote feature: {featureUriOrName}");
                }

                return Task.CompletedTask;
            });
        }

    }
}
#endif
