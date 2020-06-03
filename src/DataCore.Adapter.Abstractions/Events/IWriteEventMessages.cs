using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Feature that allows event messages to be written to an adapter.
    /// </summary>
    public interface IWriteEventMessages : IAdapterFeature {

        /// <summary>
        /// Writes a stream of event messages to an adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="channel">
        ///   A <see cref="ChannelReader{T}"/> that will provide the event messages to write to the 
        ///   adapter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ChannelReader{T}"/> that will emit a write result for each item read from 
        ///   the input <paramref name="channel"/>.
        /// </returns>
        Task<ChannelReader<WriteEventMessageResult>> WriteEventMessages(
            IAdapterCallContext context, 
            ChannelReader<WriteEventMessageItem> channel, 
            CancellationToken cancellationToken
        );

    }
}
