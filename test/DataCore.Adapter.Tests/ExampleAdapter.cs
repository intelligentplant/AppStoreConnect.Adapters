using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.Events;
using DataCore.Adapter.RealTimeData;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Tests {
    public class ExampleAdapter : IAdapter, IReadSnapshotTagValues {

        public AdapterDescriptor Descriptor { get; }

        public IAdapterFeaturesCollection Features { get; }

        public IEnumerable<AdapterProperty> Properties { get; } = Array.Empty<AdapterProperty>();

        public bool IsEnabled { get; set; } = true;

        public bool IsRunning { get; } = true;

        private readonly SnapshotSubscriptionManager _snapshotSubscriptionManager;

        private readonly EventSubscriptionManager _eventSubscriptionManager;


        public ExampleAdapter() {
            Descriptor = AdapterDescriptor.Create("unit-tests", "Unit Tests Adapter", "Adapter for use in unit tests");
            var features = new AdapterFeaturesCollection(this);
            _snapshotSubscriptionManager = new SnapshotSubscriptionManager();
            _eventSubscriptionManager = new EventSubscriptionManager();
            features.Add<ISnapshotTagValuePush, SnapshotSubscriptionManager>(_snapshotSubscriptionManager);
            features.Add<IEventMessagePush, EventSubscriptionManager>(_eventSubscriptionManager);
            features.AddFromProvider(new PingPongExtension(BackgroundTaskService.Default));
            Features = features;
        }


        public Task StartAsync(CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }


        public Task StopAsync(CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
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


        public ValueTask<bool> WriteTestEventMessage(EventMessage msg) {
            return _eventSubscriptionManager.ValueReceived(msg);
        }


        private class SnapshotSubscriptionManager : SnapshotTagValuePush {


            public SnapshotSubscriptionManager() : base(null, null, null) { }


            protected override void OnTagsAdded(IEnumerable<TagIdentifier> tags) {
                base.OnTagsAdded(tags);
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
    }
}
