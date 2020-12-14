using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;
using DataCore.Adapter.RealTimeData;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Tests {
    public class NullValueWrite : 
        IWriteSnapshotTagValues, 
        IWriteHistoricalTagValues
        //IWriteEventMessages 
    {

        public IBackgroundTaskService BackgroundTaskService => IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;


        public Task<ChannelReader<WriteTagValueResult>> WriteSnapshotTagValues(IAdapterCallContext context, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueWriteResultChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                while (await channel.WaitToReadAsync(ct)) {
                    while (channel.TryRead(out var item)) {
                        await ch.WriteAsync(new WriteTagValueResult(
                            item.CorrelationId,
                            item.TagId,
                            Common.WriteStatus.Success,
                            nameof(IWriteSnapshotTagValues),
                            null
                        ), ct);
                    }
                }
            }, true, null, cancellationToken);

            return Task.FromResult(result.Reader);
        }

        public Task<ChannelReader<WriteTagValueResult>> WriteHistoricalTagValues(IAdapterCallContext context, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueWriteResultChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                while (await channel.WaitToReadAsync(ct)) {
                    while (channel.TryRead(out var item)) {
                        await ch.WriteAsync(new WriteTagValueResult(
                            item.CorrelationId,
                            item.TagId,
                            Common.WriteStatus.Success,
                            nameof(IWriteHistoricalTagValues),
                            null
                        ), ct);
                    }
                }

            }, true, null, cancellationToken);

            return Task.FromResult(result.Reader);
        }

        public Task<ChannelReader<WriteEventMessageResult>> WriteEventMessages(IAdapterCallContext context, ChannelReader<WriteEventMessageItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageWriteResultChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                while (await channel.WaitToReadAsync(ct)) {
                    while (channel.TryRead(out var item)) {
                        await ch.WriteAsync(new WriteEventMessageResult(
                            item.CorrelationId,
                            Common.WriteStatus.Success,
                            nameof(IWriteEventMessages),
                            null
                        ), ct);
                    }
                }
            }, true, null, cancellationToken);

            return Task.FromResult(result.Reader);
        }
    }
}
