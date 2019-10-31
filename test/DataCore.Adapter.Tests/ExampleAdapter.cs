using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Tests {
    public class ExampleAdapter : IAdapter, IReadSnapshotTagValues, IDisposable {

        public AdapterDescriptor Descriptor { get; }

        public IAdapterFeaturesCollection Features { get; }

        public IEnumerable<AdapterProperty> Properties { get; } = Array.Empty<AdapterProperty>();

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
            throw new NotImplementedException();
        }


        public void Dispose() {
            _snapshotSubscriptionManager.Dispose();
        }


        public void WriteSnapshotValue(TagValueQueryResult value) {
            _snapshotSubscriptionManager.WriteSnapshotValue(value);
        }


        private class SnapshotSubscriptionManager : SnapshotTagValuePush {


            public SnapshotSubscriptionManager() : base(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance) { }


            protected override ChannelReader<TagIdentifier> GetTags(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                var channel = Channel.CreateUnbounded<TagIdentifier>();
                channel.Writer.RunBackgroundOperation((ch, ct) => {
                    foreach (var item in tagNamesOrIds) {
                        ch.TryWrite(TagIdentifier.Create(item, item));
                    }
                }, true, null, cancellationToken);

                return channel;
            }


            protected override Task OnSubscribe(IEnumerable<string> tagIds, CancellationToken cancellationToken) {
                return Task.CompletedTask;
            }


            protected override Task OnUnsubscribe(IEnumerable<string> tagIds, CancellationToken cancellationToken) {
                return Task.CompletedTask;
            }


            internal void WriteSnapshotValue(TagValueQueryResult value) {
                OnValuesChanged(new[] { value });
            }


            protected override void Dispose(bool disposing) {
                // Do nothing
            }
        }
    }
}
