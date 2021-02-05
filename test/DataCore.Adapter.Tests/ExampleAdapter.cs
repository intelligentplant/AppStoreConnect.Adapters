using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Events;
using DataCore.Adapter.RealTimeData;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Tests {
    public class ExampleAdapter : IAdapter, ITagInfo, IReadSnapshotTagValues {

        private CancellationTokenSource _stopTokenSource;

        public IBackgroundTaskService BackgroundTaskService { get; }

        public AdapterDescriptor Descriptor { get; }

        public AdapterTypeDescriptor TypeDescriptor { get; }

        public IAdapterFeaturesCollection Features { get; }

        public IEnumerable<AdapterProperty> Properties { get; } = Array.Empty<AdapterProperty>();

        public bool IsEnabled { get; set; } = true;

        public bool IsRunning { get; } = true;

        private readonly SnapshotSubscriptionManager _snapshotSubscriptionManager;

        private readonly EventSubscriptionManager _eventSubscriptionManager;

        private readonly EventTopicSubscriptionManager _eventTopicSubscriptionManager;


        public ExampleAdapter() {
            BackgroundTaskService = new BackgroundTaskServiceWrapper(
                IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default,
                () => _stopTokenSource?.Token ?? default
            );
            Descriptor = AdapterDescriptor.Create("unit-tests", "Unit Tests Adapter", "Adapter for use in unit tests");
            TypeDescriptor = this.CreateTypeDescriptor();
            var features = new AdapterFeaturesCollection(this);
            _snapshotSubscriptionManager = new SnapshotSubscriptionManager(this);
            _eventSubscriptionManager = new EventSubscriptionManager();
            _eventTopicSubscriptionManager = new EventTopicSubscriptionManager();
            features.Add<ISnapshotTagValuePush, SnapshotSubscriptionManager>(_snapshotSubscriptionManager);
            features.Add<IEventMessagePush, EventSubscriptionManager>(_eventSubscriptionManager);
            features.Add<IEventMessagePushWithTopics, EventTopicSubscriptionManager>(_eventTopicSubscriptionManager);
            features.AddFromProvider(new PingPongExtension(BackgroundTaskService));
            Features = features;
        }


        public Task StartAsync(CancellationToken cancellationToken = default) {
            _stopTokenSource = new CancellationTokenSource();
            return Task.CompletedTask;
        }


        public Task StopAsync(CancellationToken cancellationToken = default) {
            _stopTokenSource?.Cancel();
            _stopTokenSource?.Dispose();
            return Task.CompletedTask;
        }


        public Task<ChannelReader<AdapterProperty>> GetTagProperties(IAdapterCallContext context, GetTagPropertiesRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            Validator.ValidateObject(request, new ValidationContext(request), true);

            var result = Channel.CreateUnbounded<AdapterProperty>();
            result.Writer.TryComplete();
            return Task.FromResult(result.Reader);
        }


        public Task<ChannelReader<TagDefinition>> GetTags(IAdapterCallContext context, GetTagsRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            Validator.ValidateObject(request, new ValidationContext(request), true);

            var result = Channel.CreateUnbounded<TagDefinition>();
            foreach (var item in request.Tags) {
                result.Writer.TryWrite(new TagDefinition(item, item, null, null, VariantType.Double, null, null, null, null));
            }
            result.Writer.TryComplete();
            return Task.FromResult(result.Reader);
        }


        public Task<ChannelReader<TagValueQueryResult>> ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            var result = request.Tags.Select(t => new TagValueQueryResult(
                t,
                t,
                TagValueBuilder.Create()
                    .WithUtcSampleTime(DateTime.MinValue)
                    .WithValue(0)
                    .Build()
            )).PublishToChannel();

            return Task.FromResult(result);
        }


        public void Dispose() {
            _snapshotSubscriptionManager.Dispose();
        }


        public ValueTask DisposeAsync() {
            Dispose();
            return default;
        }


        public ValueTask<bool> WriteSnapshotValue(TagValueQueryResult value) {
            return _snapshotSubscriptionManager.ValueReceived(value);
        }


        public async ValueTask<bool> WriteTestEventMessage(EventMessage msg) {
            return await _eventSubscriptionManager.ValueReceived(msg).ConfigureAwait(false) && await _eventTopicSubscriptionManager.ValueReceived(msg).ConfigureAwait(false);
        }


        private class SnapshotSubscriptionManager : SnapshotTagValuePush {


            public SnapshotSubscriptionManager(ITagInfo tagInfo) : base(new SnapshotTagValuePushOptions() {
                TagResolver = CreateTagResolverFromFeature(tagInfo)
            }, null, null) { }


            protected override async Task OnTagsAdded(IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
                await base.OnTagsAdded(tags, cancellationToken).ConfigureAwait(false);
                foreach (var tag in tags) {
                    ValueReceived(new TagValueQueryResult(
                        tag.Id,
                        tag.Name,
                        TagValueBuilder.Create()
                            .WithUtcSampleTime(DateTime.MinValue)
                            .WithValue(0)
                            .Build()
                    )).GetAwaiter().GetResult();
                }
            }

        }


        private class EventSubscriptionManager : EventMessagePush {

            public EventSubscriptionManager() : base(null, null, null) { }

        }


        private class EventTopicSubscriptionManager : EventMessagePushWithTopics {

            public EventTopicSubscriptionManager() : base(null, null, null) { }

        }

    }
}
