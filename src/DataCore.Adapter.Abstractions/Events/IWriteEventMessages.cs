using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Feature that allows event messages to be written to an adapter.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.Events.WriteEventMessages,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_WriteEventMessages),
        Description = nameof(AbstractionsResources.Description_WriteEventMessages)
    )]
    public interface IWriteEventMessages : IAdapterFeature {

        /// <summary>
        /// Writes a stream of event messages to an adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="channel">
        ///   An <see cref="IAsyncEnumerable{T}"/> that will provide the event messages to write 
        ///   to the adapter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will emit a write result for each item 
        ///   read from the input <paramref name="channel"/>.
        /// </returns>
        IAsyncEnumerable<WriteEventMessageResult> WriteEventMessages(
            IAdapterCallContext context, 
            WriteEventMessagesRequest request,
            IAsyncEnumerable<WriteEventMessageItem> channel, 
            CancellationToken cancellationToken
        );

    }
}
