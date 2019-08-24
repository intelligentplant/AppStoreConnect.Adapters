using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Tests {
    public class ExampleAdapter : IAdapter, IReadSnapshotTagValues, IDisposable {

        public AdapterDescriptor Descriptor { get; }

        public IAdapterFeaturesCollection Features { get; }

        public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

        private readonly SnapshotSubscriptionManager _snapshotSubscriptionManager;


        public ExampleAdapter() {
            Descriptor = new AdapterDescriptor("unit-tests", "Unit Tests Adapter", "Adapter for use in unit tests");
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


        private class SnapshotSubscriptionManager : RealTimeData.Utilities.SnapshotTagValueSubscriptionManager {


            public SnapshotSubscriptionManager() : base(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance) { }


            protected override ChannelReader<TagIdentifier> GetTags(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                var channel = Channel.CreateUnbounded<TagIdentifier>();
                channel.Writer.RunBackgroundOperation((ch, ct) => {
                    foreach (var item in tagNamesOrIds) {
                        ch.TryWrite(new TagIdentifier(item, item));
                    }
                }, true, cancellationToken);

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
