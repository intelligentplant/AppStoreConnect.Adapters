
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;
using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Benchmarks {
    public class SnapshotPush : SnapshotTagValuePush {

        private readonly Channel<TagIdentifier> _subscriptionsAdded = Channel.CreateUnbounded<TagIdentifier>(new UnboundedChannelOptions() { 
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });


        public SnapshotPush(int samplesPerTag) : base(
            new SnapshotTagValuePushOptions(), 
            null, 
            null
        ) {
            BackgroundTaskService.QueueBackgroundWorkItem(async ct => { 
                while (await _subscriptionsAdded.Reader.WaitToReadAsync(ct)) {
                    if (!_subscriptionsAdded.Reader.TryRead(out var tag)) {
                        continue;
                    }

                    for (var i = 0; i < samplesPerTag; i++) {
                        if (ct.IsCancellationRequested) {
                            break;
                        }

                        await ValueReceived(
                            new TagValueQueryResult(
                                tag.Id,
                                tag.Name,
                                TagValueBuilder.Create().WithValue(i).Build()
                            ),
                            ct
                        );
                    }
                }
            });
        }


        private async Task Run(TagIdentifier tag, int sampleCount, CancellationToken cancellationToken) {
            for (var i = 0; i < sampleCount; i++) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }

                await ValueReceived(
                    new TagValueQueryResult(
                        tag.Id,
                        tag.Name,
                        TagValueBuilder.Create().WithValue(i).Build()
                    ),
                    cancellationToken
                );
            }
        }


        protected override async Task OnTagsAdded(IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
            await base.OnTagsAdded(tags, cancellationToken).ConfigureAwait(false);
            foreach (var tag in tags) {
                await _subscriptionsAdded.Writer.WriteAsync(tag, cancellationToken).ConfigureAwait(false);
            }
        }


        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _subscriptionsAdded.Writer.TryComplete();
        }

    }
}
