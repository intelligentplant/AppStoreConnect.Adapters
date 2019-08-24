using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Features {

    /// <summary>
    /// Feature for writing new snapshot values to adapter tags.
    /// </summary>
    public interface IWriteSnapshotTagValues : IAdapterFeature {

        /// <summary>
        /// Writes a stream of snapshot tag values to an adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="channel">
        ///   A <see cref="ChannelReader{T}"/> that will provide the tag values to write to the 
        ///   adapter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ChannelReader{T}"/> that will emit a write result for each item read from 
        ///   the input <paramref name="channel"/>.
        /// </returns>
        ChannelReader<WriteTagValueResult> WriteSnapshotTagValues(IAdapterCallContext context, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken);

    }
}
