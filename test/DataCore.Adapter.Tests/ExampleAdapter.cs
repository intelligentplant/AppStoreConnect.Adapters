using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Tests {
    public class ExampleAdapter : IAdapter, IReadSnapshotTagValues {

        public AdapterDescriptor Descriptor { get; }

        public IAdapterFeaturesCollection Features { get; }

        public IEnumerable<AdapterProperty> Properties { get; } = Array.Empty<AdapterProperty>();

        public bool IsRunning { get; } = true;

        private readonly SnapshotSubscriptionManager _snapshotSubscriptionManager;


        public ExampleAdapter() {
            Descriptor = AdapterDescriptor.Create("unit-tests", "Unit Tests Adapter", "Adapter for use in unit tests");
            var features = new AdapterFeaturesCollection(this);
            _snapshotSubscriptionManager = new SnapshotSubscriptionManager();
            features.Add<ISnapshotTagValuePush, SnapshotSubscriptionManager>(_snapshotSubscriptionManager);
            Features = features;
        }


        public Task StartAsync(CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }


        public Task StopAsync(CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }


        public ChannelReader<TagValueQueryResult> ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            return request.Tags.Select(t => new TagValueQueryResult(
                t,
                t,
                TagValueBuilder.Create()
                    .WithUtcSampleTime(DateTime.MinValue)
                    .WithValue(0)
                    .Build()
            )).PublishToChannel();
        }


        public void Dispose() {
            _snapshotSubscriptionManager.Dispose();
        }


        public ValueTask DisposeAsync() {
            Dispose();
            return default;
        }


        public ValueTask<bool> WriteSnapshotValue(TagValueQueryResult value) {
            return _snapshotSubscriptionManager.WriteSnapshotValue(value);
        }


        private class SnapshotSubscriptionManager : SnapshotTagValuePush {


            public SnapshotSubscriptionManager() : base(null, null, null) { }


            protected override void OnTagAddedToSubscription(TagIdentifier tag) {
                base.OnTagAddedToSubscription(tag);
                WriteSnapshotValue(new TagValueQueryResult(
                    tag.Id,
                    tag.Name,
                    TagValueBuilder.Create()
                        .WithUtcSampleTime(DateTime.MinValue)
                        .WithValue(0)
                        .Build()
                ));
            }


            internal ValueTask<bool> WriteSnapshotValue(TagValueQueryResult value) {
                return ValueReceived(value);
            }

        }
    }
}
