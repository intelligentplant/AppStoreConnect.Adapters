using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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


        public async IAsyncEnumerable<WriteTagValueResult> WriteSnapshotTagValues(
            IAdapterCallContext context, 
            WriteTagValuesRequest request,
            IAsyncEnumerable<WriteTagValueItem> channel, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            await foreach (var item in channel.WithCancellation(cancellationToken)) {
                yield return new WriteTagValueResult(
                    item.CorrelationId,
                    item.TagId,
                    Common.StatusCodes.Good,
                    nameof(IWriteSnapshotTagValues),
                    null
                );
            }
        }

        public async IAsyncEnumerable<WriteTagValueResult> WriteHistoricalTagValues(
            IAdapterCallContext context,
            WriteTagValuesRequest request,
            IAsyncEnumerable<WriteTagValueItem> channel, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            await foreach (var item in channel.WithCancellation(cancellationToken)) {
                yield return new WriteTagValueResult(
                    item.CorrelationId,
                    item.TagId,
                    Common.StatusCodes.Good,
                    nameof(IWriteHistoricalTagValues),
                    null
                );
            }
        }

        public async IAsyncEnumerable<WriteEventMessageResult> WriteEventMessages(
            IAdapterCallContext context, 
            IAsyncEnumerable<WriteEventMessageItem> channel, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            await foreach (var item in channel.WithCancellation(cancellationToken)) {
                yield return new WriteEventMessageResult(
                    item.CorrelationId,
                    Common.StatusCodes.Good,
                    nameof(IWriteEventMessages),
                    null
                );
            }
        }
    }
}
